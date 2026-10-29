using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LostFound.Api.Controllers;

public record OpenContactRequest(Guid ReportId);

// Issue #13. A conversation always belongs to a report owner and ONE reader.
// Authenticated identities come from the cookie; callers cannot spoof sender/recipient.
[ApiController, Authorize, Route("api/contacts")]
public class ContactsController(LostFoundDb db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static object SafeContact(ChatContact c, Guid viewerId) => new
    {
        c.Id, c.ReportId, reportTitle = c.Report!.ItemName,
        otherUserId = viewerId == c.OwnerId ? c.VisitorId : c.OwnerId,
        otherDisplayName = viewerId == c.OwnerId ? "Người liên hệ" : "Người đăng tin",
        c.CreatedAt, c.LastMessageAt
    };

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var uid = UserId;
        var list = await db.Contacts.AsNoTracking()
            .Include(c => c.Report)
            .Where(c => (c.OwnerId == uid || c.VisitorId == uid) && c.Report != null && c.Report.Type == "LOST")
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Take(100).ToListAsync();
        return Ok(new { items = list.Select(c => SafeContact(c, uid)) });
    }

    // Deprecated endpoint intentionally refuses new peer-to-peer contacts.
    // Historical rows remain available through read-only endpoints for preservation.
    [HttpPost]
    public IActionResult Open(OpenContactRequest _) => StatusCode(410,
        new { message = "Hộp thư giữa người dùng đã ngừng. Vui lòng sử dụng Phòng bàn giao hoặc Hỗ trợ & Danh bạ." });

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var uid = UserId;
        var contact = await db.Contacts.AsNoTracking().Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.Id == id && (c.OwnerId == uid || c.VisitorId == uid) && c.Report != null && c.Report.Type == "LOST");
        if (contact is null) return NotFound();
        return Ok(SafeContact(contact, uid));
    }
}
