using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

// All endpoints use the *admin* cookie scheme, not the public website session.
[Authorize(AuthenticationSchemes="AdminCookie",Roles="ADMIN"),ApiController,Route("api/admin")]
public class AdminController(LostFoundDb db) : ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var cutoff=DateTime.UtcNow.Date.AddDays(-6);
        // Only seven days of timestamps; grouping in memory avoids provider-specific Date translation.
        var recentDates=await db.Reports.AsNoTracking().Where(x=>x.CreatedAt>=cutoff)
            .Select(x=>x.CreatedAt).ToListAsync();
        var daily=recentDates.GroupBy(t=>t.Date).ToDictionary(g=>g.Key,g=>g.Count());
        var days=Enumerable.Range(0,7).Select(i=>cutoff.AddDays(i))
            .Select(d=>new {date=d.ToString("yyyy-MM-dd"),count=daily.GetValueOrDefault(d)}).ToList();
        return Ok(new {
            users=await db.Users.CountAsync(),
            openReports=await db.Reports.CountAsync(x=>x.Status=="OPEN"),
            returned=await db.Reports.CountAsync(x=>x.Status=="RETURNED"),
            closed=await db.Reports.CountAsync(x=>x.Status=="CLOSED"),
            lostReports=await db.Reports.CountAsync(x=>x.Type=="LOST"),
            foundReports=await db.Reports.CountAsync(x=>x.Type=="FOUND"),
            matches=await db.Matches.CountAsync(x=>x.Status=="SUGGESTED"),
            pendingClaims=await db.Claims.CountAsync(x=>x.Status=="PENDING"),
            openCases=await db.CaseTickets.CountAsync(x=>x.Status=="OPEN"),
            custodyWaiting=await db.CustodyRecords.CountAsync(x=>x.Status=="REQUESTED"),
            custodyHeld=await db.CustodyRecords.CountAsync(x=>x.Status=="RECEIVED"),
            expiredReports=await db.Reports.CountAsync(x=>x.Status=="EXPIRED"),
            suspendedUsers=await db.Users.CountAsync(x=>x.SuspendedUntil!=null&&x.SuspendedUntil>DateTime.UtcNow),
            daily=days
        });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery]string? q,[FromQuery]string? type,[FromQuery]string? status,[FromQuery]int page=1)
    {
        page=Math.Clamp(page,1,10000);const int size=20;
        var query=db.Reports.AsNoTracking().AsQueryable();
        if(type is "LOST" or "FOUND")query=query.Where(x=>x.Type==type);
        if(status is "OPEN" or "CLOSED" or "RETURNED" or "EXPIRED" or "HIDDEN")query=query.Where(x=>x.Status==status);
        if(!string.IsNullOrWhiteSpace(q)) {var value=$"%{q.Trim()}%";query=query.Where(x=>EF.Functions.ILike(x.ItemName,value)||EF.Functions.ILike(x.Description,value));}
        var total=await query.CountAsync();
        var items=await query.Join(db.Users,r=>r.UserId,u=>u.Id,(r,u)=>new{
            r.Id,r.ItemName,r.Type,r.Status,r.EventTime,r.CreatedAt,r.UpdatedAt,r.UserId,
            owner=u.FullName,ownerEmail=u.Email
        }).OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync();
        return Ok(new{items,total,page,pageSize=size});
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery]string? q,[FromQuery]int page=1)
    {
        page=Math.Clamp(page,1,10000);const int size=20;
        var query=db.Users.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(q)){var val=$"%{q.Trim()}%";query=query.Where(x=>EF.Functions.ILike(x.FullName,val)||EF.Functions.ILike(x.Email,val));}
        var total=await query.CountAsync();
        var items=await query.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size)
            .Select(x=>new{x.Id,x.FullName,x.Email,x.Role,x.SuspendedUntil,verified=x.EmailVerifiedAt!=null,x.CreatedAt,
                reports=db.Reports.Count(r=>r.UserId==x.Id)}).ToListAsync();
        return Ok(new{items,total,page,pageSize=size});
    }

    // Admin can monitor claims without using the public owner's inbox.
    // Evidence is private and only exposed to authorized staff in this portal.
    [HttpGet("claims")]
    public async Task<IActionResult> Claims([FromQuery]string? status,[FromQuery]int page=1)
    {
        page=Math.Clamp(page,1,10000);const int size=20;
        var query=db.Claims.AsNoTracking().AsQueryable();
        if(status is "PENDING" or "APPROVED" or "REJECTED")query=query.Where(x=>x.Status==status);
        var total=await query.CountAsync();
        var items=await query.Join(db.Reports,c=>c.FoundReportId,r=>r.Id,(c,r)=>new{c,r})
            .Join(db.Users,x=>x.c.ClaimantId,u=>u.Id,(x,u)=>new{
                x.c.Id,x.c.Status,x.c.CreatedAt,x.c.ReviewedAt,x.c.HandedOverAt,
                x.c.Evidence,reportName=x.r.ItemName,x.c.FoundReportId,
                claimant=u.FullName,claimantEmail=u.Email
            }).OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync();
        return Ok(new{items,total,page,pageSize=size});
    }

    // Staff can view handover progress and identities but not private chat messages.
    [HttpGet("handovers")]
    public async Task<IActionResult> Handovers([FromQuery]int page=1)
    {
        page=Math.Clamp(page,1,10000); const int size=20;
        var query=db.HandoverRooms.AsNoTracking();
        var total=await query.CountAsync();
        var rooms=await query.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync();
        var result=new List<object>();
        foreach(var room in rooms)
        {
            var claim=await db.Claims.AsNoTracking().FirstAsync(x=>x.Id==room.ClaimId);
            var found=await db.Reports.AsNoTracking().FirstAsync(x=>x.Id==claim.FoundReportId);
            var holder=await db.Users.AsNoTracking().FirstAsync(x=>x.Id==found.UserId);
            var claimant=await db.Users.AsNoTracking().FirstAsync(x=>x.Id==claim.ClaimantId);
            result.Add(new{room.Id,room.Status,room.CreatedAt,room.CompletedAt,
                room.HolderConfirmedAt,room.ClaimantConfirmedAt, itemName=found.ItemName,
                holder=holder.FullName, claimant=claimant.FullName});
        }
        return Ok(new{items=result,total,page,pageSize=size});
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery]int page=1)
    {
        page=Math.Clamp(page,1,10000);const int size=25;
        var query=db.AuditLogs.AsNoTracking();
        var total=await query.CountAsync();
        var items=await query.Join(db.Users,a=>a.ActorId,u=>u.Id,(a,u)=>new{
            a.Id,a.Action,a.TargetType,a.TargetId,a.CreatedAt,actor=u.FullName,actorEmail=u.Email
        }).OrderByDescending(x=>x.CreatedAt).Skip((page-1)*size).Take(size).ToListAsync();
        return Ok(new{items,total,page,pageSize=size});
    }

    [HttpPatch("reports/{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        var item=await db.Reports.FindAsync(id);
        if(item is null)return NotFound(new{message="Không tìm thấy bài đăng."});
        if(item.Status!="OPEN")return Conflict(new{message="Chỉ có thể đóng bài đang OPEN."});
        if(await db.Claims.AnyAsync(c=>c.FoundReportId==id&&c.Status=="APPROVED") ||
           await db.CustodyRecords.AnyAsync(c=>c.ReportId==id&&c.Status=="RECEIVED"))
            return Conflict(new{message="Bài đang có yêu cầu bàn giao hoặc đồ ký gửi. Xử lý hồ sơ trước khi đóng."});
        item.Status="CLOSED";item.UpdatedAt=DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="ADMIN_CLOSE_REPORT",TargetType="REPORT",TargetId=id});
        await db.SaveChangesAsync();return Ok(new{message="Đã đóng bài đăng."});
    }

    [HttpPatch("reports/{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid id)
    {
        var item=await db.Reports.FindAsync(id);
        if(item is null)return NotFound(new{message="Không tìm thấy bài đăng."});
        if(item.Status is not ("CLOSED" or "HIDDEN" or "EXPIRED"))return Conflict(new{message="Chỉ mở lại bài đã đóng, ẩn hoặc hết hạn. Không thể mở lại đồ đã trao trả."});
        if(item.Type=="FOUND" && await db.Claims.AnyAsync(x=>x.FoundReportId==id&&x.Status=="APPROVED"))
            return Conflict(new{message="Bài có yêu cầu đã duyệt; cần xử lý yêu cầu trước khi mở lại."});
        item.Status="OPEN";item.ExpiresAt=DateTime.UtcNow.AddDays(30);item.ExpirationNotifiedAt=null;item.UpdatedAt=DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog{ActorId=UserId,Action="ADMIN_REOPEN_REPORT",TargetType="REPORT",TargetId=id});
        await db.SaveChangesAsync();return Ok(new{message="Đã mở lại bài đăng."});
    }
}
