using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

[ApiController,Route("api/admin/auth")]
public class AdminAuthController(LostFoundDb db,IPasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpPost("login"),EnableRateLimiting("sensitive")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email=(req.Email??"").Trim().ToLowerInvariant();
        var user=await db.Users.FirstOrDefaultAsync(x=>x.Email==email);
        if(user is null||user.Role!="ADMIN"||user.EmailVerifiedAt is null||
           passwordHasher.VerifyHashedPassword(user,user.PasswordHash,req.Password??"")==PasswordVerificationResult.Failed)
            return Unauthorized(new{message="Tài khoản hoặc mật khẩu quản trị không hợp lệ."});
        var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,user.Id.ToString()),new(ClaimTypes.Name,user.FullName),new(ClaimTypes.Role,user.Role)};
        await HttpContext.SignInAsync("AdminCookie",new ClaimsPrincipal(new ClaimsIdentity(claims,"AdminCookie")),new AuthenticationProperties{IsPersistent=false,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(8)});
        return Ok(new{user=new{user.Id,user.Email,user.FullName,user.Role}});
    }

    [Authorize(AuthenticationSchemes="AdminCookie",Roles="ADMIN"),HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u=await db.Users.FindAsync(id);
        if(u is null||u.Role!="ADMIN")return Unauthorized();
        return Ok(new{user=new{u.Id,u.Email,u.FullName,u.Role}});
    }

    [Authorize(AuthenticationSchemes="AdminCookie",Roles="ADMIN"),HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return Ok(new{message="Đã đăng xuất cổng quản trị."});
    }
}
