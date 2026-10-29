using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Services;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

[ApiController,Route("api/admin/auth")]
public class AdminAuthController(LostFoundDb db,IPasswordHasher<AppUser> passwordHasher, SchoolEmailPolicy policy) : ControllerBase
{
    [HttpPost("login"),EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email=(req.Email??"").Trim().ToLowerInvariant();
        var user=await db.Users.FirstOrDefaultAsync(x=>x.Email==email);
        if(policy.AccountType(email) is null)return Unauthorized(new{message="Tài khoản không hợp lệ."});
        if(user is null || (user.Role!="ADMIN" && (user.Role!="STAFF" || user.DepartmentCode is not ("SECURITY" or "LIBRARY" or "DORM"))) || user.EmailVerifiedAt is null ||
           (user.SuspendedUntil.HasValue && user.SuspendedUntil>DateTime.UtcNow) ||
           string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(req.Password) ||
           passwordHasher.VerifyHashedPassword(user,user.PasswordHash,req.Password)==PasswordVerificationResult.Failed)
            return Unauthorized(new{message="Tài khoản hoặc mật khẩu quản trị không hợp lệ."});
        var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,user.Id.ToString()),new(ClaimTypes.Name,user.FullName),new(ClaimTypes.Role,user.Role)};
        await HttpContext.SignInAsync("AdminCookie",new ClaimsPrincipal(new ClaimsIdentity(claims,"AdminCookie")),new AuthenticationProperties{IsPersistent=false,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(8)});
        return Ok(new{user=new{user.Id,user.Email,user.FullName,user.Role,user.DepartmentCode}});
    }

    [Authorize(AuthenticationSchemes="AdminCookie",Roles="ADMIN,STAFF"),HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u=await db.Users.FindAsync(id);
        if(u is null || u.Role is not ("ADMIN" or "STAFF"))return Unauthorized();
        return Ok(new{user=new{u.Id,u.Email,u.FullName,u.Role,u.DepartmentCode}});
    }

    [Authorize(AuthenticationSchemes="AdminCookie",Roles="ADMIN,STAFF"),HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return Ok(new{message="Đã đăng xuất cổng quản trị."});
    }
}
