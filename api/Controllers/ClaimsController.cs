using System.Data;
using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record CreateClaimRequest(Guid FoundReportId,string Evidence);
public record ReviewRequest(bool Approve);

[Authorize,ApiController,Route("api/claims")]
public class ClaimsController(LostFoundDb db) : ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Admin=>User.IsInRole("ADMIN");
    private static object View(ClaimRequest c,ItemReport? report=null)=>new{c.Id,c.FoundReportId,c.ClaimantId,c.Evidence,c.Status,c.CreatedAt,c.ReviewedAt,c.HandedOverAt,reportName=report?.ItemName};

    [HttpPost]
    public async Task<IActionResult> Create(CreateClaimRequest r)
    {
        if(string.IsNullOrWhiteSpace(r.Evidence)||r.Evidence.Trim().Length<12||r.Evidence.Length>4000)return BadRequest(new {message="Bằng chứng cần từ 12 đến 4000 ký tự."});
        var found=await db.Reports.FindAsync(r.FoundReportId);
        if(found is null||found.Type!="FOUND"||found.Status!="OPEN")return BadRequest(new {message="Bài nhặt đồ không còn nhận yêu cầu."});
        if(found.UserId==UserId)return BadRequest(new {message="Không thể tự yêu cầu nhận món đồ mình đang giữ."});
        var count=await db.Claims.CountAsync(x=>x.ClaimantId==UserId&&x.CreatedAt>DateTime.UtcNow.AddDays(-1));
        if(count>=10)return StatusCode(429,new {message="Bạn đã gửi quá nhiều yêu cầu trong ngày."});
        if(await db.Claims.AnyAsync(x=>x.ClaimantId==UserId&&x.FoundReportId==r.FoundReportId&&(x.Status=="PENDING"||x.Status=="APPROVED")))return Conflict(new {message="Bạn đã gửi yêu cầu đối với món đồ này."});
        var claim=new ClaimRequest{ClaimantId=UserId,FoundReportId=found.Id,Evidence=r.Evidence.Trim()};
        db.Claims.Add(claim);
        db.Notifications.Add(new Notification{UserId=found.UserId,Type="CLAIM",ReportId=found.Id,Content="Có yêu cầu nhận đồ mới. Hãy kiểm tra bằng chứng riêng tư."});
        await db.SaveChangesAsync();return Ok(new{id=claim.Id,message="Đã gửi yêu cầu xác minh."});
    }
    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var claims=await db.Claims.AsNoTracking().Where(x=>x.ClaimantId==UserId).OrderByDescending(x=>x.CreatedAt).ToListAsync();
        var ids=claims.Select(c=>c.FoundReportId).ToArray();
        var reports=await db.Reports.Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id);
        return Ok(claims.Select(c=>View(c,reports.GetValueOrDefault(c.FoundReportId))));
    }
    [HttpGet("inbox")]
    public async Task<IActionResult> Inbox()
    {
        var claims=await db.Claims.AsNoTracking().Join(db.Reports,x=>x.FoundReportId,r=>r.Id,(x,r)=>new{x,r})
            .Where(x=>x.r.UserId==UserId||Admin).OrderByDescending(x=>x.x.CreatedAt).Take(200).ToListAsync();
        return Ok(claims.Select(x=>View(x.x,x.r)));
    }
    [HttpPatch("{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id,ReviewRequest req)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var claim=await db.Claims.FirstOrDefaultAsync(x=>x.Id==id);
        if(claim is null)return NotFound();
        var found=await db.Reports.FindAsync(claim.FoundReportId);
        if(found is null)return NotFound();
        if(found.UserId!=UserId&&!Admin)return Forbid();
        if(claim.Status!="PENDING"||found.Status!="OPEN")return Conflict(new{message="Yêu cầu không còn chờ xét duyệt."});
        if(req.Approve&&await db.Claims.AnyAsync(x=>x.FoundReportId==found.Id&&x.Status=="APPROVED"))return Conflict(new {message="Đã có một yêu cầu được chấp thuận."});
        claim.Status=req.Approve?"APPROVED":"REJECTED";claim.ReviewedAt=DateTime.UtcNow;
        if(req.Approve && !await db.HandoverRooms.AnyAsync(x=>x.ClaimId==claim.Id))
            db.HandoverRooms.Add(new HandoverRoom{ClaimId=claim.Id});
        db.Notifications.Add(new Notification{UserId=claim.ClaimantId,Type="CLAIM",ReportId=found.Id,Content=req.Approve?"Yêu cầu của bạn đã được duyệt. Mở Phòng bàn giao để nhắn tin và thống nhất điểm hẹn an toàn.":"Yêu cầu nhận đồ chưa được chấp thuận."});
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action=req.Approve?"CLAIM_APPROVED":"CLAIM_REJECTED",TargetType="CLAIM",TargetId=claim.Id});
        await db.SaveChangesAsync();await transaction.CommitAsync();return Ok(new{message="Đã cập nhật yêu cầu."});
    }
    [HttpPost("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var claim=await db.Claims.FindAsync(id);
        if(claim is null)return NotFound();
        if(claim.ClaimantId!=UserId)return Forbid();
        if(claim.Status is not ("PENDING" or "APPROVED"))return Conflict(new{message="Yêu cầu này không thể rút."});
        var room=await db.HandoverRooms.FirstOrDefaultAsync(x=>x.ClaimId==id);
        if(room is not null && (room.Status is "DISPUTED" or "COMPLETED" ||room.HolderConfirmedAt!=null||room.ClaimantConfirmedAt!=null))
            return Conflict(new{message="Đã có xác nhận bàn giao hoặc tranh chấp. Cần quản trị viên xử lý."});
        claim.Status="CANCELLED";
        if(room is not null){room.Status="CANCELLED";foreach(var a in await db.HandoverAppointments.Where(x=>x.RoomId==room.Id && (x.Status=="ACCEPTED"||x.Status=="PROPOSED")).ToListAsync())a.Status="CANCELLED";}
        var found=await db.Reports.FindAsync(claim.FoundReportId);
        if(found is not null)db.Notifications.Add(new Notification{UserId=found.UserId,ReportId=found.Id,Type="CLAIM",Content="Người yêu cầu đã rút yêu cầu nhận đồ. Bạn có thể xem xét yêu cầu khác."});
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="CLAIM_WITHDRAWN",TargetType="CLAIM",TargetId=id});
        await db.SaveChangesAsync();await tx.CommitAsync();return Ok(new{message="Đã rút yêu cầu."});
    }
    public record RevokeRequest(string Reason);
    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id,RevokeRequest req)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var claim=await db.Claims.FindAsync(id);
        if(claim is null)return NotFound();
        var found=await db.Reports.FindAsync(claim.FoundReportId);
        if(found is null)return NotFound();
        if(found.UserId!=UserId&&!Admin)return Forbid();
        if(claim.Status!="APPROVED")return Conflict(new{message="Chỉ có thể hủy chấp thuận đang hoạt động."});
        if((req.Reason?.Trim().Length??0)<10)return BadRequest(new{message="Cần nêu lý do (ít nhất 10 ký tự)."});
        var room=await db.HandoverRooms.FirstOrDefaultAsync(x=>x.ClaimId==id);
        if(room is not null && (room.Status is "DISPUTED" or "COMPLETED" ||room.HolderConfirmedAt!=null ||room.ClaimantConfirmedAt!=null))
            return Conflict(new{message="Phòng đang tranh chấp/đã xác nhận nhận đồ; cần quản trị viên đối soát."});
        claim.Status="CANCELLED";
        if(room is not null){room.Status="CANCELLED";foreach(var a in await db.HandoverAppointments.Where(x=>x.RoomId==room.Id && (x.Status=="ACCEPTED"||x.Status=="PROPOSED")).ToListAsync())a.Status="CANCELLED";}
        db.Notifications.Add(new Notification{UserId=claim.ClaimantId,ReportId=found.Id,Type="CLAIM",Content=$"Yêu cầu đã duyệt bị hủy. Lý do: {req.Reason.Trim()[..Math.Min(140,req.Reason.Trim().Length)]}"});
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="CLAIM_APPROVAL_REVOKED",TargetType="CLAIM",TargetId=id});
        await db.SaveChangesAsync();await tx.CommitAsync();return Ok(new{message="Đã hủy chấp thuận. Có thể xem xét người khác."});
    }

    // Deprecated path intentionally cannot finish a handover on the finder's word alone.
    [HttpPost("{id:guid}/handover")]
    public IActionResult LegacyHandover(Guid id) => Conflict(new {message="Hãy dùng Phòng bàn giao và xác nhận từ cả hai phía sau khi gặp mặt."});
}
