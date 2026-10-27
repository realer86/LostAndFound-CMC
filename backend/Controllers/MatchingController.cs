using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

// No Claims, handover, Google Auth, background job or new database tables are needed.
[ApiController]
[Authorize]
[Route("api/items/{id:guid}/matches")]
[Route("api/reports/{id:guid}/matches")]
public class MatchingController(LostFoundDb db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Recommendations(Guid id, [FromQuery] int minScore = 50,
        [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        if (minScore is < 50 or > 100)
            return BadRequest(new { message = "minScore phải từ 50 đến 100." });
        limit = Math.Clamp(limit, 1, 20);
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var source = await db.Reports.AsNoTracking().Include(x => x.Location)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (source is null || source.UserId != userId) return NotFound(new { message = "Không tìm thấy bài đăng của bạn." });
        if (source.Status != "OPEN" || source.Type is not ("LOST" or "FOUND"))
            return Ok(new { reportId = id, threshold = minScore, matches = Array.Empty<object>() });

        var opposite = source.Type == "LOST" ? "FOUND" : "LOST";
        // For the coursework-scale dataset, calculate on demand; don't mutate the DB.
        // A capped candidate window avoids unbounded in-memory evaluation.
        var candidates = await db.Reports.AsNoTracking()
            .Include(x => x.Location).Include(x => x.Category).Include(x => x.Images)
            .Where(x => x.Type == opposite && x.Status == "OPEN" && x.UserId != userId)
            .OrderByDescending(x => x.EventTime).Take(1000).ToListAsync(cancellationToken);

        var matches = candidates.Select(report => new { report, result = ReportMatchingService.Evaluate(source, report) })
            .Where(x => x.result.Score >= minScore)
            .OrderByDescending(x => x.result.Score).ThenByDescending(x => x.result.Coverage)
            .ThenByDescending(x => x.report.EventTime).Take(limit)
            .Select(x => new
            {
                id = x.report.Id, score = x.result.Score, level = x.result.Level,
                coverage = x.result.Coverage, reasons = x.result.Reasons,
                report = new
                {
                    x.report.Id, x.report.Type, x.report.Status, x.report.ItemName,
                    x.report.Description, x.report.Color, x.report.Brand,
                    x.report.CategoryId, category = x.report.Category?.Name,
                    x.report.LocationId, location = x.report.Location?.Name,
                    x.report.LocationDetail, x.report.EventTime, x.report.CreatedAt,
                    x.report.UserId, images = x.report.Images.Select(y => y.Url).ToArray()
                }
            }).ToList();
        return Ok(new { reportId = id, threshold = minScore, matches });
    }
}
