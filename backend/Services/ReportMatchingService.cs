using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LostFound.Api.Domain;

namespace LostFound.Api.Services;

// Heuristic similarity score (0-100), NOT a probability of common ownership.
// Separate from Claims/REQ-04 and from the authentication provider.
public static class ReportMatchingService
{
    private static readonly Dictionary<string, string> Alias = new(StringComparer.Ordinal)
    {
        ["lap top"] = "may tinh", ["laptop"] = "may tinh", ["notebook"] = "may tinh",
        ["dien thoai"] = "smartphone", ["phone"] = "smartphone", ["dtdd"] = "smartphone",
        ["bop"] = "vi", ["wallet"] = "vi", ["purse"] = "vi",
        ["airpods"] = "tai nghe", ["earphone"] = "tai nghe",
        ["chum chia khoa"] = "chia khoa", ["keychain"] = "chia khoa",
        ["the sv"] = "the sinh vien", ["student card"] = "the sinh vien",
        ["xam"] = "ghi", ["gray"] = "ghi", ["grey"] = "ghi"
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var form = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch == 'đ' ? 'd' : char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        var normalized = string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        foreach (var (from, to) in Alias.OrderByDescending(x => x.Key.Length))
        {
            normalized = Regex.Replace(normalized, $@"\b{Regex.Escape(from)}\b", to, RegexOptions.CultureInvariant);
        }
        return normalized;
    }

    public static double Similarity(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a == b) return 1;

        var aa = a.Split(' ').ToHashSet(StringComparer.Ordinal);
        var bb = b.Split(' ').ToHashSet(StringComparer.Ordinal);
        var overlap = aa.Intersect(bb).Count();
        var containment = (double)overlap / Math.Min(aa.Count, bb.Count);
        var jaccard = (double)overlap / aa.Union(bb).Count();
        // Small spelling differences are allowed, but bigram similarity alone cannot
        // override a completely different object name.
        var wordScore = 0.6 * jaccard + 0.4 * containment;
        return Math.Clamp(Math.Max(wordScore, 0.7 * BigramDice(a, b)), 0, 1);
    }

    private static double BigramDice(string a, string b)
    {
        if (a.Length < 2 || b.Length < 2) return a == b ? 1 : 0;
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < a.Length - 1; i++)
        {
            var bigram = a.Substring(i, 2);
            counts[bigram] = counts.GetValueOrDefault(bigram) + 1;
        }
        var overlap = 0;
        for (var i = 0; i < b.Length - 1; i++)
        {
            var bigram = b.Substring(i, 2);
            if (counts.GetValueOrDefault(bigram) < 1) continue;
            counts[bigram]--;
            overlap++;
        }
        return 2d * overlap / (a.Length + b.Length - 2);
    }

    public static MatchEvaluation Evaluate(ItemReport source, ItemReport other)
    {
        if (source.Id == other.Id || source.UserId == other.UserId ||
            source.Status != "OPEN" || other.Status != "OPEN" ||
            !((source.Type == "LOST" && other.Type == "FOUND") ||
              (source.Type == "FOUND" && other.Type == "LOST")))
            return new MatchEvaluation(0, 0, "NONE", []);

        var lost = source.Type == "LOST" ? source : other;
        var found = source.Type == "FOUND" ? source : other;
        double weighted = 0, availableWeight = 0;
        var reasons = new List<string>();
        var comparedFields = 0;
        void Factor(double weight, bool available, double similarity, string reason)
        {
            if (!available) return;
            weighted += weight * Math.Clamp(similarity, 0, 1);
            availableWeight += weight;
            comparedFields++;
            if (similarity >= 0.65) reasons.Add(reason);
        }

        var name = Similarity(source.ItemName, other.ItemName);
        // Description assists incomplete names but cannot substitute for the object type.
        var expanded = Similarity($"{source.ItemName} {source.Description}",
                                  $"{other.ItemName} {other.Description}");
        var nameScore = Math.Max(name, 0.85 * expanded);
        Factor(35, !string.IsNullOrWhiteSpace(source.ItemName) &&
                   !string.IsNullOrWhiteSpace(other.ItemName), nameScore, "Tên hoặc mô tả tương tự");

        Factor(20, source.CategoryId.HasValue && other.CategoryId.HasValue,
            source.CategoryId == other.CategoryId ? 1 : 0, "Cùng danh mục");

        var locationTextSource = string.Join(" ", new[] { source.Location?.Name, source.LocationDetail }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        var locationTextOther = string.Join(" ", new[] { other.Location?.Name, other.LocationDetail }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        var locationScore = Similarity(locationTextSource, locationTextOther);
        if (source.LocationId.HasValue && other.LocationId.HasValue &&
            source.LocationId == other.LocationId) locationScore = Math.Max(locationScore, 1);
        Factor(20, locationTextSource.Length > 0 && locationTextOther.Length > 0,
            locationScore, "Khu vực gần hoặc trùng nhau");

        if (lost.EventTime != default && found.EventTime != default)
        {
            var hours = Math.Abs((found.EventTime - lost.EventTime).TotalHours);
            var chronological = found.EventTime < lost.EventTime.AddHours(-2) ? 0.15 : 1.0;
            Factor(15, true, Math.Exp(-hours / 96.0) * chronological, "Thời điểm gần nhau");
        }
        Factor(5, !string.IsNullOrWhiteSpace(source.Color) && !string.IsNullOrWhiteSpace(other.Color),
            Similarity(source.Color, other.Color), "Màu sắc tương tự");
        Factor(5, !string.IsNullOrWhiteSpace(source.Brand) && !string.IsNullOrWhiteSpace(other.Brand),
            Similarity(source.Brand, other.Brand), "Cùng thương hiệu");

        if (availableWeight < 35 || comparedFields < 2 || nameScore < 0.22)
            return new MatchEvaluation(0, availableWeight / 100, "NONE", []);

        var coverage = availableWeight / 100;
        var score = 100 * weighted / availableWeight * (0.75 + 0.25 * coverage);
        // Sparse descriptions must not be labelled "high confidence" by accident.
        if (coverage < 0.60) score = Math.Min(score, 79);
        var finalScore = Math.Clamp((int)Math.Round(score, MidpointRounding.AwayFromZero), 0, 100);
        var level = finalScore >= 80 ? "HIGH" : finalScore >= 50 ? "POSSIBLE" : "LOW";
        return new MatchEvaluation(finalScore, Math.Round(coverage, 2), level, reasons.Take(4).ToArray());
    }
}

public record MatchEvaluation(int Score, double Coverage, string Level, IReadOnlyList<string> Reasons);
