using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Services;

/// <summary>
/// Optional, local-only personas for exercising multi-account workflows.
/// Their reserved .invalid addresses cannot be confused with real CMC accounts.
/// </summary>
public static class DevelopmentTestUsers
{
    private sealed record Persona(string Email, string DisplayName, string Role, string AccountType);

    private static readonly Persona[] Personas =
    [
        new("student01@foundly.invalid", "Sinh viên A (kiểm thử)", "USER", "STUDENT"),
        new("student02@foundly.invalid", "Sinh viên B (kiểm thử)", "USER", "STUDENT"),
        new("lecturer01@foundly.invalid", "Giảng viên (kiểm thử)", "USER", "LECTURER"),
        new("admin@foundly.invalid", "Quản trị viên (kiểm thử)", "ADMIN", "LECTURER")
    ];

    // Explicit four-account allowlist only; arbitrary .invalid emails remain forbidden.
    public static string? AccountType(string? email)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        return Personas.FirstOrDefault(persona => persona.Email == normalized)?.AccountType;
    }

    public static async Task SeedIfEnabledAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        if (!config.GetValue<bool>("DevSeed:Enabled")) return;

        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("DevSeed may only be enabled in Development.");

        var password = config["DevSeed:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 16 || password.Length > 128)
            throw new InvalidOperationException("Set DEV_SEED_PASSWORD in .env (16-128 characters) for local test accounts.");

        var db = services.GetRequiredService<LostFoundDb>();
        var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();
        var added = 0;
        foreach (var persona in Personas)
        {
            // Never overwrite existing users, role assignments, or password hashes.
            if (await db.Users.AnyAsync(u => u.Email == persona.Email)) continue;

            var user = new AppUser
            {
                Email = persona.Email,
                FullName = persona.DisplayName,
                Role = persona.Role,
                EmailVerifiedAt = DateTime.UtcNow
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            added++;
        }
        if (added > 0) await db.SaveChangesAsync();
    }
}
