using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using LostFound.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record RegisterRequest(string Email, string FullName, string Password);
public record LoginRequest(string Email, string Password);
public record VerifyRequest(string Email, string Code);
public record ResendRequest(string Email);
public record GoogleLoginRequest(string Credential);

[ApiController, Route("api/auth")]
public class AuthController(
    LostFoundDb db,
    IPasswordHasher<AppUser> passwords,
    IConfiguration config,
    IWebHostEnvironment env,
    SchoolEmailPolicy schoolEmails,
    EmailOtpSender sender,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthController> logger) : ControllerBase
{
    private object PublicUser(AppUser u) => new
    {
        u.Id, u.FullName, u.Email, u.Role,
        accountType = schoolEmails.AccountType(u.Email),
        verified = u.EmailVerifiedAt.HasValue
    };

    private static string NewCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    private static string Normalize(string? email) => SchoolEmailPolicy.NormalizeEmail(email);
    private bool Allowed(string email) => schoolEmails.IsSchoolEmail(email);
    private IActionResult BadSchoolEmail() => BadRequest(new { message = $"Chỉ chấp nhận email trường {schoolEmails.AllowedDomains}." });

    private async Task SignIn(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (User.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Ok(new { user = (object?)null });
        var user = await db.Users.FindAsync(id);
        if (user is null || !(Allowed(user.Email) || schoolEmails.IsLocalTestAccount(user.Email)) || user.EmailVerifiedAt is null ||
            (user.SuspendedUntil.HasValue && user.SuspendedUntil > DateTime.UtcNow))
        {
            await HttpContext.SignOutAsync();
            return Ok(new { user = (object?)null });
        }
        return Ok(new { user = PublicUser(user) });
    }

    [HttpPost("register"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken cancellationToken)
    {
        var email = Normalize(req.Email);
        if (!Allowed(email)) return BadSchoolEmail();
        var name = req.FullName?.Trim() ?? "";
        if (name.Length < 2 || name.Length > 120 || req.Password is null || req.Password.Length < 10 || req.Password.Length > 128)
            return BadRequest(new { message = "Tên cần 2–120 ký tự, mật khẩu cần 10–128 ký tự." });
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Conflict(new { message = "Email này đã được đăng ký. Nếu chưa xác minh, hãy chọn Gửi lại OTP." });

        var code = NewCode();
        var user = new AppUser
        {
            Email = email, FullName = name,
            VerifyExpiresAt = DateTime.UtcNow.AddMinutes(10),
            VerifyTokenHash = "", // Hashed below using PBKDF2, never store plaintext code.
            OtpSentAt = DateTime.UtcNow
        };
        user.VerifyTokenHash = passwords.HashPassword(user, code);
        user.PasswordHash = passwords.HashPassword(user, req.Password);

        try { await sender.SendAsync(email, code, cancellationToken); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("OTP delivery failed for school registration ({ErrorType})", ex.GetType().Name);
            return StatusCode(503, new { message = "Chưa gửi được OTP. Kiểm tra cấu hình SMTP hoặc thử lại sau." });
        }

        db.Users.Add(user);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Email này đã được đăng ký." });
        }
        return Ok(new
        {
            message = sender.IsDevelopmentCodeEnabled ? "Mã OTP thử nghiệm đã tạo." : "Đã gửi OTP đến email trường.",
            devVerificationCode = sender.IsDevelopmentCodeEnabled ? code : null
        });
    }

    [HttpPost("resend"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Resend(ResendRequest req, CancellationToken cancellationToken)
    {
        var email = Normalize(req.Email);
        if (!Allowed(email)) return BadSchoolEmail();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || user.EmailVerifiedAt.HasValue)
            return Ok(new { message = "Nếu tài khoản cần xác minh, OTP mới sẽ được gửi." });
        if (user.OtpSentAt.HasValue && user.OtpSentAt > DateTime.UtcNow.AddSeconds(-60))
            return StatusCode(429, new { message = "Vui lòng chờ 60 giây trước khi yêu cầu OTP khác." });

        var code = NewCode();
        try { await sender.SendAsync(email, code, cancellationToken); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("OTP resend failed ({ErrorType})", ex.GetType().Name);
            return StatusCode(503, new { message = "Chưa gửi được OTP. Vui lòng thử lại sau." });
        }
        user.VerifyTokenHash = passwords.HashPassword(user, code);
        user.VerifyExpiresAt = DateTime.UtcNow.AddMinutes(10);
        user.OtpSentAt = DateTime.UtcNow;
        user.OtpAttemptCount = 0;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            message = "Nếu tài khoản cần xác minh, OTP mới sẽ được gửi.",
            devVerificationCode = sender.IsDevelopmentCodeEnabled ? code : null
        });
    }

    [HttpPost("verify"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Verify(VerifyRequest req, CancellationToken cancellationToken)
    {
        var email = Normalize(req.Email);
        if (!Allowed(email)) return BadSchoolEmail();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || user.VerifyTokenHash is null || user.VerifyExpiresAt <= DateTime.UtcNow ||
            user.OtpAttemptCount >= 5 || req.Code is null || req.Code.Length != 6 || !req.Code.All(char.IsAsciiDigit))
            return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn. Có thể yêu cầu gửi lại mã." });

        PasswordVerificationResult verified;
        try { verified = passwords.VerifyHashedPassword(user, user.VerifyTokenHash, req.Code); }
        catch (FormatException) { verified = PasswordVerificationResult.Failed; }
        if (verified == PasswordVerificationResult.Failed)
        {
            user.OtpAttemptCount++;
            if (user.OtpAttemptCount >= 5)
            {
                user.VerifyTokenHash = null;
                user.VerifyExpiresAt = null;
            }
            await db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn. Có thể yêu cầu gửi lại mã." });
        }
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.VerifyTokenHash = null;
        user.VerifyExpiresAt = null;
        user.OtpAttemptCount = 0;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Email đã được xác minh. Bạn có thể đăng nhập." });
    }

    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken cancellationToken)
    {
        var email = Normalize(req.Email);
        if (!Allowed(email) && !schoolEmails.IsLocalTestAccount(email)) return BadSchoolEmail();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash) || req.Password is null ||
            passwords.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        if (user.EmailVerifiedAt is null) return StatusCode(403, new { message = "Vui lòng xác minh email trước khi đăng nhập." });
        if (user.SuspendedUntil.HasValue && user.SuspendedUntil > DateTime.UtcNow)
            return StatusCode(403, new { message = "Tài khoản đang bị tạm khóa." });
        if (user.Role == "ADMIN") return StatusCode(403, new { message = "Đăng nhập quản trị chưa được hỗ trợ trên giao diện này." });
        await SignIn(user);
        return Ok(new { user = PublicUser(user) });
    }

    [HttpPost("google"), EnableRateLimiting("auth")]
    public async Task<IActionResult> Google(GoogleLoginRequest req, CancellationToken cancellationToken)
    {
        var clientId = config["Auth:GoogleClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return StatusCode(503, new { message = "Google Login chưa được cấu hình." });
        if (string.IsNullOrWhiteSpace(req.Credential) || req.Credential.Length > 8192)
            return BadRequest(new { message = "Google credential không hợp lệ." });

        // Google tokeninfo is an HTTPS, server-to-server ID-token verification service.
        // Never trust an email or role sent directly by the browser.
        JsonDocument token;
        try
        {
            using var http = httpClientFactory.CreateClient("GoogleTokenInfo");
            using var response = await http.GetAsync(
                "https://oauth2.googleapis.com/tokeninfo?id_token=" + Uri.EscapeDataString(req.Credential), cancellationToken);
            if (!response.IsSuccessStatusCode) return Unauthorized(new { message = "Google ID Token không hợp lệ." });
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            token = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning("Google token verification service unavailable ({ErrorType})", ex.GetType().Name);
            return StatusCode(503, new { message = "Chưa thể xác minh tài khoản Google. Thử lại sau." });
        }
        using (token)
        {
            var p = token.RootElement;
            static string Read(JsonElement root, string key) => root.TryGetProperty(key, out var item) ? item.ToString() : "";
            var issuer = Read(p, "iss");
            var subject = Read(p, "sub");
            var email = Normalize(Read(p, "email"));
            if (Read(p, "aud") != clientId ||
                issuer is not ("accounts.google.com" or "https://accounts.google.com") ||
                subject.Length is < 3 or > 128 ||
                !string.Equals(Read(p, "email_verified"), "true", StringComparison.OrdinalIgnoreCase) ||
                !long.TryParse(Read(p, "exp"), out var expires) ||
                expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return Unauthorized(new { message = "Google ID Token không hợp lệ." });
            if (!Allowed(email)) return BadSchoolEmail();

            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
            if (user is not null && user.GoogleSubject is not null && user.GoogleSubject != subject)
                return StatusCode(403, new { message = "Tài khoản đã liên kết với Google khác." });
            if (user is not null && user.Role == "ADMIN")
                return StatusCode(403, new { message = "Đăng nhập quản trị chưa được hỗ trợ trên giao diện này." });
            if (user is not null && user.SuspendedUntil.HasValue && user.SuspendedUntil > DateTime.UtcNow)
                return StatusCode(403, new { message = "Tài khoản đang bị tạm khóa." });

            if (user is null)
            {
                var name = Read(p, "name").Trim();
                if (name.Length is < 2 or > 120) name = email.Split('@')[0];
                user = new AppUser
                {
                    Email = email, FullName = name, PasswordHash = "", // Google-only: no local password.
                    GoogleSubject = subject, EmailVerifiedAt = DateTime.UtcNow
                };
                db.Users.Add(user);
            }
            else
            {
                // Safe linking requires a Google-issued ID token with verified school email.
                user.GoogleSubject = subject;
                user.EmailVerifiedAt ??= DateTime.UtcNow;
                user.VerifyTokenHash = null;
                user.VerifyExpiresAt = null;
            }
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Tài khoản đang được cập nhật. Vui lòng thử lại." });
            }
            await SignIn(user);
            return Ok(new { user = PublicUser(user) });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok(new { message = "Đã đăng xuất." });
    }
}
