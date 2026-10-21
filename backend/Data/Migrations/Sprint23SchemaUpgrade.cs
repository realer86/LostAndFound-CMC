using LostFound.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Data.Migrations;

/// <summary>
/// Idempotent schema bootstrap for existing Sprint 1 databases.
/// EnsureCreatedAsync only creates the schema if the database has no tables.
/// The SQL file is embedded in the API build: no host file path dependency.
/// For production, migrate to versioned EF migrations and test backups.
/// </summary>
public static class Sprint23SchemaUpgrade
{
    public static async Task ApplyAsync(LostFoundDb db, CancellationToken ct = default)
    {
        var assembly = typeof(Sprint23SchemaUpgrade).Assembly;
        await using var stream = assembly.GetManifestResourceStream("Foundly.Sprint23.Schema")
            ?? throw new InvalidOperationException("Embedded Sprint 2/3 schema SQL not found.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct);
        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
