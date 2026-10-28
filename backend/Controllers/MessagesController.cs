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
        var allowed = await db.Contacts.AnyAsync(c => c.Id == contactId && (c.OwnerId == uid || c.VisitorId == uid));
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

    [HttpPost]
    public async Task<IActionResult> Send(SendChatMessageRequest req)
    {
        var uid = UserId;
        var content = req.Content?.Trim() ?? "";
        if (content.Length is < 1 or > 1500)
            return BadRequest(new { message = "Nội dung tin nhắn phải dài 1–1500 ký tự." });
        var contact = await db.Contacts.Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.Id == req.ContactId && (c.OwnerId == uid || c.VisitorId == uid));
        if (contact is null) return NotFound();
        if (contact.Report?.Status != "OPEN")
            return Conflict(new { message = "Bài đăng đã đóng; cuộc trò chuyện chỉ cho phép đọc." });
        var sentRecently = await db.Messages.CountAsync(m => m.SenderId == uid && m.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));
        if (sentRecently >= 15)
            return StatusCode(429, new { message = "Bạn gửi tin nhắn quá nhanh, hãy thử lại sau." });

        var message = new ChatMessage
        {
            ContactId = contact.Id, ReportId = contact.ReportId, SenderId = uid,
            RecipientId = uid == contact.OwnerId ? contact.VisitorId : contact.OwnerId,
            Content = content, CreatedAt = DateTime.UtcNow
        };
        contact.LastMessageAt = message.CreatedAt;
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        return Created($"/api/messages?contactId={contact.Id}", SafeMessage(message));
    }
}
