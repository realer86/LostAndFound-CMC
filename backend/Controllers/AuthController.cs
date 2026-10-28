using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record RegisterRequest(string Email, string FullName, string Password);
public record LoginRequest(string Email, string Password);
public record VerifyRequest(string Email, string Code);

[ApiController, Route("api/auth")]
public class AuthController(LostFoundDb db, IPasswordHasher<AppUser> passwords, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    private static object PublicUser(AppUser u) => new {u.Id,u.FullName,u.Email,u.Role,verified=u.EmailVerifiedAt.HasValue};
    private static string TokenHash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    private static string NormalEmail(string email) => email.Trim().ToLowerInvariant();

    [HttpGet("me")]
    public async Task<IActionResult> Me() {
        if(User.Identity?.IsAuthenticated!=true) return Ok(new {user=(object?)null});
        var user=await db.Users.FindAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        return Ok(new {user=user is null?null:PublicUser(user)});
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var email=NormalEmail(req.Email??"");
        if(!new EmailAddressAttribute().IsValid(email)) return BadRequest(new {message="Email không hợp lệ."});
        var domain=config["AllowedEmailDomain"]?.Trim().TrimStart('@').ToLowerInvariant();
        if(!string.IsNullOrWhiteSpace(domain)&&!email.EndsWith("@"+domain,StringComparison.Ordinal)) return BadRequest(new {message=$"Chỉ chấp nhận email trường @{domain}."});
        if(string.IsNullOrWhiteSpace(req.FullName) || req.FullName.Trim().Length<2 || req.FullName.Trim().Length>120 || string.IsNullOrWhiteSpace(req.Password) || req.Password.Length<10 || req.Password.Length>128) return BadRequest(new {message="Tên cần 2–120 ký tự, mật khẩu cần 10–128 ký tự."});
        if(await db.Users.AnyAsync(x=>x.Email==email)) return Conflict(new {message="Email này đã được đăng ký."});
        var rawCode=RandomNumberGenerator.GetInt32(100000,1000000).ToString();
        var user=new AppUser{Email=email,FullName=req.FullName.Trim(),VerifyTokenHash=TokenHash(rawCode),VerifyExpiresAt=DateTime.UtcNow.AddMinutes(15)};
        user.PasswordHash=passwords.HashPassword(user,req.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        // Development-only: no mail delivery configured. A real deployment MUST add an email provider.
        return Ok(new {message="Tài khoản đã tạo. Hãy xác minh email trước khi đăng nhập.",devVerificationCode=env.IsDevelopment()?rawCode:null});
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyRequest req)
    {
        var email=NormalEmail(req.Email??"");
        var user=await db.Users.FirstOrDefaultAsync(x=>x.Email==email);
        if(user is null||user.VerifyTokenHash is null||user.VerifyExpiresAt<DateTime.UtcNow||
           !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(user.VerifyTokenHash),SHA256.HashData(Encoding.UTF8.GetBytes(req.Code??""))))
            return BadRequest(new {message="Mã không đúng hoặc đã hết hạn."});
        user.EmailVerifiedAt=DateTime.UtcNow;
        user.VerifyTokenHash=null;
        user.VerifyExpiresAt=null;
        await db.SaveChangesAsync();
        return Ok(new {message="Email đã được xác minh. Bạn có thể đăng nhập."});
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email=NormalEmail(req.Email??"");
        var user=await db.Users.FirstOrDefaultAsync(x=>x.Email==email);
        if(user is null || passwords.VerifyHashedPassword(user,user.PasswordHash,req.Password??"")==PasswordVerificationResult.Failed)
            return Unauthorized(new {message="Email hoặc mật khẩu không đúng."});
        if(user.EmailVerifiedAt is null) return StatusCode(403,new {message="Vui lòng xác minh email trước khi đăng nhập."});
        if(user.Role=="ADMIN") return StatusCode(403,new {message="Tài khoản quản trị chỉ đăng nhập qua website Admin (cổng 5174)."});
        var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,user.Id.ToString()),new(ClaimTypes.Name,user.FullName),new(ClaimTypes.Role,user.Role)};
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=false,ExpiresUtc=DateTimeOffset.UtcNow.AddHours(12)});
        return Ok(new {user=PublicUser(user)});
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync();return Ok(new {message="Đã đăng xuất."});}
}
