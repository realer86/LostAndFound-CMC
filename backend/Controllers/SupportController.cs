using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record SupportMessageRequest(string? Content);

internal static class SupportView
{
    public static object Thread(SupportThread t) => new
    {
        t.Id, t.CaseId, t.CustodyId, t.Department, t.CreatedAt, t.LastMessageAt,
        title = t.CaseId.HasValue ? "Báo cáo gửi quản trị viên" : t.Department switch
        {
            "LIBRARY" => "Ký gửi – Thư viện",
            "SECURITY" => "Ký gửi – Bảo vệ",
            "DORM" => "Ký gửi – Ký túc xá",
            _ => "Hỗ trợ ký gửi"
        },
        // Until department-specific accounts are provisioned, the Admin console handles the queue.
        routingNote = t.CaseId.HasValue ? "Admin phụ trách xử lý báo cáo" : "Yêu cầu chuyển đến quầy; nhân viên được phân quyền hoặc Admin sẽ phản hồi"
    };
    public static object Message(SupportMessage m, Guid owner) => new
    {
        m.Id, m.ThreadId, m.Content, m.CreatedAt,
        mine = m.SenderId == owner,
        senderType = m.SenderType
    };
}

// Student/lecturer support inbox: only conversations attached to cases they submitted
// or custody requests for their own reports; never direct chats with another student.
[ApiController, Authorize(AuthenticationSchemes=CookieAuthenticationDefaults.AuthenticationScheme), Route("api/support")]
public class SupportController(LostFoundDb db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("threads")]
    public async Task<IActionResult> Mine()
    {
        var id=UserId;
        var rows=await db.SupportThreads.AsNoTracking().Where(x=>x.UserId==id)
            .OrderByDescending(x=>x.LastMessageAt).Take(100).ToListAsync();
        return Ok(new{items=rows.Select(SupportView.Thread)});
    }

    [HttpGet("threads/{id:guid}/messages")]
    public async Task<IActionResult> History(Guid id)
    {
        var owner=UserId;
        if(!await db.SupportThreads.AnyAsync(x=>x.Id==id && x.UserId==owner))return NotFound();
        var messages=await db.SupportMessages.AsNoTracking().Where(x=>x.ThreadId==id)
            .OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Take(100).ToListAsync();
        messages.Reverse();
        return Ok(new{items=messages.Select(x=>SupportView.Message(x,owner))});
    }

    [HttpPost("threads/{id:guid}/messages")]
    public async Task<IActionResult> Send(Guid id,SupportMessageRequest request)
    {
        var owner=UserId;
        var thread=await db.SupportThreads.FirstOrDefaultAsync(x=>x.Id==id && x.UserId==owner);
        if(thread is null)return NotFound();
        var content=request.Content?.Trim()??"";
        if(content.Length is < 1 or > 1500)return BadRequest(new{message="Tin nhắn cần từ 1 đến 1500 ký tự."});
        var count=await db.SupportMessages.CountAsync(x=>x.SenderId==owner && x.CreatedAt>DateTime.UtcNow.AddMinutes(-1));
        if(count>=10)return StatusCode(429,new{message="Bạn gửi quá nhiều tin nhắn. Hãy thử lại sau một phút."});
        var message=new SupportMessage{ThreadId=id,SenderId=owner,SenderType="USER",Content=content};
        thread.LastMessageAt=message.CreatedAt;
        db.SupportMessages.Add(message);
        await db.SaveChangesAsync();
        return Ok(SupportView.Message(message,owner));
    }
}

// Restricted backoffice inbox. Other end users cannot read or reply to official support.
[ApiController, Authorize(AuthenticationSchemes="AdminCookie",Roles="ADMIN,STAFF"), Route("api/admin/support")]
public class AdminSupportController(LostFoundDb db) : ControllerBase
{
    private Guid AdminId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsStaff => User.IsInRole("STAFF");
    private async Task<string?> AssignedDepartment() =>
        (await db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==AdminId))?.DepartmentCode;

    [HttpGet("threads")]
    public async Task<IActionResult> Inbox([FromQuery]string? department=null)
    {
        var query=db.SupportThreads.AsNoTracking().AsQueryable();
        if(IsStaff)
        {
            var ownDepartment=await AssignedDepartment();
            if(ownDepartment is null)return Forbid();
            query=query.Where(x=>x.Department==ownDepartment);
        }
        if(!string.IsNullOrWhiteSpace(department))
        {
            var code=department.Trim().ToUpperInvariant();
            if(code is not ("ADMIN" or "SECURITY" or "LIBRARY" or "DORM"))return BadRequest(new{message="Bộ phận không hợp lệ."});
            query=query.Where(x=>x.Department==code);
        }
        var rows=await query.OrderByDescending(x=>x.LastMessageAt).Take(200).ToListAsync();
        var ids=rows.Select(x=>x.UserId).Distinct().ToArray();
        var people=await db.Users.AsNoTracking().Where(x=>ids.Contains(x.Id))
            .ToDictionaryAsync(x=>x.Id,x=>x.FullName);
        return Ok(new{items=rows.Select(t=>new{t.Id,t.UserId,userName=people.GetValueOrDefault(t.UserId,"Người dùng"),t.CaseId,t.CustodyId,t.Department,t.CreatedAt,t.LastMessageAt,
            title=t.CaseId.HasValue?"Báo cáo / tranh chấp": "Yêu cầu ký gửi"})});
    }

    [HttpGet("threads/{id:guid}/messages")]
    public async Task<IActionResult> History(Guid id)
    {
        var thread=await db.SupportThreads.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);
        if(thread is null || (IsStaff && thread.Department!=await AssignedDepartment()))return NotFound();
        var messages=await db.SupportMessages.AsNoTracking().Where(x=>x.ThreadId==id)
            .OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Take(100).ToListAsync();
        messages.Reverse();
        return Ok(new{items=messages.Select(x=>SupportView.Message(x,thread.UserId))});
    }

    [HttpPost("threads/{id:guid}/messages")]
    public async Task<IActionResult> Reply(Guid id,SupportMessageRequest request)
    {
        var thread=await db.SupportThreads.FirstOrDefaultAsync(x=>x.Id==id);
        if(thread is null || (IsStaff && thread.Department!=await AssignedDepartment()))return NotFound();
        var content=request.Content?.Trim()??"";
        if(content.Length is < 1 or > 1500)return BadRequest(new{message="Tin nhắn cần từ 1 đến 1500 ký tự."});
        var adminId=AdminId;
        if(await db.SupportMessages.CountAsync(x=>x.SenderId==adminId && x.CreatedAt>DateTime.UtcNow.AddMinutes(-1))>=20)
            return StatusCode(429,new{message="Vui lòng gửi chậm hơn."});
        var message=new SupportMessage{ThreadId=id,SenderId=adminId,SenderType=IsStaff?"STAFF":"ADMIN",Content=content};
        thread.LastMessageAt=message.CreatedAt;
        db.SupportMessages.Add(message);
        db.AuditLogs.Add(new AuditLog{ActorId=adminId,Action="SUPPORT_REPLY",TargetType="SUPPORT_THREAD",TargetId=id});
        db.Notifications.Add(new Notification{UserId=thread.UserId,Type="SUPPORT",Content="Bạn có phản hồi mới trong Hỗ trợ & Danh bạ."});
        await db.SaveChangesAsync();
        return Ok(SupportView.Message(message,thread.UserId));
    }
}
