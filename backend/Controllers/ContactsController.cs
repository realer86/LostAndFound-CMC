using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Services;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

/// <summary>
/// Issue #13: pseudonymous users (only shared report/claim/message) and internal
/// security/management office directory, never real phone/email of students.
/// </summary>
[Authorize, ApiController, Route("api/contacts"), Route("contacts")]
public sealed class ContactsController(LostFoundDb db) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static object OfficeView(OfficeDirectoryEntry entry) => new
    {
        entry.Id, entry.Type, entry.Name, entry.Location,
        entry.InternalPhone, entry.WorkingSchedule, entry.DutiesByTime
    };

    [HttpGet("directory")]
    public async Task<IActionResult> Directory(CancellationToken ct)
    {
        var entries = await db.OfficeDirectory.AsNoTracking()
            .OrderBy(x => x.Type).ThenBy(x => x.Name).ToListAsync(ct);
        return Ok(entries.Select(OfficeView));
    }

    // GET /api/contacts -> { people: [...], offices: [...] }
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var me = CurrentUserId;
        // Only participants in the same report chat can appear as contacts.
        var fromMessages = await (from m in db.Messages.AsNoTracking()
            join r in db.Reports.AsNoTracking() on m.ReportId equals r.Id
            where m.SenderId == me || m.RecipientId == me
            select new
            {
                ItemId = r.Id, r.ItemName,
                OtherUserId = m.SenderId == me ? m.RecipientId : m.SenderId
            }).Distinct().ToListAsync(ct);

        // The participant linked by a claim can be contacted even before first chat.
        var fromClaims = await (from c in db.Claims.AsNoTracking()
            join r in db.Reports.AsNoTracking() on c.ItemId equals r.Id
            where (r.UserId == me || c.ClaimerId == me) && c.Status != "REJECTED"
            select new
            {
                ItemId = r.Id, r.ItemName,
                OtherUserId = r.UserId == me ? c.ClaimerId : r.UserId
            }).Distinct().ToListAsync(ct);

        var people = fromMessages.Concat(fromClaims)
            .Where(x => x.OtherUserId != me)
            .GroupBy(x => new { x.ItemId, x.OtherUserId })
            .Select(g => new
            {
                g.Key.ItemId,
                ItemName = g.First().ItemName,
                g.Key.OtherUserId,
                alias = PrivacyGuard.Alias(g.Key.OtherUserId)
            })
            .OrderBy(x => x.ItemName).Take(200).ToArray();

        var offices = await db.OfficeDirectory.AsNoTracking()
            .OrderBy(x => x.Type).ThenBy(x => x.Name).ToListAsync(ct);
        return Ok(new { people, offices = offices.Select(OfficeView) });
    }
}
