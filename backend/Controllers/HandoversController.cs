using System.Data;
using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record SendHandoverMessage(string Content);
public record ProposeHandoverAppointment(string Place, DateTimeOffset MeetingAt);
public record RespondHandoverAppointment(bool Accept);

/// <summary>
/// Only the APPROVED claimant and the owner of the FOUND report can access a room.
/// Contact details and private ownership evidence are not returned by these endpoints.
/// </summary>
[Authorize, ApiController, Route("api/handovers")]
public class HandoversController(LostFoundDb db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly string[] SafePlaces = [
        "Quầy bảo vệ - Tòa A", "Quầy Lost & Found - Thư viện",
        "Phòng trực bảo vệ - Cổng chính", "Quầy trực ký túc xá"
    ];
    private sealed record Access(HandoverRoom Room, ClaimRequest Claim, ItemReport Found);

    // Do not grant ADMIN portal sessions access: the public cookie scheme is required.
    private async Task<Access?> AuthorizedRoom(Guid roomId)
    {
        var room = await db.HandoverRooms.FirstOrDefaultAsync(x => x.Id == roomId);
        if(room is null) return null;
        var claim = await db.Claims.FirstOrDefaultAsync(x => x.Id == room.ClaimId);
        if(claim is null || claim.Status != "APPROVED") return null;
        var found = await db.Reports.FirstOrDefaultAsync(x => x.Id == claim.FoundReportId);
        if(found is null || (UserId != found.UserId && UserId != claim.ClaimantId)) return null;
        return new Access(room, claim, found);
    }
    private async Task<object> Overview(Access room)
    {
        var holder = await db.Users.AsNoTracking().FirstAsync(x => x.Id == room.Found.UserId);
        var claimant = await db.Users.AsNoTracking().FirstAsync(x => x.Id == room.Claim.ClaimantId);
        var appointments = await db.HandoverAppointments.AsNoTracking()
            .Where(x => x.RoomId == room.Room.Id).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var accepted = appointments.FirstOrDefault(x => x.Status == "ACCEPTED");
        var pending = appointments.FirstOrDefault(x => x.Status == "PROPOSED");
        return new {
            room.Room.Id, room.Room.ClaimId, room.Room.Status, room.Room.HolderConfirmedAt,
            room.Room.ClaimantConfirmedAt, room.Room.CompletedAt,
            itemName = room.Found.ItemName,
            holder = new { holder.Id, holder.FullName, verified = holder.EmailVerifiedAt != null },
            claimant = new { claimant.Id, claimant.FullName, verified = claimant.EmailVerifiedAt != null },
            myRole = UserId == room.Found.UserId ? "HOLDER" : "CLAIMANT",
            acceptedAppointment = accepted is null ? null : new { accepted.Id,accepted.Place,accepted.MeetingAt,accepted.Status },
            pendingAppointment = pending is null ? null : new { pending.Id,pending.Place,pending.MeetingAt,pending.Status,pending.ProposedById },
            safePlaces = SafePlaces
        };
    }
    private void Push(Guid receiverId, Guid reportId, string message, string type="HANDOVER") =>
        db.Notifications.Add(new Notification { UserId=receiverId, ReportId=reportId, Type=type, Content=message });

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var ids = await db.Claims.AsNoTracking()
            .Join(db.Reports,c => c.FoundReportId,r => r.Id,(c,r) => new { c, r })
            .Where(x => x.c.Status == "APPROVED" &&
                (x.c.ClaimantId == UserId || x.r.UserId == UserId))
            .Select(x => x.c.Id).ToListAsync();
        var rooms = await db.HandoverRooms.AsNoTracking().Where(x => ids.Contains(x.ClaimId))
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        var result = new List<object>();
        foreach(var r in rooms)
        {
            var claim = await db.Claims.AsNoTracking().FirstAsync(x=>x.Id==r.ClaimId);
            var found = await db.Reports.AsNoTracking().FirstAsync(x=>x.Id==claim.FoundReportId);
            result.Add(await Overview(new Access(r,claim,found)));
        }
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var access=await AuthorizedRoom(id);
        return access is null ? NotFound(new {message="Không tìm thấy phòng bàn giao của bạn."}) : Ok(await Overview(access));
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> Messages(Guid id)
    {
        if(await AuthorizedRoom(id) is null)return NotFound();
        var items=await db.HandoverMessages.AsNoTracking().Where(x=>x.RoomId==id)
            .OrderByDescending(x=>x.CreatedAt).Take(100).OrderBy(x=>x.CreatedAt)
            .Select(x=>new {x.Id,x.SenderId,x.Content,x.CreatedAt}).ToListAsync();
        return Ok(items);
    }
    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, SendHandoverMessage req)
    {
        var access=await AuthorizedRoom(id);
        if(access is null)return NotFound();
        if(access.Room.Status is "COMPLETED" or "CANCELLED" or "DISPUTED")return Conflict(new{message="Vụ việc đã hoàn tất, phòng chat ở chế độ chỉ đọc."});
        var message=req.Content?.Trim()??"";
        if(message.Length<1 || message.Length>1500)return BadRequest(new{message="Tin nhắn dài từ 1 đến 1500 ký tự."});
        var sent=await db.HandoverMessages.CountAsync(x=>x.RoomId==id && x.SenderId==UserId && x.CreatedAt>DateTime.UtcNow.AddMinutes(-1));
        if(sent>=15)return StatusCode(429,new{message="Bạn gửi tin nhắn quá nhanh. Vui lòng thử lại sau."});
        var other=access.Found.UserId==UserId?access.Claim.ClaimantId:access.Found.UserId;
        var item=new HandoverMessage{RoomId=id,SenderId=UserId,Content=message};
        db.HandoverMessages.Add(item);
        Push(other,access.Found.Id,$"Bạn có tin nhắn mới trong Phòng bàn giao: {access.Found.ItemName}.","MESSAGE");
        await db.SaveChangesAsync();
        return Ok(new{item.Id,item.SenderId,item.Content,item.CreatedAt});
    }

    [HttpPost("{id:guid}/appointments")]
    public async Task<IActionResult> Propose(Guid id,ProposeHandoverAppointment req)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var access=await AuthorizedRoom(id);
        if(access is null)return NotFound();
        if(access.Room.Status is "COMPLETED" or "CANCELLED" or "DISPUTED" || access.Room.HolderConfirmedAt!=null || access.Room.ClaimantConfirmedAt!=null)
            return Conflict(new{message="Không thể thay lịch khi đã xác nhận bàn giao."});
        if(!SafePlaces.Contains(req.Place))return BadRequest(new{message="Vui lòng chọn điểm bàn giao công cộng có trong danh sách."});
        var when=req.MeetingAt.UtcDateTime;
        if(when<=DateTime.UtcNow.AddMinutes(15) || when>DateTime.UtcNow.AddDays(30))
            return BadRequest(new{message="Lịch hẹn phải sau ít nhất 15 phút và trong 30 ngày tới."});
        var previous=await db.HandoverAppointments.Where(x=>x.RoomId==id && x.Status=="PROPOSED").ToListAsync();
        foreach(var other in previous)other.Status="SUPERSEDED";
        var proposal=new HandoverAppointment{RoomId=id,ProposedById=UserId,Place=req.Place,MeetingAt=when};
        db.HandoverAppointments.Add(proposal);
        var receiver=access.Found.UserId==UserId?access.Claim.ClaimantId:access.Found.UserId;
        Push(receiver,access.Found.Id,$"Có đề xuất lịch gặp mới cho {access.Found.ItemName}. Vào Phòng bàn giao để xác nhận.","APPOINTMENT");
        await db.SaveChangesAsync();await tx.CommitAsync();
        return Ok(new{proposal.Id,message="Đã gửi đề xuất lịch hẹn. Chờ đối phương đồng ý."});
    }

    [HttpPatch("{id:guid}/appointments/{appointmentId:guid}")]
    public async Task<IActionResult> Respond(Guid id,Guid appointmentId,RespondHandoverAppointment req)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var access=await AuthorizedRoom(id);
        if(access is null)return NotFound();
        if(access.Room.Status is "COMPLETED" or "CANCELLED" or "DISPUTED")return Conflict(new{message="Phòng đã hoàn tất."});
        var appt=await db.HandoverAppointments.FirstOrDefaultAsync(x=>x.Id==appointmentId&&x.RoomId==id);
        if(appt is null)return NotFound();
        if(appt.Status!="PROPOSED")return Conflict(new{message="Lịch này đã được xử lý."});
        if(appt.ProposedById==UserId)return BadRequest(new{message="Người đề xuất không thể tự chấp nhận lịch."});
        if(access.Room.HolderConfirmedAt!=null || access.Room.ClaimantConfirmedAt!=null)
            return Conflict(new{message="Đang xác nhận bàn giao, không thể thay đổi lịch."});
        appt.Status=req.Accept?"ACCEPTED":"DECLINED";appt.RespondedAt=DateTime.UtcNow;
        if(req.Accept)
        {
            var older=await db.HandoverAppointments.Where(x=>x.RoomId==id && x.Id!=appointmentId && x.Status=="ACCEPTED").ToListAsync();
            foreach(var x in older)x.Status="SUPERSEDED";
            access.Room.Status="SCHEDULED";
        }
        Push(appt.ProposedById,access.Found.Id,req.Accept?$"Đối phương đã đồng ý lịch nhận {access.Found.ItemName}.":$"Đối phương từ chối lịch nhận {access.Found.ItemName}. Hãy đề xuất lịch khác.","APPOINTMENT");
        await db.SaveChangesAsync();await tx.CommitAsync();
        return Ok(new{message=req.Accept?"Đã thống nhất lịch gặp.":"Đã từ chối lịch gặp."});
    }

    [HttpPatch("{id:guid}/appointments/{appointmentId:guid}/cancel")]
    public async Task<IActionResult> CancelAppointment(Guid id,Guid appointmentId)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var access=await AuthorizedRoom(id);
        if(access is null)return NotFound();
        if(access.Room.Status is "COMPLETED" or "CANCELLED" or "DISPUTED" || access.Room.HolderConfirmedAt!=null || access.Room.ClaimantConfirmedAt!=null)
            return Conflict(new{message="Không thể hủy lịch khi đang tranh chấp hoặc có xác nhận trao trả."});
        var appointment=await db.HandoverAppointments.FirstOrDefaultAsync(x=>x.Id==appointmentId&&x.RoomId==id);
        if(appointment is null)return NotFound();
        if(appointment.Status is not ("ACCEPTED" or "PROPOSED"))return Conflict(new{message="Lịch hẹn không còn hiệu lực."});
        appointment.Status="CANCELLED";
        access.Room.Status="ARRANGING";
        var other=access.Found.UserId==UserId?access.Claim.ClaimantId:access.Found.UserId;
        Push(other,access.Found.Id,$"Lịch gặp cho '{access.Found.ItemName}' đã bị hủy. Hãy thống nhất lịch mới.","APPOINTMENT");
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="APPOINTMENT_CANCELLED",TargetType="APPOINTMENT",TargetId=appointmentId});
        await db.SaveChangesAsync();await tx.CommitAsync();return Ok(new{message="Đã hủy lịch. Hai bên có thể đề xuất lại."});
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmPhysicalHandover(Guid id)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var access=await AuthorizedRoom(id);
        if(access is null)return NotFound();
        if(access.Room.Status is "COMPLETED" or "CANCELLED" or "DISPUTED")return Conflict(new{message="Vụ việc đã hoàn tất."});
        if(access.Found.Status!="OPEN")return Conflict(new{message="Bài đồ nhặt được không còn hiệu lực."});
        var custody=await db.CustodyRecords.FirstOrDefaultAsync(x=>x.ReportId==access.Found.Id);
        if(custody is not null && custody.Status=="RECEIVED")return Conflict(new{message="Đồ đang do quầy bảo vệ giữ; cần nhân viên quầy xác nhận đã trao đồ trước."});
        if(custody is not null && custody.Status=="RELEASED" && UserId==access.Found.UserId)return Conflict(new{message="Phía người giữ đã được xác nhận bởi nhân viên quầy; chỉ người nhận cần xác nhận."});
        if(!await db.HandoverAppointments.AnyAsync(x=>x.RoomId==id && x.Status=="ACCEPTED"))
            return Conflict(new{message="Hai bên phải thống nhất lịch và địa điểm trước khi xác nhận bàn giao."});
        var now=DateTime.UtcNow;
        if(UserId==access.Found.UserId)
        {
            if(access.Room.HolderConfirmedAt!=null)return Conflict(new{message="Bạn đã xác nhận bàn giao rồi."});
            access.Room.HolderConfirmedAt=now;
        }
        else
        {
            if(access.Room.ClaimantConfirmedAt!=null)return Conflict(new{message="Bạn đã xác nhận nhận đồ rồi."});
            access.Room.ClaimantConfirmedAt=now;
        }
        var other=UserId==access.Found.UserId?access.Claim.ClaimantId:access.Found.UserId;
        if(access.Room.HolderConfirmedAt!=null && access.Room.ClaimantConfirmedAt!=null)
        {
            access.Room.Status="COMPLETED"; access.Room.CompletedAt=now;
            access.Claim.HandedOverAt=now; access.Found.Status="RETURNED"; access.Found.UpdatedAt=now;
            var pending=await db.Claims.Where(x=>x.FoundReportId==access.Found.Id && x.Id!=access.Claim.Id && x.Status=="PENDING").ToListAsync();
            foreach(var otherClaim in pending){otherClaim.Status="REJECTED";otherClaim.ReviewedAt=now;}
            // Matching MVP computes matches on demand; no persisted Matches table.
            // Do not close the claimant's unrelated LOST reports automatically.
            Push(other,access.Found.Id,$"Bàn giao {access.Found.ItemName} hoàn tất. Cả hai bên đã xác nhận.","RETURNED");
            db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="HANDOVER_COMPLETED",TargetType="HANDOVER",TargetId=id});
        }
        else
        {
            access.Room.Status="AWAITING_CONFIRMATION";
            Push(other,access.Found.Id,$"Đối phương đã xác nhận bàn giao {access.Found.ItemName}. Vui lòng xác nhận sau khi thực sự giao/nhận đồ.","HANDOVER");
        }
        await db.SaveChangesAsync();await tx.CommitAsync();
        return Ok(new{message=access.Room.Status=="COMPLETED"?"Cả hai đã xác nhận. Vụ việc hoàn tất.":"Đã ghi nhận xác nhận của bạn. Đang chờ đối phương."});
    }
}
