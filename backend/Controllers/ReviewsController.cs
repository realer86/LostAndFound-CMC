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
// Public responses include the reviewer full name and public post title but never IDs, email, private evidence, or case IDs.
[ApiController, Route("api/reviews")]
public class ReviewsController(LostFoundDb db) : ControllerBase
{
    [AllowAnonymous, HttpGet("public")]
    public async Task<IActionResult> Public([FromQuery] int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        const int pageSize = 6;
        var approved = db.PublicReviews.AsNoTracking().Where(r => r.IsVisible);
        var count = await approved.CountAsync();
        var average = count == 0 ? (double?)null : Math.Round(await approved.AverageAsync(r => (double)r.Rating), 1);
        // Follow the completed handover to its FOUND post to display its actual public title.
        // Do not expose reviewer IDs, report IDs, emails or private ownership evidence.
        // Map historic role labels at read time without modifying existing reviews.
        var items = await (from review in approved
            join reviewer in db.Users.AsNoTracking() on review.ReviewerId equals reviewer.Id
            join room in db.HandoverRooms.AsNoTracking() on review.HandoverRoomId equals room.Id
            join claim in db.Claims.AsNoTracking() on room.ClaimId equals claim.Id
            join report in db.Reports.AsNoTracking() on claim.FoundReportId equals report.Id
            orderby review.CreatedAt descending, review.Id descending
            select new
            {
                review.Rating,
                review.Comment,
                review.CreatedAt,
                fullName = reviewer.FullName,
                itemName = report.ItemName,
                participantRole = review.ParticipantRole == "Người nhận đồ" ? "Người mất"
                    : review.ParticipantRole == "Người nhặt được" ? "Người nhặt"
                    : review.ParticipantRole
            }).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { count, average, items, page, pageSize });
    }

    // List only COMPLETED handovers for the currently signed-in user that have not been reviewed yet.
    [Authorize, HttpGet("eligible")]
    public async Task<IActionResult> Eligible([FromQuery] Guid? handoverRoomId = null)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var available = await (from room in db.HandoverRooms.AsNoTracking()
            join claim in db.Claims.AsNoTracking() on room.ClaimId equals claim.Id
            join report in db.Reports.AsNoTracking() on claim.FoundReportId equals report.Id
            where (handoverRoomId == null || room.Id == handoverRoomId)
                && room.Status == "COMPLETED" && room.CompletedAt != null && claim.HandedOverAt != null
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
            select new { participantRole = claim.ClaimantId == userId ? "Người mất" : "Người nhặt" })
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
