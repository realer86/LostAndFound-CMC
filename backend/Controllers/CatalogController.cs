using LostFound.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

[ApiController, Route("api")]
public class CatalogController(LostFoundDb db) : ControllerBase
{
    // Tách từ GET /api/reports/catalog của bản Final thành 2 API cho Issue #6.
    [HttpGet("categories")]
    public async Task<IActionResult> Categories() => Ok(await db.Categories.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet("locations")]
    public async Task<IActionResult> Locations() => Ok(await db.Locations.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    // Giữ response gộp để tương thích mã Form gốc.
    [HttpGet("items/catalog")]
    [HttpGet("reports/catalog")]
    public async Task<IActionResult> Catalog() => Ok(new {
        categories = await db.Categories.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        locations = await db.Locations.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
    });
}
