using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using LostFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public sealed record SendAnonymousMessageRequest(Guid ItemId, Guid RecipientId, string? Content);

/// <summary>
/// Issue #13: Message belongs to ONE report and ONE pair (author / interested user).
/// No public email, mobile, FullName or PrivateIdentifier fields are projected.
/// </summary>
[Authorize, ApiController, Route("api/messages"), Route("messages")]
public sealed class MessagesController(LostFoundDb db) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static object MessageDto(AnonymousMessage x) => new
    {
        x.Id, ItemId = x.ReportId, x.SenderId, x.RecipientId,
        senderAlias = PrivacyGuard.Alias(x.SenderId),
        recipientAlias = PrivacyGuard.Alias(x.RecipientId),
        x.Content, x.CreatedAt
    };

    // POST /api/messages
    [HttpPost]
    public async Task<IActionResult> Send(SendAnonymousMessageRequest request, CancellationToken ct)
    {
        var content = request.Content?.Trim() ?? "";
        if (request.ItemId == Guid.Empty || request.RecipientId == Guid.Empty ||
            content.Length < 1 || content.Length > 2000)
            return BadRequest(new { message = "ItemId, RecipientId và Content (1–2000 ký tự) là bắt buộc." });
        if (request.RecipientId == CurrentUserId)
            return BadRequest(new { message = "Không thể nhắn tin cho chính mình." });
        if (PrivacyGuard.ContainsPersonalContact(content))
            return BadRequest(new { message = "Vui lòng không gửi email hoặc số điện thoại cá nhân qua liên hệ ẩn danh." });

        var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ItemId, ct);
        if (report is null) return NotFound(new { message = "Không tìm thấy bài đăng." });
        if (report.Status != "OPEN") return Conflict(new { message = "Bài đăng không còn mở để nhắn tin." });
        if (!await db.Users.AnyAsync(x => x.Id == request.RecipientId, ct))
            return NotFound(new { message = "Không tìm thấy người nhận." });

        var senderIsOwner = report.UserId == CurrentUserId;
        var recipientIsOwner = report.UserId == request.RecipientId;
        if (senderIsOwner == recipientIsOwner)
            return Forbid(); // Only owner ↔ an interested member, never strangers ↔ strangers.

        // Once a claim has been approved, only that claimant may send *new* messages.
        var approved = await db.Claims.AsNoTracking()
            .Where(x => x.ItemId == report.Id && x.Status == "APPROVED")
            .Select(x => (Guid?)x.ClaimerId).FirstOrDefaultAsync(ct);
        var other = senderIsOwner ? request.RecipientId : CurrentUserId;
        if (approved.HasValue && approved.Value != other) return Forbid();
        if (senderIsOwner && !approved.HasValue)
        {
            // Owner can only reply to a person who asked about this item or sent a claim.
            var hasClaim = await db.Claims.AnyAsync(x => x.ItemId == report.Id &&
                x.ClaimerId == other && x.Status != "REJECTED", ct);
            var previouslyContacted = await db.Messages.AnyAsync(x => x.ReportId == report.Id &&
                x.SenderId == other && x.RecipientId == CurrentUserId, ct);
            if (!hasClaim && !previouslyContacted) return Forbid();
        }

        // Avoid accidental spam in this demo (20 messages / minute / sender).
        if (await db.Messages.CountAsync(x => x.SenderId == CurrentUserId &&
            x.CreatedAt >= DateTime.UtcNow.AddMinutes(-1), ct) >= 20)
            return StatusCode(429, new { message = "Bạn gửi tin quá nhanh, hãy thử lại sau." });

        var message = new AnonymousMessage
        {
            ReportId = report.Id, SenderId = CurrentUserId,
            RecipientId = request.RecipientId, Content = content
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
        return Created($"/api/messages?itemId={report.Id}&otherUserId={other}", MessageDto(message));
    }

    // GET /api/messages?itemId=...&otherUserId=...&page=1&pageSize=30
    [HttpGet]
    public async Task<IActionResult> Thread(Guid itemId, Guid otherUserId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        if (itemId == Guid.Empty || otherUserId == Guid.Empty || otherUserId == CurrentUserId)
            return BadRequest(new { message = "Cần ItemId và OtherUserId hợp lệ." });
        var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == itemId, ct);
        if (report is null) return NotFound();
        if (report.UserId != CurrentUserId && report.UserId != otherUserId)
            return Forbid(); // only one side is the report owner

        // Query strictly for this pair; other users' conversations and claim evidence stay private.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Messages.AsNoTracking().Where(x => x.ReportId == itemId &&
            ((x.SenderId == CurrentUserId && x.RecipientId == otherUserId) ||
             (x.SenderId == otherUserId && x.RecipientId == CurrentUserId)));
        var total = await query.CountAsync(ct);
        var messages = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        messages.Reverse();
        return Ok(new { page, pageSize, total, items = messages.Select(MessageDto) });
    }
}
