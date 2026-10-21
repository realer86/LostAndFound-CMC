using System.Globalization;
using System.Text;
using System.Text.Json;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Services;

/// <summary>Bidirectional, weighted, explainable fuzzy matching for the university prototype.</summary>
public class MatchEngine(LostFoundDb db, IConfiguration config)
{
    private static readonly Dictionary<string,string> Synonyms = new(StringComparer.Ordinal) {
        ["bop"]="vi",["wallet"]="vi",["vi da"]="vi",["purse"]="vi",
        ["laptop"]="may tinh",["lap top"]="may tinh",["notebook"]="may tinh",
        ["dien thoai"]="smartphone",["phone"]="smartphone",["dtdd"]="smartphone",
        ["earphone"]="tai nghe",["airpod"]="tai nghe",["airpods"]="tai nghe",
        ["chum chia khoa"]="chia khoa",["key"]="chia khoa",
        ["student card"]="the sinh vien",["the sv"]="the sinh vien"
    };
    private static readonly (string key, int weight)[] Weights = {
        ("name",25),("location",20),("time",20),("description",15),("color",10),("brand",5),("category",5)
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var normalized = value.ToLowerInvariant().Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach(var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c == 'đ' ? 'd' : char.IsLetterOrDigit(c) ? c : ' ');
        }
        var text = string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        foreach(var pair in Synonyms.OrderByDescending(x => x.Key.Length))
            text = System.Text.RegularExpressions.Regex.Replace(text, $@"\b{System.Text.RegularExpressions.Regex.Escape(pair.Key)}\b", pair.Value, System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        return text;
    }
    public static double Similarity(string? first, string? second)
    {
        var a = Normalize(first); var b = Normalize(second);
        if(a.Length == 0 || b.Length == 0) return 0;
        if(a == b) return 1;
        var aa = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var bb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var jaccard = (double)aa.Intersect(bb).Count() / aa.Union(bb).Count();
        return Math.Clamp(Math.Max(jaccard, DiceBigrams(a, b)),0,1);
    }
    private static double DiceBigrams(string a, string b)
    {
        if(a.Length < 2 || b.Length < 2) return a == b ? 1 : 0;
        var x = new Dictionary<string,int>();
        for(int i=0;i<a.Length-1;i++) { var p=a.Substring(i,2); x[p] = x.GetValueOrDefault(p)+1; }
        var intersection=0;
        for(int i=0;i<b.Length-1;i++) { var p=b.Substring(i,2); if(x.GetValueOrDefault(p)>0) {intersection++;x[p]--;} }
        return 2.0*intersection / (a.Length+b.Length-2);
    }
    public static MatchScore Calculate(ItemReport lost, ItemReport found)
    {
        // Defensive invariant: only (LOST, FOUND) pairs are eligible for matching.
        if(lost.Type!="LOST" || found.Type!="FOUND" || lost.Id==found.Id)
            return new MatchScore(0,0,0,new Dictionary<string,double>(),false);
        var values = new Dictionary<string,double>();
        void Add(string key, bool available, double value) { if (available) values[key]=Math.Clamp(value,0,1); }
        Add("name", !string.IsNullOrWhiteSpace(lost.ItemName) && !string.IsNullOrWhiteSpace(found.ItemName), Similarity(lost.ItemName,found.ItemName));
        var lLoc = lost.Location?.Name ?? lost.LocationDetail;
        var fLoc = found.Location?.Name ?? found.LocationDetail;
        var location = Similarity(lLoc,fLoc);
        if(lost.LocationId.HasValue && found.LocationId.HasValue)
            location = lost.LocationId == found.LocationId ? 1 : Math.Max(location,0.20);
        Add("location",!string.IsNullOrWhiteSpace(lLoc)&&!string.IsNullOrWhiteSpace(fLoc),location);
        var diffHours = Math.Abs((lost.EventTime-found.EventTime).TotalHours);
        var chronology = found.EventTime < lost.EventTime.AddHours(-2) ? 0.25 : 1.0;
        Add("time",lost.EventTime!=default && found.EventTime!=default,Math.Exp(-diffHours/96.0)*chronology);
        Add("description",!string.IsNullOrWhiteSpace(lost.Description)&&!string.IsNullOrWhiteSpace(found.Description),Similarity(lost.Description,found.Description));
        Add("color",!string.IsNullOrWhiteSpace(lost.Color)&&!string.IsNullOrWhiteSpace(found.Color),Similarity(lost.Color,found.Color));
        Add("brand",!string.IsNullOrWhiteSpace(lost.Brand)&&!string.IsNullOrWhiteSpace(found.Brand),Similarity(lost.Brand,found.Brand));
        Add("category",lost.CategoryId.HasValue&&found.CategoryId.HasValue,lost.CategoryId==found.CategoryId?1:0.25);
        var available = Weights.Where(w=>values.ContainsKey(w.key)).ToList();
        if(available.Count==0) return new(0,0,0,values,false);
        var weight = available.Sum(w=>w.weight);
        var raw = 100*available.Sum(w=>w.weight*values[w.key])/weight;
        var coverage=weight/100.0;
        var score=Math.Round(raw*(0.7+0.3*coverage),1);
        var sufficient=coverage>=.55 && available.Count>=3 && values.ContainsKey("name") &&
            (values.ContainsKey("location")||values.ContainsKey("description"));
        return new(score,coverage,available.Count,values,sufficient);
    }
    public async Task MatchAsync(Guid reportId,CancellationToken token=default)
    {
        var report=await db.Reports.Include(r=>r.Location).FirstOrDefaultAsync(r=>r.Id==reportId,token);
        if(report is null||report.Status!="OPEN") return;
        var other=report.Type=="LOST"?"FOUND":"LOST";
        var max=Math.Clamp(config.GetValue<int?>("Matching:MaxCandidates")??500,50,3000);
        // Broad filter: never reject only due to differing category/location or sparse text.
        var candidates=await db.Reports.AsNoTracking().Include(r=>r.Location)
            .Where(r=>r.Type==other && r.Status=="OPEN" && r.UserId!=report.UserId)
            .OrderByDescending(r=>r.EventTime).Take(max).ToListAsync(token);
        foreach(var candidate in candidates)
        {
            var lost=report.Type=="LOST"? report:candidate;
            var found=report.Type=="FOUND"?report:candidate;
            var result=Calculate(lost,found);
            if(result.Score<55 || !result.Sufficient) continue;
            var existing=await db.Matches.FirstOrDefaultAsync(x=>x.LostReportId==lost.Id && x.FoundReportId==found.Id,token);
            if(existing is not null && existing.Status!="SUGGESTED") continue;
            if(existing is null)
            {
                existing=new MatchResult{LostReportId=lost.Id,FoundReportId=found.Id};
                db.Matches.Add(existing);
            }
            var shouldNotify=existing.Score<55; // Notify once when first entering the matching list.
            existing.Score=result.Score;
            existing.Coverage=result.Coverage;
            existing.ComparedFeatures=result.ComparedFeatures;
            existing.ScoreDetails=JsonSerializer.Serialize(result.Breakdown);
            if(shouldNotify)
            {
                foreach(var id in new[]{lost.UserId,found.UserId}.Distinct())
                    db.Notifications.Add(new Notification{UserId=id,Type="MATCH",ReportId=id==lost.UserId?lost.Id:found.Id,Content=$"Có gợi ý tương đồng mới ({result.Score:0} điểm). Hãy kiểm tra và xác minh."});
            }
        }
        await db.SaveChangesAsync(token);
    }
}
public record MatchScore(double Score,double Coverage,int ComparedFeatures,Dictionary<string,double> Breakdown,bool Sufficient);
