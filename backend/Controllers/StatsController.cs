using LostFound.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

// Số liệu công khai, chỉ là các con số tổng hợp, không chứa dữ liệu cá nhân.
[ApiController, Route("api/stats")]
public class StatsController(LostFoundDb db) : ControllerBase
{
    [HttpGet("public"), ResponseCache(Duration = 60)]
    public async Task<IActionResult> Public()
    {
        // "Đã trả" = bàn giao hoàn tất, tức là cả hai bên đã xác nhận (HandedOverAt được ghi lúc đó).
        var returned = await db.Claims.AsNoTracking().CountAsync(x => x.HandedOverAt != null);
        var open = await db.Reports.AsNoTracking().CountAsync(x => x.Status == "OPEN");
        var members = await db.Users.AsNoTracking().CountAsync(x => x.Role == "USER" && x.EmailVerifiedAt != null);
        return Ok(new { returned, open, members });
    }
}
