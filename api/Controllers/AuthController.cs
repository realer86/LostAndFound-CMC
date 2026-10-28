using System.Security.Claims;
using System.Text.RegularExpressions;
using Google.Apis.Auth;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;

namespace LostFound.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly LostFoundDb _db;
    private static readonly HashSet<string> RegisteredEmails = new(StringComparer.OrdinalIgnoreCase);

    public AuthController(IMemoryCache cache, IConfiguration config, LostFoundDb db)
    {
        _cache = cache;
        _config = config;
        _db = db;
    }

    private (bool IsValid, string Role) ValidateSchoolEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return (false, "");
        email = email.Trim().ToLower();

        if (Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@st\.cmcu\.edu\.vn$")) return (true, "Student"); 
        if (Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@cmc\.edu\.vn$")) return (true, "Lecturer"); 
        return (false, "");
    }

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] OtpRequest req)
    {
        var check = ValidateSchoolEmail(req.Email);
        if (!check.IsValid) return BadRequest(new { message = "Chỉ chấp nhận email trường (@st.cmcu.edu.vn hoặc @cmc.edu.vn)!" });

        if (await _db.Users.AnyAsync(u => u.Email == req.Email.ToLower()))
            return BadRequest(new { message = "Email này đã được đăng ký!" });

        var otp = new Random().Next(100000, 999999).ToString();
        _cache.Set($"OTP_{req.Email.ToLower()}", otp, TimeSpan.FromMinutes(5));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Foundly CMC System", _config["AuthSettings:SmtpEmail"]));
        message.To.Add(new MailboxAddress("", req.Email));
        message.Subject = "[Foundly CMC] Mã xác thực OTP";
        message.Body = new TextPart("html") { Text = $"<h3>Xin chào {check.Role},</h3><p>Mã OTP của bạn là: <b>{otp}</b></p>" };

        using var client = new SmtpClient();
        await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_config["AuthSettings:SmtpEmail"], _config["AuthSettings:SmtpAppPassword"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        return Ok(new { message = $"Đã gửi mã OTP tới {req.Email}" });
    }

    [HttpPost("verify-register")]
    public async Task<IActionResult> VerifyAndRegister([FromBody] VerifyRegisterRequest req)
    {
        var check = ValidateSchoolEmail(req.Email);
        if (!check.IsValid) return BadRequest(new { message = "Email không hợp lệ." });

        if (!_cache.TryGetValue($"OTP_{req.Email.ToLower()}", out string? savedOtp) || savedOtp != req.Otp)
            return BadRequest(new { message = "Mã OTP sai hoặc hết hạn!" });

        _cache.Remove($"OTP_{req.Email.ToLower()}");

        // 1. LƯU VÀO DATABASE NẾU CHƯA CÓ
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user == null)
        {
            user = new AppUser { Email = req.Email.ToLower(), FullName = req.FullName, Role = check.Role, EmailVerifiedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // 2. TẠO COOKIE (Bắt buộc phải có NameIdentifier để fix lỗi 500)
        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { message = "Thành công!", user = new { email = user.Email, fullName = user.FullName, role = user.Role }});
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _config["AuthSettings:GoogleClientId"] } };
        var payload = await GoogleJsonWebSignature.ValidateAsync(req.Credential, settings);
        var check = ValidateSchoolEmail(payload.Email);
        string assignedRole = check.IsValid ? check.Role : "Guest";

        // 1. LƯU VÀO DATABASE NẾU CHƯA CÓ
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email.ToLower());
        if (user == null)
        {
            user = new AppUser { Email = payload.Email.ToLower(), FullName = payload.Name, Role = assignedRole, EmailVerifiedAt = DateTime.UtcNow };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // 2. TẠO COOKIE (Bắt buộc phải có NameIdentifier để fix lỗi 500)
        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new { message = "Thành công!", user = new { email = user.Email, fullName = user.FullName, role = user.Role }});
    }
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("lostfound.session"); 
        return Ok(new { message = "Đã đăng xuất" });
    }
}

public record OtpRequest(string Email);
public record VerifyRegisterRequest(string Email, string FullName, string Password, string Otp);
public record GoogleLoginRequest(string Credential);