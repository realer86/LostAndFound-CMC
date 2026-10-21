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
            .Where(c => c.OwnerId == uid || c.VisitorId == uid)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Take(100).ToListAsync();
        return Ok(new { items = list.Select(c => SafeContact(c, uid)) });
    }

    [HttpPost]
    public async Task<IActionResult> Open(OpenContactRequest req)
    {
        var uid = UserId;
        var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.ReportId);
        if (report is null || report.Status != "OPEN")
            return NotFound(new { message = "Bài đăng không còn khả dụng." });
        if (report.UserId == uid)
            return BadRequest(new { message = "Không thể tự liên hệ bài đăng của mình." });

        var existing = await db.Contacts.Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.ReportId == req.ReportId && c.VisitorId == uid);
        if (existing is not null) return Ok(SafeContact(existing, uid));

        var contact = new ChatContact { ReportId = report.Id, OwnerId = report.UserId, VisitorId = uid };
        db.Contacts.Add(contact);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Another request created the same (report, visitor) conversation simultaneously.
            db.Entry(contact).State = EntityState.Detached;
            var current = await db.Contacts.Include(c => c.Report)
                .FirstAsync(c => c.ReportId == req.ReportId && c.VisitorId == uid);
            return Ok(SafeContact(current, uid));
        }
        // Reload only the safe name/title fields, never return an AppUser entity or report private data.
        var created = await db.Contacts.AsNoTracking().Include(c => c.Report).FirstAsync(c => c.Id == contact.Id);
        return Created($"/api/contacts/{contact.Id}", SafeContact(created, uid));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var uid = UserId;
        var contact = await db.Contacts.AsNoTracking().Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.Id == id && (c.OwnerId == uid || c.VisitorId == uid));
        if (contact is null) return NotFound();
        return Ok(SafeContact(contact, uid));
    }
}
