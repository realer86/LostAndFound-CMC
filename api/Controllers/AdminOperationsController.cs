using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record ResolveTicketRequest(string Action,string Note);
public record SuspendUserRequest(int Days,string Reason);
public record CustodyActionRequest(string Action,string? ReceiptCode);

[Authorize(AuthenticationSchemes="AdminCookie",Roles="ADMIN"),ApiController,Route("api/admin/operations")]
public class AdminOperationsController(LostFoundDb db) : ControllerBase
{
    private Guid AdminId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private void Log(string action,string type,Guid id)=>db.AuditLogs.Add(new AuditLog{ActorId=AdminId,Action=action,TargetType=type,TargetId=id});
    [HttpGet("cases")]
    public async Task<IActionResult> Cases([FromQuery]string? status,[FromQuery]int page=1)
    {
        var query=db.CaseTickets.AsNoTracking();
        if(status is "OPEN" or "RESOLVED" or "DISMISSED")query=query.Where(x=>x.Status==status);
        page=Math.Clamp(page,1,10000);const int size=20;
        var total=await query.CountAsync();
        var entries=await query.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync();
        var ids=entries.Select(x=>x.ReporterId).ToArray();
        var users=await db.Users.AsNoTracking().Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.FullName);
        return Ok(new{items=entries.Select(x=>new{x.Id,x.Type,x.Status,x.Reason,x.Resolution,x.ReportId,x.RoomId,x.ReporterId,reporter=users.GetValueOrDefault(x.ReporterId),x.CreatedAt,x.ResolvedAt}),total,page,pageSize=size});
    }
    [HttpPatch("cases/{id:guid}")]
    public async Task<IActionResult> Resolve(Guid id,ResolveTicketRequest req)
    {
        var ticket=await db.CaseTickets.FirstOrDefaultAsync(x=>x.Id==id);
        if(ticket is null)return NotFound();
        if(ticket.Status!="OPEN")return Conflict(new{message="Hồ sơ đã được xử lý."});
        if((req.Note?.Trim().Length??0)<8 || req.Note!.Length>1500)return BadRequest(new{message="Ghi chú xử lý cần 8-1500 ký tự."});
        if(req.Action is not ("DISMISS" or "HIDE_REPORT" or "RESUME_ROOM" or "CANCEL_HANDOVER"))return BadRequest(new{message="Hành động không hợp lệ."});
        if(req.Action=="HIDE_REPORT")
        {
            if(ticket.Type!="REPORT_ABUSE"||!ticket.ReportId.HasValue)return BadRequest(new{message="Chỉ ẩn bài đối với báo cáo vi phạm."});
            var post=await db.Reports.FindAsync(ticket.ReportId.Value);
            if(post is null)return NotFound();
            if(post.Status is "RETURNED" or "CLOSED")return Conflict(new{message="Không thể ẩn bài đã hoàn tất."});
            if(await db.Claims.AnyAsync(c=>c.FoundReportId==post.Id&&c.Status=="APPROVED") ||
               await db.CustodyRecords.AnyAsync(c=>c.ReportId==post.Id&&c.Status=="RECEIVED"))
                return Conflict(new{message="Bài đang có phòng bàn giao. Hãy xử lý yêu cầu đang duyệt/tranh chấp trước khi ẩn."});
            post.Status="HIDDEN";post.UpdatedAt=DateTime.UtcNow;
            db.Notifications.Add(new Notification{UserId=post.UserId,ReportId=post.Id,Type="MODERATION",Content="Bài đăng của bạn đã bị ẩn do báo cáo vi phạm. Liên hệ quản trị viên nếu cần giải thích."});
        }
        if(req.Action is "RESUME_ROOM" or "CANCEL_HANDOVER" || (req.Action=="DISMISS" && ticket.RoomId.HasValue))
        {
            if(ticket.Type=="REPORT_ABUSE"||!ticket.RoomId.HasValue)return BadRequest(new{message="Hành động yêu cầu một tranh chấp Phòng bàn giao."});
            var room=await db.HandoverRooms.FindAsync(ticket.RoomId.Value);
            if(room is null||room.Status!="DISPUTED")return Conflict(new{message="Phòng không còn ở trạng thái tranh chấp."});
            var claim=await db.Claims.FindAsync(room.ClaimId);
            if(claim is null)return NotFound();
            var found=await db.Reports.FindAsync(claim.FoundReportId);
            if(found is null)return NotFound();
            if(req.Action=="CANCEL_HANDOVER")
            {
                if(room.HolderConfirmedAt!=null||room.ClaimantConfirmedAt!=null)return Conflict(new{message="Đã có xác nhận giao nhận; cần đối soát thủ công trước khi hủy."});
                room.Status="CANCELLED";claim.Status="CANCELLED";
                foreach(var a in await db.HandoverAppointments.Where(x=>x.RoomId==room.Id && (x.Status=="PROPOSED"||x.Status=="ACCEPTED")).ToListAsync())a.Status="CANCELLED";
            }
            else
            {
                room.Status=room.HolderConfirmedAt!=null||room.ClaimantConfirmedAt!=null?"AWAITING_CONFIRMATION":await db.HandoverAppointments.AnyAsync(x=>x.RoomId==room.Id&&x.Status=="ACCEPTED")?"SCHEDULED":"ARRANGING";
            }
            db.Notifications.Add(new Notification{UserId=found.UserId,ReportId=found.Id,Type="DISPUTE",Content=$"Quản trị viên đã xử lý tranh chấp. Kết quả: {req.Action}."});
            db.Notifications.Add(new Notification{UserId=claim.ClaimantId,ReportId=found.Id,Type="DISPUTE",Content=$"Quản trị viên đã xử lý tranh chấp. Kết quả: {req.Action}."});
        }
        ticket.Status=req.Action=="DISMISS"?"DISMISSED":"RESOLVED";
        ticket.Resolution=req.Note.Trim();ticket.ResolvedById=AdminId;ticket.ResolvedAt=DateTime.UtcNow;
        db.Notifications.Add(new Notification{UserId=ticket.ReporterId,ReportId=ticket.ReportId,Type="CASE",Content=$"Báo cáo đã được xử lý: {ticket.Resolution}"});
        Log("CASE_"+req.Action,"CASE",id);
        await db.SaveChangesAsync();return Ok(new{message="Đã xử lý và ghi nhật ký."});
    }
    [HttpPatch("users/{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id,SuspendUserRequest req)
    {
        if(req.Days<0||req.Days>365)return BadRequest(new{message="Số ngày khóa 0-365 (0 nghĩa là mở khóa)."});
        if(string.IsNullOrWhiteSpace(req.Reason)||req.Reason.Trim().Length<8)return BadRequest(new{message="Cần nêu lý do xử lý."});
        var user=await db.Users.FindAsync(id);
        if(user is null)return NotFound();
        if(user.Role=="ADMIN")return Conflict(new{message="Không khóa tài khoản quản trị ở đây."});
        user.SuspendedUntil=req.Days==0?null:DateTime.UtcNow.AddDays(req.Days);
        db.Notifications.Add(new Notification{UserId=id,Type="MODERATION",Content=req.Days==0?"Tài khoản đã được mở khóa.":$"Tài khoản bị hạn chế {req.Days} ngày. Lý do: {req.Reason.Trim()[..Math.Min(150,req.Reason.Trim().Length)]}"});
        Log(req.Days==0?"USER_UNSUSPENDED":"USER_SUSPENDED","USER",id);
        await db.SaveChangesAsync();return Ok(new{message=req.Days==0?"Đã mở khóa tài khoản.":"Đã giới hạn tài khoản."});
    }
    [HttpGet("custody")]
    public async Task<IActionResult> Custody([FromQuery]int page=1)
    {
        page=Math.Clamp(page,1,10000);const int size=20;
        var total=await db.CustodyRecords.CountAsync();
        var entries=await db.CustodyRecords.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync();
        var ids=entries.Select(x=>x.ReportId).ToArray();
        var names=await db.Reports.AsNoTracking().Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.ItemName);
        return Ok(new{items=entries.Select(x=>new{x.Id,x.ReportId,x.RequestedById,x.Desk,x.ReceiptCode,x.Status,x.ReceivedAt,x.ReleasedAt,x.CreatedAt,itemName=names.GetValueOrDefault(x.ReportId)}),total,page,pageSize=size});
    }
    [HttpPatch("custody/{id:guid}")]
    public async Task<IActionResult> CustodyAction(Guid id,CustodyActionRequest req)
    {
        var record=await db.CustodyRecords.FindAsync(id);
        if(record is null)return NotFound();
        var report=await db.Reports.FindAsync(record.ReportId);
        if(report is null)return NotFound();
        if(req.Action=="ACCEPT")
        {
            if(record.Status!="REQUESTED"||report.Status!="OPEN")return Conflict(new{message="Chỉ nhận ký gửi mới của bài còn mở."});
            var approvedRooms=await db.Claims.Where(c=>c.FoundReportId==report.Id && c.Status=="APPROVED")
                .Join(db.HandoverRooms,c=>c.Id,h=>h.ClaimId,(c,h)=>h).ToListAsync();
            if(approvedRooms.Any(h=>h.HolderConfirmedAt!=null||h.ClaimantConfirmedAt!=null))
                return Conflict(new{message="Vụ việc đã có xác nhận bàn giao. Không tiếp nhận ký gửi vào thời điểm này."});
            var code=req.ReceiptCode?.Trim();
            if(string.IsNullOrWhiteSpace(code)||code.Length<4||code.Length>120)return BadRequest(new{message="Cần nhập mã biên nhận từ 4-120 ký tự."});
            record.Status="RECEIVED";record.ReceiptCode=code;record.ReceivedAt=DateTime.UtcNow;record.AcceptedByAdminId=AdminId;
            db.Notifications.Add(new Notification{UserId=record.RequestedById,ReportId=report.Id,Type="CUSTODY",Content=$"Quầy {record.Desk} đã ghi nhận tiếp nhận '{report.ItemName}'. Mã biên nhận được quản lý nội bộ."});
        }
        else if(req.Action=="RELEASE")
        {
            if(record.Status!="RECEIVED")return Conflict(new{message="Quầy chưa nhận đồ hoặc đã hoàn tất."});
            var info=await db.Claims.Where(x=>x.FoundReportId==record.ReportId && x.Status=="APPROVED")
                .Join(db.HandoverRooms,c=>c.Id,r=>r.ClaimId,(c,r)=>new{c,r}).FirstOrDefaultAsync();
            if(info is null || info.r.Status is "CANCELLED" or "DISPUTED" or "COMPLETED")return Conflict(new{message="Phải có yêu cầu đã duyệt và phòng bàn giao hợp lệ."});
            if(!await db.HandoverAppointments.AnyAsync(x=>x.RoomId==info.r.Id&&x.Status=="ACCEPTED"))return Conflict(new{message="Cần lịch bàn giao đã được thống nhất."});
            // Staff physically verifies identity before this action; the claimant must still confirm receipt.
            record.Status="RELEASED";record.ReleasedAt=DateTime.UtcNow;
            info.r.HolderConfirmedAt=DateTime.UtcNow;info.r.Status="AWAITING_CONFIRMATION";
            db.Notifications.Add(new Notification{UserId=info.c.ClaimantId,ReportId=report.Id,Type="CUSTODY",Content="Nhân viên quầy đã xác nhận trao đồ. Hãy xác nhận ĐÃ NHẬN trong Phòng bàn giao sau khi bạn thực sự nhận đồ."});
            db.Notifications.Add(new Notification{UserId=report.UserId,ReportId=report.Id,Type="CUSTODY",Content="Nhân viên quầy đã xác nhận trao đồ cho người đã được duyệt. Đang chờ người nhận xác nhận."});
        }
        else if(req.Action=="REJECT")
        {
            if(record.Status!="REQUESTED")return Conflict(new{message="Chỉ từ chối yêu cầu đang chờ."});
            record.Status="REJECTED";
            db.Notifications.Add(new Notification{UserId=record.RequestedById,ReportId=report.Id,Type="CUSTODY",Content="Yêu cầu ký gửi không được chấp thuận. Vui lòng liên hệ quầy bảo vệ."});
        }
        else return BadRequest(new{message="Hành động phải là ACCEPT, RELEASE hoặc REJECT."});
        Log("CUSTODY_"+req.Action,"CUSTODY",id);
        await db.SaveChangesAsync();return Ok(new{message="Đã cập nhật hồ sơ ký gửi."});
    }
}
