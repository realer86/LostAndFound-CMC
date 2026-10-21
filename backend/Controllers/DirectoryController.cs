using LostFound.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

// Public institutional directory only; NEVER return student email or personal numbers.
[ApiController, Route("api/directory")]
public class DirectoryController(LostFoundDb db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await db.Directories.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Category).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Location, x.InternalPhone, x.OfficeHours, x.Category })
            .ToListAsync();
        return Ok(new { items });
    }
}
