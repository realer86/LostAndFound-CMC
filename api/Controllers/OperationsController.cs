using LostFound.Api.Services;
using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record ReportIssueRequest(string Type,string Reason,Guid? ReportId,Guid? RoomId);
public record DeskRequest(string Desk);

[Authorize,ApiController,Route("api/operations")]
public class OperationsController(LostFoundDb db, MatchEngine engine) : ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly string[] Desks = ["Quầy bảo vệ - Tòa A", "Quầy Lost & Found - Thư viện", "Phòng trực bảo vệ - Cổng chính", "Quầy trực ký túc xá"];

    [HttpGet("cases/mine")]
    public async Task<IActionResult> MyCases()=>Ok(await db.CaseTickets.AsNoTracking().Where(x=>x.ReporterId==UserId).OrderByDescending(x=>x.CreatedAt).Take(100).Select(x=>new{x.Id,x.Type,x.Status,x.ReportId,x.RoomId,x.Reason,x.Resolution,x.CreatedAt,x.ResolvedAt}).ToListAsync());

    [HttpPost("cases")]
    public async Task<IActionResult> CreateCase(ReportIssueRequest req)
    {
        var reason=req.Reason?.Trim()??"";
        if(reason.Length<15||reason.Length>2000)return BadRequest(new{message="Mô tả cần từ 15 đến 2000 ký tự."});
        if(req.Type is not ("REPORT_ABUSE" or "HANDOVER_DISPUTE" or "NO_SHOW"))return BadRequest(new{message="Loại báo cáo không hợp lệ."});
        Guid? reportId=req.ReportId,roomId=req.RoomId;
        Guid? other=null;
        if(req.Type=="REPORT_ABUSE")
        {
            if(!reportId.HasValue||!await db.Reports.AnyAsync(x=>x.Id==reportId.Value))return NotFound(new{message="Không tìm thấy bài đăng."});
        }
        else
        {
            if(!roomId.HasValue)return BadRequest(new{message="Cần chọn phòng bàn giao."});
            var claimInfo=await db.HandoverRooms.Where(x=>x.Id==roomId).Join(db.Claims,r=>r.ClaimId,c=>c.Id,(r,c)=>new{r,c})
                .Join(db.Reports,x=>x.c.FoundReportId,p=>p.Id,(x,p)=>new{x.r,x.c,p}).FirstOrDefaultAsync();
            if(claimInfo is null)return NotFound();
            if(claimInfo.p.UserId!=UserId && claimInfo.c.ClaimantId!=UserId)return Forbid();
            if(claimInfo.r.Status is "COMPLETED" or "CANCELLED")return Conflict(new{message="Phòng đã kết thúc."});
            if(claimInfo.r.Status=="DISPUTED" && await db.CaseTickets.AnyAsync(x=>x.RoomId==roomId&&x.Status=="OPEN"))return Conflict(new{message="Phòng đang có tranh chấp chưa được xử lý."});
            if(req.Type=="NO_SHOW" && !await db.HandoverAppointments.AnyAsync(x=>x.RoomId==roomId && x.Status=="ACCEPTED" && x.MeetingAt<DateTime.UtcNow))return Conflict(new{message="Chỉ báo vắng mặt sau thời điểm lịch hẹn đã xác nhận."});
            reportId=claimInfo.p.Id;
            claimInfo.r.Status="DISPUTED";
            other=claimInfo.p.UserId==UserId?claimInfo.c.ClaimantId:claimInfo.p.UserId;
        }
        if(await db.CaseTickets.CountAsync(x=>x.ReporterId==UserId && x.CreatedAt>DateTime.UtcNow.AddDays(-1))>=8)return StatusCode(429,new{message="Bạn đã gửi quá nhiều báo cáo hôm nay."});
        var ticket=new CaseTicket{ReporterId=UserId,ReportId=reportId,RoomId=roomId,Type=req.Type,Reason=reason};
        db.CaseTickets.Add(ticket);
        if(other.HasValue)db.Notifications.Add(new Notification{UserId=other.Value,Type="DISPUTE",ReportId=reportId,Content="Phòng bàn giao đang chờ quản trị viên xem xét khiếu nại."});
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="CASE_SUBMITTED",TargetType="CASE",TargetId=ticket.Id});
        await db.SaveChangesAsync();return Ok(new{ticket.Id,message="Đã gửi báo cáo; quản trị viên sẽ xử lý."});
    }

    [HttpGet("custody/mine")]
    public async Task<IActionResult> MineCustody()
    {
        var ownedIds=await db.Reports.AsNoTracking().Where(x=>x.UserId==UserId).Select(x=>x.Id).ToListAsync();
        return Ok(await db.CustodyRecords.AsNoTracking().Where(x=>ownedIds.Contains(x.ReportId)).OrderByDescending(x=>x.CreatedAt).ToListAsync());
    }
    [HttpGet("custody/desks")]public IActionResult SafeDesks()=>Ok(Desks);
    [HttpPost("custody/{reportId:guid}")]
    public async Task<IActionResult> RequestCustody(Guid reportId,DeskRequest req)
    {
        var report=await db.Reports.FirstOrDefaultAsync(x=>x.Id==reportId && x.UserId==UserId);
        if(report is null)return NotFound();
        if(report.Type!="FOUND" || report.Status!="OPEN")return Conflict(new{message="Chỉ có thể gửi đồ nhặt được đang mở đến quầy."});
        if(!Desks.Contains(req.Desk))return BadRequest(new{message="Chọn quầy an toàn trong danh sách."});
        if(await db.CustodyRecords.AnyAsync(x=>x.ReportId==reportId))return Conflict(new{message="Bài này đã có hồ sơ ký gửi."});
        var record=new CustodyRecord{ReportId=reportId,RequestedById=UserId,Desk=req.Desk};
        db.CustodyRecords.Add(record);
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="CUSTODY_REQUESTED",TargetType="CUSTODY",TargetId=record.Id});
        await db.SaveChangesAsync();return Ok(new{record.Id,message="Đã gửi yêu cầu ký gửi. Chỉ mang đồ đến quầy khi đã được nhân viên xác nhận tiếp nhận."});
    }

    [HttpPatch("reports/{reportId:guid}/renew")]
    public async Task<IActionResult> Renew(Guid reportId)
    {
        var report=await db.Reports.FirstOrDefaultAsync(x=>x.Id==reportId && x.UserId==UserId);
        if(report is null)return NotFound();
        if(report.Status is not ("OPEN" or "EXPIRED"))return Conflict(new{message="Chỉ gia hạn bài đang mở hoặc đã hết hạn."});
        if(await db.Claims.AnyAsync(x=>x.FoundReportId==reportId && x.Status=="APPROVED"))return Conflict(new{message="Bài đang trong quá trình bàn giao."});
        report.ExpiresAt=DateTime.UtcNow.AddDays(30);report.ExpirationNotifiedAt=null;report.Status="OPEN";report.UpdatedAt=DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="REPORT_RENEWED",TargetType="REPORT",TargetId=reportId});
        await db.SaveChangesAsync();await engine.MatchAsync(report.Id);return Ok(new{message="Đã gia hạn thêm 30 ngày."});
    }
}
