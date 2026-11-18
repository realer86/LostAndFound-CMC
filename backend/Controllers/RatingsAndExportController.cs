using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LostFound.Api.Data;
using LostFound.Api.Domain;

namespace LostFound.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class RatingsAndExportController : ControllerBase
    {
        private readonly LostFoundDb _context;

        public RatingsAndExportController(LostFoundDb context)
        {
            _context = context;
        }

        // 1. Lấy danh sách đánh giá theo bài đăng
        [HttpGet("items/{itemId}/ratings")]
        public async Task<IActionResult> GetItemRatings(int itemId)
        {
            var ratings = await _context.PostRatings
                .Where(r => r.ItemId == itemId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.ItemId,
                    r.UserName,
                    r.Stars,
                    r.Comment,
                    r.CreatedAt
                })
                .ToListAsync();

            double averageStars = ratings.Any() ? Math.Round(ratings.Average(r => r.Stars), 1) : 0;

            return Ok(new
            {
                itemId,
                averageStars,
                totalRatings = ratings.Count,
                ratings
            });
        }

        // 2. Gửi đánh giá 1-5 sao dưới bài đăng
        public class CreateRatingDto
        {
            public string UserName { get; set; } = string.Empty;
            public int Stars { get; set; }
            public string? Comment { get; set; }
        }

        [HttpPost("items/{itemId}/ratings")]
        public async Task<IActionResult> AddOrUpdateRating(int itemId, [FromBody] CreateRatingDto dto)
        {
            if (dto.Stars < 1 || dto.Stars > 5)
                return BadRequest(new { message = "Số sao phải từ 1 đến 5." });

            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest(new { message = "Vui lòng nhập tên người dùng đánh giá." });

            var cleanUserName = dto.UserName.Trim();

            var existingRating = await _context.PostRatings
                .FirstOrDefaultAsync(r => r.ItemId == itemId && r.UserName.ToLower() == cleanUserName.ToLower());

            if (existingRating != null)
            {
                existingRating.Stars = dto.Stars;
                existingRating.Comment = dto.Comment?.Trim();
                existingRating.CreatedAt = DateTime.UtcNow;
                _context.PostRatings.Update(existingRating);
            }
            else
            {
                var newRating = new PostRating
                {
                    ItemId = itemId,
                    UserName = cleanUserName,
                    Stars = dto.Stars,
                    Comment = dto.Comment?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.PostRatings.Add(newRating);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đánh giá thành công!" });
        }

        // 3. Xuất file CSV các món đồ đã được trả từ bảng Reports
        [HttpGet("items/export-returned")]
        public async Task<IActionResult> ExportReturnedItems()
        {
            var allReports = await _context.Reports.ToListAsync();

            var returnedReports = allReports
                .Where(r =>
                {
                    var statusVal = r.GetType().GetProperty("Status")?.GetValue(r)?.ToString()?.ToLower() ?? "";
                    return statusVal == "returned"
                        || statusVal == "completed"
                        || statusVal == "handedover"
                        || statusVal == "handed_over"
                        || statusVal == "closed"
                        || statusVal.Contains("đã trả");
                })
                .ToList();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("STT,Ma Bai Dang (ID),Tieu De / Ten Do Vat,Trang Thai,Ngay Tao");

            int index = 1;
            foreach (var item in returnedReports)
            {
                var id = item.GetType().GetProperty("Id")?.GetValue(item)?.ToString() ?? "";
                var title = item.GetType().GetProperty("Title")?.GetValue(item)?.ToString()
                         ?? item.GetType().GetProperty("ItemName")?.GetValue(item)?.ToString()
                         ?? "Không tiêu đề";
                var createdAtObj = item.GetType().GetProperty("CreatedAt")?.GetValue(item);
                string dateStr = createdAtObj is DateTime dt
                    ? dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                    : DateTime.Now.ToString("dd/MM/yyyy");

                csvBuilder.AppendLine($"{index++},\"{id}\",\"{title.Replace("\"", "\"\"")}\",\"Đã trao trả\",\"{dateStr}\"");
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var finalBytes = preamble.Concat(csvBytes).ToArray();

            return File(finalBytes, "text/csv; charset=utf-8", $"ThongKe_DoDaTra_CMC_{DateTime.Now:yyyyMMdd}.csv");
        }

        // 4. API Sửa bài đăng (PUT /api/reports/{id})
        public class UpdateReportDto
        {
            public string? ItemName { get; set; }
            public string? Description { get; set; }
            public string? Color { get; set; }
            public string? Brand { get; set; }
            public int? CategoryId { get; set; }
            public int? LocationId { get; set; }
            public string? LocationDetail { get; set; }
            public DateTime? EventTime { get; set; }
        }

        [HttpPut("reports/{id:guid}")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateReportDto dto)
        {
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
                return NotFound(new { message = "Không tìm thấy bài đăng cần sửa." });

            if (!string.IsNullOrWhiteSpace(dto.ItemName))
                report.ItemName = dto.ItemName.Trim();

            report.Description = dto.Description?.Trim();
            report.Color = dto.Color?.Trim();
            report.Brand = dto.Brand?.Trim();
            report.CategoryId = dto.CategoryId;
            report.LocationId = dto.LocationId;
            report.LocationDetail = dto.LocationDetail?.Trim();

            if (dto.EventTime.HasValue)
                report.EventTime = dto.EventTime.Value.ToUniversalTime();

            var updatedAtProp = report.GetType().GetProperty("UpdatedAt");
            if (updatedAtProp != null && updatedAtProp.CanWrite)
                updatedAtProp.SetValue(report, DateTime.UtcNow);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật bài đăng thành công!", id = report.Id });
        }

        // 5. API Đóng bài đăng (PATCH /api/reports/{id}/close)
        [HttpPatch("reports/{id:guid}/close")]
        public async Task<IActionResult> CloseReport(Guid id)
        {
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (report == null)
                return NotFound(new { message = "Không tìm thấy bài đăng." });

            report.Status = "CLOSED";

            var updatedAtProp = report.GetType().GetProperty("UpdatedAt");
            if (updatedAtProp != null && updatedAtProp.CanWrite)
                updatedAtProp.SetValue(report, DateTime.UtcNow);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã đóng bài đăng thành công." });
        }
    }
}