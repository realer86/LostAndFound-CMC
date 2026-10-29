using LostFound.Api.Data;
using LostFound.Api.Domain;
using LostFound.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<LostFoundDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Main")));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<SchoolEmailPolicy>();
builder.Services.AddScoped<EmailOtpSender>();
builder.Services.AddHttpClient("GoogleTokenInfo", c => c.Timeout = TimeSpan.FromSeconds(10));
// Bản Final dùng Cookie Authentication; giữ nguyên cơ chế đăng nhập cho Form POST.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "foundly.sprint1.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        // Reject older cookies as soon as the school-only policy takes effect.
        options.Events.OnValidatePrincipal = async context =>
        {
            var rawId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var db = context.HttpContext.RequestServices.GetRequiredService<LostFoundDb>();
            var policy = context.HttpContext.RequestServices.GetRequiredService<SchoolEmailPolicy>();
            var user = Guid.TryParse(rawId, out var id) ? await db.Users.FindAsync(id) : null;
            if (user is null || user.Role != context.Principal?.FindFirstValue(ClaimTypes.Role) ||
                policy.AccountType(user.Email) is null || user.EmailVerifiedAt is null ||
                (user.SuspendedUntil.HasValue && user.SuspendedUntil > DateTime.UtcNow))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });
// Separate cookie and scheme for back-office. A public login cannot access admin APIs.
builder.Services.AddAuthentication().AddCookie("AdminCookie", options =>
{
    options.Cookie.Name = "foundly.admin.session";
    options.Cookie.Path = "/api/admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode=401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode=403; return Task.CompletedTask; };
    options.Events.OnValidatePrincipal = async context =>
    {
        var rawId=context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var db=context.HttpContext.RequestServices.GetRequiredService<LostFoundDb>();
        var user=Guid.TryParse(rawId,out var id)?await db.Users.FindAsync(id):null;
        var policy=context.HttpContext.RequestServices.GetRequiredService<SchoolEmailPolicy>();
        if(user is null || user.Role != context.Principal?.FindFirstValue(ClaimTypes.Role) ||
           (user.Role!="ADMIN" && (user.Role!="STAFF" || user.DepartmentCode is not ("SECURITY" or "LIBRARY" or "DORM"))) ||
           policy.AccountType(user.Email) is null || user.EmailVerifiedAt is null ||
           (user.SuspendedUntil.HasValue && user.SuspendedUntil>DateTime.UtcNow))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync("AdminCookie");
        }
    };
});
builder.Services.AddAuthorization();
// A coarse IP-based limit complements per-account OTP cooldown/attempt limits.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30, Window = TimeSpan.FromMinutes(5), QueueLimit = 0,
            AutoReplenishment = true
        }));
});
var app = builder.Build();

// Giữ Origin guard của bản Final, thu gọn cho Web Sprint 1 trên localhost:5175.
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        context.Request.Headers.TryGetValue("Origin", out var origin))
    {
        if (!Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var uri) ||
            !(uri.Host == "localhost" || uri.Host == "127.0.0.1") ||
            uri.Port is not (5175 or 5174 or 8081))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { message = "Origin không được phép." });
            return;
        }
    }
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", app = "Foundly Sprint 1" }));

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LostFoundDb>();
    // Giữ EnsureCreatedAsync() của Final cho demo localhost. Không dùng thay migration ở production.
    const int attempts = 25;
    for (var i = 0; i < attempts; i++)
    {
        try { await db.Database.EnsureCreatedAsync(); break; }
        catch when (i < attempts - 1) { await Task.Delay(2000); }
    }
    // EF EnsureCreated không thêm bảng mới vào DB có sẵn. Chạy script idempotent #13
    // cho cả CSDL Sprint 1 cũ và CSDL tạo mới mà không xóa dữ liệu.
    var schemaPath = Path.Combine(app.Environment.ContentRootPath, "DatabaseScripts", "04_sprint2_contacts.sql");
    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(schemaPath));
    var authSchema = Path.Combine(app.Environment.ContentRootPath, "DatabaseScripts", "05_school_auth.sql");
    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(authSchema));
    var operationsSchema = Path.Combine(app.Environment.ContentRootPath, "DatabaseScripts", "06_operations.sql");
    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(operationsSchema));
    var supportSchema = Path.Combine(app.Environment.ContentRootPath, "DatabaseScripts", "07_support_channels.sql");
    await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(supportSchema));

    // Sửa vấn đề seed liên kết trong Final: mỗi danh mục/khu vực được kiểm tra độc lập.
    if (!await db.Categories.AnyAsync())
    {
        db.Categories.AddRange(new[] { "Điện tử", "Giấy tờ", "Ví & túi", "Chìa khóa", "Trang sức", "Sách & đồ dùng", "Khác" }
            .Select(name => new Category { Name = name }));
    }
    if (!await db.Locations.AnyAsync())
    {
        db.Locations.AddRange(new[] { "Thư viện", "Giảng đường A", "Giảng đường B", "Căn tin", "Ký túc xá", "Sân thể thao", "Bãi xe", "Khu vực khác" }
            .Select(name => new CampusLocation { Name = name }));
    }
    await db.SaveChangesAsync();

    // Thông tin phòng ban bên dưới chỉ là ví dụ để demo; không khẳng định số máy thực của trường.
    if (app.Environment.IsDevelopment() && !await db.Directories.AnyAsync())
    {
        db.Directories.AddRange(
            new CampusDirectoryEntry { Name = "Phòng Bảo vệ (demo)", Location = "Sảnh tòa nhà A (demo)", InternalPhone = "1001", OfficeHours = "Theo ca trực", Category = "Bảo vệ" },
            new CampusDirectoryEntry { Name = "Ban Quản lý (demo)", Location = "Tầng 1 (demo)", InternalPhone = "1002", OfficeHours = "Giờ hành chính", Category = "Quản lý" });
        await db.SaveChangesAsync();
    }

    // Explicit opt-in, Development-only local test identities. Never create real school accounts.
    await DevelopmentTestUsers.SeedIfEnabledAsync(scope.ServiceProvider);
}
app.Run();
