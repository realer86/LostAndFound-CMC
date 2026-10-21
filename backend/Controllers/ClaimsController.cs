using System.Data;
using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace LostFound.Api.Controllers;

public sealed record CreateItemClaimRequest(Guid ItemId, string? SecretText);
public sealed record UpdateItemClaimStatusRequest(string? Status);

/// <summary>
/// Issue #17. Evidence is returned ONLY to the owner of the FOUND report.
/// The authenticated caller ID is taken from the cookie, never from request JSON.
/// </summary>
[Authorize, ApiController, Route("api/claims"), Route("claims")]
public sealed class ClaimsController(LostFoundDb db) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static object PublicClaim(ItemClaim c) => new
    {
        c.Id, ItemId = c.ItemId, c.ClaimerId, c.Status, c.CreatedAt, c.ReviewedAt
    };

    private static object OwnerClaim(ItemClaim c) => new
    {
        c.Id, ItemId = c.ItemId, c.ClaimerId, c.SecretText, c.Status, c.CreatedAt, c.ReviewedAt
    };

    // Both claim creation and approval lock the SAME Reports row before checking
    // conflicts. This serializes competing claim submissions / approvals per item.
    private async Task LockItemAsync(Guid reportId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT \"Id\" FROM \"Reports\" WHERE \"Id\" = @reportId FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "reportId";
        parameter.Value = reportId;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(ct);
    }

    // POST /api/claims
    [HttpPost]
    public async Task<IActionResult> Create(CreateItemClaimRequest request, CancellationToken ct)
    {
        var secret = request.SecretText?.Trim() ?? "";
        if (request.ItemId == Guid.Empty || secret.Length < 12 || secret.Length > 4000)
            return BadRequest(new { message = "ItemId hợp lệ và SecretText dài 12–4000 ký tự là bắt buộc." });

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockItemAsync(request.ItemId, ct);
        var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ItemId, ct);
        if (report is null) return NotFound(new { message = "Không tìm thấy bài đăng." });
        if (report.Type != "FOUND" || report.Status != "OPEN")
            return Conflict(new { message = "Chỉ được xin nhận bài FOUND còn mở." });
        if (report.UserId == CurrentUserId)
            return BadRequest(new { message = "Chủ bài không thể tự xin nhận đồ." });
        if (await db.Claims.AnyAsync(x => x.ItemId == report.Id && x.Status == "APPROVED", ct))
            return Conflict(new { message = "Món đồ đã có người được phê duyệt." });
        if (await db.Claims.AnyAsync(x => x.ItemId == report.Id && x.ClaimerId == CurrentUserId, ct))
            return Conflict(new { message = "Bạn đã gửi yêu cầu cho món đồ này." });

        var claim = new ItemClaim { ItemId = report.Id, ClaimerId = CurrentUserId, SecretText = secret };
        db.Claims.Add(claim);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                   { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "Yêu cầu trùng hoặc món đồ đã được phê duyệt." });
        }
        // SecretText deliberately omitted from the create response.
        return Created($"/api/claims/{claim.Id}", PublicClaim(claim));
    }

    // GET /api/claims/mine: user's own statuses, NO private evidence in response.
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var claims = await db.Claims.AsNoTracking()
            .Where(x => x.ClaimerId == CurrentUserId)
            .OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct);
        return Ok(claims.Select(PublicClaim));
    }

    // GET /api/claims/{id}: owner sees SecretText, claimant sees status only.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var claim = await db.Claims.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (claim is null) return NotFound();
        var ownerId = await db.Reports.AsNoTracking().Where(x => x.Id == claim.ItemId)
            .Select(x => (Guid?)x.UserId).FirstOrDefaultAsync(ct);
        if (ownerId == CurrentUserId) return Ok(OwnerClaim(claim));
        if (claim.ClaimerId == CurrentUserId) return Ok(PublicClaim(claim));
        return NotFound(); // No existence / evidence disclosure to bystanders.
    }

    // GET /api/items/{id}/claims: ONLY the author of this FOUND report.
    [HttpGet("/api/items/{id:guid}/claims")]
    [HttpGet("/items/{id:guid}/claims")]
    public async Task<IActionResult> ForItem(Guid id, CancellationToken ct)
    {
        var item = await db.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.UserId != CurrentUserId) return Forbid();
        var claims = await db.Claims.AsNoTracking().Where(x => x.ItemId == id)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Ok(claims.Select(OwnerClaim));
    }

    // PATCH /api/claims/{id}/status { "status": "APPROVED" | "REJECTED" }
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Review(Guid id, UpdateItemClaimStatusRequest request, CancellationToken ct)
    {
        var status = request.Status?.Trim().ToUpperInvariant();
        if (status is not ("APPROVED" or "REJECTED"))
            return BadRequest(new { message = "Status phải là APPROVED hoặc REJECTED." });

        // Look up the immutable ItemId to lock its report row.
        var reportId = await db.Claims.AsNoTracking().Where(x => x.Id == id)
            .Select(x => (Guid?)x.ItemId).FirstOrDefaultAsync(ct);
        if (reportId is null) return NotFound();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await LockItemAsync(reportId.Value, ct);
        var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == reportId, ct);
        if (report is null) return NotFound();
        if (report.UserId != CurrentUserId) return Forbid();
        if (report.Type != "FOUND" || report.Status != "OPEN")
            return Conflict(new { message = "Bài nhặt đồ không còn mở." });

        var claim = await db.Claims.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (claim is null) return NotFound();
        if (claim.Status != "PENDING")
            return Conflict(new { message = "Chỉ xử lý yêu cầu đang PENDING." });
        if (status == "APPROVED" && await db.Claims.AnyAsync(
                x => x.ItemId == claim.ItemId && x.Status == "APPROVED", ct))
            return Conflict(new { message = "Món đồ đã có người được phê duyệt." });

        var now = DateTime.UtcNow;
        claim.Status = status;
        claim.ReviewedAt = now;
        var rejectedOthers = 0;
        if (status == "APPROVED")
        {
            var otherPending = await db.Claims.Where(x => x.ItemId == claim.ItemId &&
                x.Id != claim.Id && x.Status == "PENDING").ToListAsync(ct);
            foreach (var other in otherPending)
            {
                other.Status = "REJECTED";
                other.ReviewedAt = now;
            }
            rejectedOthers = otherPending.Count;
        }

        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                   { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "Xung đột duyệt: đã có người được chấp thuận." });
        }
        return Ok(new { claim.Id, claim.Status, claim.ReviewedAt, automaticallyRejected = rejectedOthers });
    }
}
