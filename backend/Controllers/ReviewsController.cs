using System.Security.Claims;
using Npgsql;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record SubmitFoundlyReview(Guid HandoverRoomId, int Rating, string? Comment);

// Reviews can only come from either participant of a COMPLETED handover.
// Public responses do not include stored user identities, email fields, private evidence, or case IDs.
[ApiController, Route("api/reviews")]
public class ReviewsController(LostFoundDb db) : ControllerBase
{
    [AllowAnonymous, HttpGet("public")]
    public async Task<IActionResult> Public()
    {
        var approved = db.PublicReviews.AsNoTracking().Where(r => r.IsVisible);
        var count = await approved.CountAsync();
        var average = count == 0 ? (double?)null : Math.Round(await approved.AverageAsync(r => (double)r.Rating), 1);
        var items = await approved.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Take(6)
            .Select(r => new { r.Rating, r.Comment, r.CreatedAt, r.ParticipantRole })
            .ToListAsync();
        return Ok(new { count, average, items });
    }

    // List only COMPLETED handovers for the currently signed-in user that have not been reviewed yet.
    [Authorize, HttpGet("eligible")]
    public async Task<IActionResult> Eligible()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var available = await (from room in db.HandoverRooms.AsNoTracking()
            join claim in db.Claims.AsNoTracking() on room.ClaimId equals claim.Id
            join report in db.Reports.AsNoTracking() on claim.FoundReportId equals report.Id
            where room.Status == "COMPLETED" && room.CompletedAt != null && claim.HandedOverAt != null
                && (claim.ClaimantId == userId || report.UserId == userId)
                && !db.PublicReviews.Any(r => r.HandoverRoomId == room.Id && r.ReviewerId == userId)
            orderby room.CompletedAt descending
            select new { handoverRoomId = room.Id, itemName = report.ItemName, completedAt = room.CompletedAt })
            .Take(20).ToListAsync();
        return Ok(available);
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Submit(SubmitFoundlyReview request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var comment = request.Comment?.Trim() ?? "";
        if (request.Rating is < 1 or > 5 || comment.Length < 10 || comment.Length > 500)
            return BadRequest(new { message = "Chọn 1–5 sao và viết nhận xét từ 10 đến 500 ký tự." });

        var handover = await (from room in db.HandoverRooms.AsNoTracking()
            join claim in db.Claims.AsNoTracking() on room.ClaimId equals claim.Id
            join report in db.Reports.AsNoTracking() on claim.FoundReportId equals report.Id
            where room.Id == request.HandoverRoomId && room.Status == "COMPLETED"
                && room.CompletedAt != null && claim.HandedOverAt != null
                && (claim.ClaimantId == userId || report.UserId == userId)
            select new { participantRole = claim.ClaimantId == userId ? "Người nhận đồ" : "Người nhặt được" })
            .FirstOrDefaultAsync();
        if (handover is null) return Forbid();
        if (await db.PublicReviews.AnyAsync(r => r.HandoverRoomId == request.HandoverRoomId && r.ReviewerId == userId))
            return Conflict(new { message = "Bạn đã đánh giá lần bàn giao này." });

        db.PublicReviews.Add(new FoundlyPublicReview
        {
            HandoverRoomId = request.HandoverRoomId,
            ReviewerId = userId,
            ParticipantRole = handover.participantRole,
            Rating = request.Rating,
            Comment = comment,
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Conflict(new { message = "Bạn đã đánh giá lần bàn giao này." }); }
        return Ok(new { message = "Cảm ơn bạn đã chia sẻ trải nghiệm bàn giao thực tế!" });
    }
}
