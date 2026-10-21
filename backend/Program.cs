using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<LostFoundDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Main")));
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
// Bản Final dùng Cookie Authentication; giữ nguyên cơ chế đăng nhập cho Form POST.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "foundly.sprint1.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
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
builder.Services.AddAuthorization();
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
            uri.Port is not (5175 or 8081))
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

    if (app.Environment.IsDevelopment())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var demoUsers = new[]
        {
            ("minh@campus.edu", "Minh - Người mất đồ", "Demo12345!"),
            ("lan@campus.edu", "Lan - Người nhặt đồ", "Demo12345!")
        };
        foreach (var (email, name, password) in demoUsers)
        {
            if (await db.Users.AnyAsync(x => x.Email == email)) continue;
            var user = new AppUser { Email = email, FullName = name, EmailVerifiedAt = DateTime.UtcNow };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
        }
        await db.SaveChangesAsync();
    }
}
app.Run();
