using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

// ReportRequest, PublicReport, Check, Copy là những đoạn được tách từ
// api/Controllers/ReportsController.cs (Final). Route /items theo Issue #6.
public record ReportRequest(string Type, string ItemName, string? Description, string? Color,
    string? Brand, string? LocationDetail, int? CategoryId, int? LocationId,
    DateTime EventTime, string? PrivateIdentifier);

[ApiController, Route("api/items")]
[Route("api/reports")]
public class ItemsController(LostFoundDb db) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private bool Admin => User.IsInRole("ADMIN");

    private static object PublicReport(ItemReport x) => new {
        x.Id, x.Type, x.Status, x.ItemName, x.Description, x.Color, x.Brand, x.CategoryId,
        category = x.Category?.Name, x.LocationId, location = x.Location?.Name, x.LocationDetail,
        x.EventTime, x.CreatedAt, x.UpdatedAt, x.ExpiresAt, x.UserId,
        images = x.Images.Select(im => im.Url).ToArray()
    };

    private static string? Check(ReportRequest r)
    {
        if (r.Type is not ("LOST" or "FOUND")) return "Loại bài phải là LOST hoặc FOUND.";
        if (string.IsNullOrWhiteSpace(r.ItemName) || r.ItemName.Trim().Length > 140) return "Tên vật phẩm cần 1–140 ký tự.";
        if ((r.Description?.Length ?? 0) > 3500 || (r.PrivateIdentifier?.Length ?? 0) > 200 ||
            (r.LocationDetail?.Length ?? 0) > 300 || (r.Color?.Length ?? 0) > 80 || (r.Brand?.Length ?? 0) > 80)
            return "Một số nội dung vượt giới hạn ký tự.";
        if (r.EventTime == default || r.EventTime.ToUniversalTime() > DateTime.UtcNow.AddHours(2) ||
            r.EventTime.ToUniversalTime() < DateTime.UtcNow.AddYears(-5)) return "Thời gian không hợp lệ.";
        return null;
    }
    private static DateTime Utc(DateTime t) => t.Kind == DateTimeKind.Utc ? t :
        t.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(t, DateTimeKind.Utc) : t.ToUniversalTime();
    private static void Copy(ItemReport x, ReportRequest r)
    {
        x.Type = r.Type;
        x.ItemName = r.ItemName.Trim();
        x.Description = r.Description?.Trim() ?? "";
        x.Color = r.Color?.Trim();
        x.Brand = r.Brand?.Trim();
        x.LocationDetail = r.LocationDetail?.Trim();
        x.CategoryId = r.CategoryId;
        x.LocationId = r.LocationId;
        x.EventTime = Utc(r.EventTime);
        if (r.Type == "LOST") x.PrivateIdentifier = null;
        else if (!string.IsNullOrWhiteSpace(r.PrivateIdentifier)) x.PrivateIdentifier = r.PrivateIdentifier.Trim();
        x.UpdatedAt = DateTime.UtcNow;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? type,
        [FromQuery] int? categoryId, [FromQuery] int? locationId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 40);
        var query = db.Reports.AsNoTracking().Include(x => x.Images)
            .Include(x => x.Category).Include(x => x.Location)
            .Where(x => x.Status == "OPEN");
        if (type is "LOST" or "FOUND") query = query.Where(x => x.Type == type);
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId);
        if (locationId.HasValue) query = query.Where(x => x.LocationId == locationId);
        if (!string.IsNullOrWhiteSpace(q)) {
            var term = $"%{q.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.ItemName, term) ||
                EF.Functions.ILike(x.Description, term) || EF.Functions.ILike(x.LocationDetail ?? "", term));
        }
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items = items.Select(PublicReport) });
    }

    [Authorize, HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var items = await db.Reports.AsNoTracking().Include(x => x.Images)
            .Include(x => x.Location).Include(x => x.Category)
            .Where(x => x.UserId == UserId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(items.Select(PublicReport));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var r = await db.Reports.AsNoTracking().Include(x => x.Images)
            .Include(x => x.Location).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r is null || (r.Status != "OPEN" && r.UserId != UserId && !Admin))
            return NotFound(new { message = "Không tìm thấy bài." });
        return Ok(PublicReport(r));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Create(ReportRequest r)
    {
        var validation = Check(r); if (validation is not null) return BadRequest(new { message = validation });
        if (r.CategoryId.HasValue && !await db.Categories.AnyAsync(x => x.Id == r.CategoryId))
            return BadRequest(new { message = "Nhóm đồ không tồn tại." });
        if (r.LocationId.HasValue && !await db.Locations.AnyAsync(x => x.Id == r.LocationId))
            return BadRequest(new { message = "Địa điểm không tồn tại." });
        var entity = new ItemReport { UserId = UserId!.Value };
        Copy(entity, r);
        db.Reports.Add(entity);
        await db.SaveChangesAsync();
        // Bản Final gọi MatchEngine sau SaveChanges; matching không thuộc 3 Issue này.
        return Created($"/api/items/{entity.Id}",
            new { id = entity.Id, message = "Đăng tin thành công." });
    }
}
