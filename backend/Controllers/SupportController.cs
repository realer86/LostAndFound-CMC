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
        var threadIds=rows.Select(x=>x.Id).ToArray();
        var unreadIds=await db.SupportThreads.AsNoTracking()
            .Where(x=>threadIds.Contains(x.Id) && db.SupportMessages.Any(m=>
                m.ThreadId==x.Id && m.SenderId!=id && m.CreatedAt>x.UserLastReadAt))
            .Select(x=>x.Id).ToHashSetAsync();
        var items=new List<object>();
        foreach(var thread in rows)
        {
            items.Add(new
            {
                thread.Id, thread.CaseId, thread.CustodyId, thread.Department, thread.CreatedAt, thread.LastMessageAt,
                title=thread.CaseId.HasValue ? "Báo cáo gửi quản trị viên" : thread.Department switch
                {
                    "LIBRARY"=>"Ký gửi – Thư viện","SECURITY"=>"Ký gửi – Bảo vệ","DORM"=>"Ký gửi – Ký túc xá",_=>"Hỗ trợ ký gửi"
                },
                routingNote=thread.CaseId.HasValue ? "Admin phụ trách xử lý báo cáo" : "Yêu cầu chuyển đến quầy; nhân viên được phân quyền hoặc Admin sẽ phản hồi",
                hasUnread=unreadIds.Contains(thread.Id)
            });
        }
        return Ok(new{items});
    }

    [HttpGet("threads/{id:guid}/messages")]
    public async Task<IActionResult> History(Guid id)
    {
        var owner=UserId;
        var thread=await db.SupportThreads.FirstOrDefaultAsync(x=>x.Id==id && x.UserId==owner);
        if(thread is null)return NotFound();
        thread.UserLastReadAt=DateTime.UtcNow;
        var relatedNotifications=await db.Notifications.Where(x=>x.UserId==owner &&
            x.Type=="SUPPORT" && x.RelatedId==id && !x.IsRead).ToListAsync();
        foreach(var notification in relatedNotifications)notification.IsRead=true;
        var messages=await db.SupportMessages.AsNoTracking().Where(x=>x.ThreadId==id)
            .OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Take(100).ToListAsync();
        messages.Reverse();
        await db.SaveChangesAsync();
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
        thread.UserLastReadAt=message.CreatedAt;
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
        var threadIds=rows.Select(x=>x.Id).ToArray();
        var unreadIds=await db.SupportThreads.AsNoTracking()
            .Where(x=>threadIds.Contains(x.Id) && db.SupportMessages.Any(m=>
                m.ThreadId==x.Id && m.SenderType=="USER" && m.CreatedAt>x.StaffLastReadAt))
            .Select(x=>x.Id).ToHashSetAsync();
        var items=new List<object>();
        foreach(var thread in rows)
        {
            items.Add(new{thread.Id,thread.UserId,userName=people.GetValueOrDefault(thread.UserId,"Người dùng"),thread.CaseId,thread.CustodyId,thread.Department,thread.CreatedAt,thread.LastMessageAt,
                title=thread.CaseId.HasValue?"Báo cáo / tranh chấp": "Yêu cầu ký gửi",hasUnread=unreadIds.Contains(thread.Id)});
        }
        return Ok(new{items});
    }

    [HttpGet("threads/{id:guid}/messages")]
    public async Task<IActionResult> History(Guid id)
    {
        var thread=await db.SupportThreads.FirstOrDefaultAsync(x=>x.Id==id);
        if(thread is null || (IsStaff && thread.Department!=await AssignedDepartment()))return NotFound();
        thread.StaffLastReadAt=DateTime.UtcNow;
        var messages=await db.SupportMessages.AsNoTracking().Where(x=>x.ThreadId==id)
            .OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Take(100).ToListAsync();
        messages.Reverse();
        await db.SaveChangesAsync();
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
        thread.StaffLastReadAt=message.CreatedAt;
        thread.LastMessageAt=message.CreatedAt;
        db.SupportMessages.Add(message);
        db.AuditLogs.Add(new AuditLog{ActorId=adminId,Action="SUPPORT_REPLY",TargetType="SUPPORT_THREAD",TargetId=id});
        db.Notifications.Add(new Notification{UserId=thread.UserId,Type="SUPPORT",RelatedId=thread.Id,Content="Bạn có phản hồi mới trong Hỗ trợ & Danh bạ."});
        await db.SaveChangesAsync();
        return Ok(SupportView.Message(message,thread.UserId));
    }
}
