using System.Text.RegularExpressions;
using Google.Apis.Auth;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MimeKit;
using Microsoft.AspNetCore.Authentication;

namespace Foundly.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private static readonly HashSet<string> RegisteredEmails = new(StringComparer.OrdinalIgnoreCase);

    public AuthController(IMemoryCache cache, IConfiguration config)
    {
        _cache = cache;
        _config = config;
    }

    // Hàm kiểm tra đuôi Email trường
    private (bool IsValid, string Role) ValidateSchoolEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return (false, "");
        email = email.Trim().ToLower();

        if (Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@st\.cmcu\.edu\.vn$"))
            return (true, "Student"); 

        if (Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@cmc\.edu\.vn$"))
            return (true, "Lecturer"); 

        return (false, "");
    }

    // 1. API YÊU CẦU GỬI MÃ OTP (Chỉ cho email trường)
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] OtpRequest req)
    {
        var check = ValidateSchoolEmail(req.Email);
        if (!check.IsValid)
            return BadRequest(new { message = "Chỉ chấp nhận email trường (@st.cmcu.edu.vn hoặc @cmc.edu.vn)!" });

        if (RegisteredEmails.Contains(req.Email))
            return BadRequest(new { message = "Email này đã được đăng ký! Mỗi người chỉ được tạo 1 tài khoản." });

        var otp = new Random().Next(100000, 999999).ToString();
        _cache.Set($"OTP_{req.Email.ToLower()}", otp, TimeSpan.FromMinutes(5));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Foundly CMC System", _config["AuthSettings:SmtpEmail"]));
        message.To.Add(new MailboxAddress("", req.Email));
        message.Subject = "[Foundly CMC] Mã xác thực OTP đăng ký tài khoản";
        message.Body = new TextPart("html")
        {
            Text = $"<h3>Xin chào {check.Role},</h3><p>Mã OTP xác minh danh tính của bạn là: <b style='font-size:20px;color:#0d9488;'>{otp}</b></p><p>Mã có hiệu lực trong 5 phút.</p>"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_config["AuthSettings:SmtpEmail"], _config["AuthSettings:SmtpAppPassword"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        return Ok(new { message = $"Đã gửi mã OTP tới {req.Email}", role = check.Role });
    }

    // 2. API XÁC MINH OTP
    [HttpPost("verify-register")]
    public IActionResult VerifyAndRegister([FromBody] VerifyRegisterRequest req)
    {
        var check = ValidateSchoolEmail(req.Email);
        if (!check.IsValid)
            return BadRequest(new { message = "Email không hợp lệ." });

        if (RegisteredEmails.Contains(req.Email))
            return BadRequest(new { message = "Tài khoản này đã tồn tại trên hệ thống!" });

        if (!_cache.TryGetValue($"OTP_{req.Email.ToLower()}", out string? savedOtp) || savedOtp != req.Otp)
            return BadRequest(new { message = "Mã OTP không chính xác hoặc đã hết hạn!" });

        _cache.Remove($"OTP_{req.Email.ToLower()}");
        RegisteredEmails.Add(req.Email.ToLower());

        return Ok(new
        {
            message = "Đăng ký và xác minh danh tính thành công!",
            user = new { email = req.Email, fullName = req.FullName, role = check.Role }
        });
    }

    // 3. API ĐĂNG NHẬP GOOGLE (Chấp nhận MỌI Gmail)
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest req)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _config["AuthSettings:GoogleClientId"] }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(req.Credential, settings);
        var check = ValidateSchoolEmail(payload.Email);

        // Sinh viên/GV dùng mail trường qua Google sẽ có role tương ứng. Mail ngoài sẽ là Guest.
        string assignedRole = check.IsValid ? check.Role : "Guest";

        RegisteredEmails.Add(payload.Email.ToLower());

        return Ok(new
        {
            message = "Đăng nhập Google thành công!",
            user = new { email = payload.Email, fullName = payload.Name, role = assignedRole }
        });
        
    }
    // 4. API ĐĂNG XUẤT
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Lệnh chuẩn để xóa phiên đăng nhập (Cookie) của .NET
        await HttpContext.SignOutAsync();
        
        // Dự phòng xóa thủ công nếu hệ thống cũ của bạn dùng tên cookie tùy chỉnh
        Response.Cookies.Delete("auth_token"); 
        
        return Ok(new { message = "Đăng xuất thành công" });
    }
}


public record OtpRequest(string Email);
public record VerifyRegisterRequest(string Email, string FullName, string Password, string Otp);
public record GoogleLoginRequest(string Credential);
