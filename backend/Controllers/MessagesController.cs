using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record SendChatMessageRequest(Guid ContactId, string? Content);

[ApiController, Authorize, Route("api/messages")]
public class MessagesController(LostFoundDb db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static object SafeMessage(ChatMessage m) => new
    {
        m.Id, m.ContactId, m.ReportId, m.SenderId, m.RecipientId, m.Content, m.CreatedAt
    };

    [HttpGet]
    public async Task<IActionResult> History([FromQuery] Guid contactId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (contactId == Guid.Empty) return BadRequest(new { message = "Thiếu contactId." });
        var uid = UserId;
        var allowed = await db.Contacts.AnyAsync(c => c.Id == contactId && (c.OwnerId == uid || c.VisitorId == uid) && c.Report != null && c.Report.Type == "LOST");
        if (!allowed) return NotFound(); // Don't reveal other people's conversations.
        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var total = await db.Messages.CountAsync(m => m.ContactId == contactId);
        // Most recent page returned in chronological display order.
        var recent = await db.Messages.AsNoTracking().Where(m => m.ContactId == contactId)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        recent.Reverse();
        return Ok(new { contactId, page, pageSize, total, items = recent.Select(SafeMessage) });
    }

    // Legacy archive is read-only. No direct message bypasses Claims or official support.
    [HttpPost]
    public IActionResult Send(SendChatMessageRequest _) => StatusCode(410,
        new { message = "Hộp thư người dùng cũ chỉ để lưu trữ. Hãy dùng Phòng bàn giao hoặc Hỗ trợ & Danh bạ." });
}
