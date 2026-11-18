using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Services;

public static class MatchNotificationService
{
    public static async Task RecordAsync(
        LostFoundDb db, ItemReport source, IEnumerable<ItemReport> matches, CancellationToken cancellationToken = default)
    {
        foreach (var match in matches)
        {
            await InsertAsync(db, source.UserId, source.Id, match.Id, cancellationToken);
            await InsertAsync(db, match.UserId, match.Id, source.Id, cancellationToken);
        }
    }

    private static Task<int> InsertAsync(
        LostFoundDb db, Guid userId, Guid reportId, Guid relatedId, CancellationToken cancellationToken)
    {
        var content = "Có gợi ý phù hợp mới cho bài đăng của bạn.";
        return db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Notifications" ("Id", "UserId", "Type", "Content", "ReportId", "RelatedId", "IsRead", "CreatedAt")
            VALUES ({Guid.NewGuid()}, {userId}, {"MATCH"}, {content}, {reportId}, {relatedId}, {false}, {DateTime.UtcNow})
            ON CONFLICT ("UserId", "ReportId", "RelatedId") WHERE "Type" = 'MATCH' DO NOTHING
            """, cancellationToken);
    }

    public static async Task<List<ItemReport>> FindMatchesAsync(
        LostFoundDb db, ItemReport source, int minScore, CancellationToken cancellationToken = default)
    {
        var opposite = source.Type == "LOST" ? "FOUND" : "LOST";
        var candidates = await db.Reports.AsNoTracking()
            .Include(x => x.Location).Include(x => x.Category)
            .Where(x => x.Type == opposite && x.Status == "OPEN" && x.UserId != source.UserId)
            .OrderByDescending(x => x.EventTime).Take(1000).ToListAsync(cancellationToken);

        return candidates.Select(report => new
            {
                Report = report,
                Evaluation = ReportMatchingService.Evaluate(source, report)
            })
            .Where(x => x.Evaluation.Score >= minScore)
            .OrderByDescending(x => x.Evaluation.Score)
            .ThenByDescending(x => x.Evaluation.Coverage)
            .ThenByDescending(x => x.Report.EventTime)
            .Take(10)
            .Select(x => x.Report)
            .ToList();
    }
}
