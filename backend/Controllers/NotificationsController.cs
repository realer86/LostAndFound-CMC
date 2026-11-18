using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

[Authorize,ApiController,Route("api/notifications")]
public class NotificationsController(LostFoundDb db):ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]public async Task<IActionResult> List()=>Ok(await db.Notifications.AsNoTracking().Where(x=>x.UserId==UserId).OrderByDescending(x=>x.CreatedAt).Take(80).ToListAsync());
    [HttpGet("matches/unread")]
    public async Task<IActionResult> UnreadMatches()
    {
        var count=await db.Notifications.CountAsync(x=>x.UserId==UserId && x.Type=="MATCH" && !x.IsRead);
        return Ok(new{count});
    }
    [HttpPatch("matches/{reportId:guid}/read")]
    public async Task<IActionResult> ReadMatches(Guid reportId)
    {
        if(!await db.Reports.AnyAsync(x=>x.Id==reportId && x.UserId==UserId))
            return NotFound(new{message="Không tìm thấy bài đăng của bạn."});
        var notifications=await db.Notifications.Where(x=>x.UserId==UserId &&
            x.Type=="MATCH" && x.ReportId==reportId && !x.IsRead).ToListAsync();
        foreach(var notification in notifications)notification.IsRead=true;
        await db.SaveChangesAsync();
        return Ok(new{message="Đã xem các gợi ý mới."});
    }
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id)
    {
        var n=await db.Notifications.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId);
        if(n is null)return NotFound();n.IsRead=true;await db.SaveChangesAsync();return Ok(new {message="Đã đọc."});
    }
}
