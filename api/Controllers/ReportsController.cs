using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using LostFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

public record ReportRequest(string Type,string ItemName,string? Description,string? Color,string? Brand,string? LocationDetail,int? CategoryId,int? LocationId,DateTime EventTime,string? PrivateIdentifier);

[ApiController, Route("api/reports")]
public class ReportsController(LostFoundDb db,MatchEngine engine) : ControllerBase
{
    private Guid? UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
    private bool Admin=>User.IsInRole("ADMIN");
    private static object PublicReport(ItemReport x)=>new {
        x.Id,x.Type,x.Status,x.ItemName,x.Description,x.Color,x.Brand,x.CategoryId,
        category=x.Category?.Name,x.LocationId,location=x.Location?.Name,x.LocationDetail,
        x.EventTime,x.CreatedAt,x.UpdatedAt,x.ExpiresAt,x.UserId,
        images=x.Images.Select(im=>im.Url).ToArray()
    };
    private static string? Check(ReportRequest r)
    {
        if(r.Type is not ("LOST" or "FOUND"))return "Loại bài phải là LOST hoặc FOUND.";
        if(string.IsNullOrWhiteSpace(r.ItemName)||r.ItemName.Trim().Length>140)return "Tên vật phẩm cần 1–140 ký tự.";
        if((r.Description?.Length??0)>3500||(r.PrivateIdentifier?.Length??0)>200||(r.LocationDetail?.Length??0)>300||(r.Color?.Length??0)>80||(r.Brand?.Length??0)>80)return "Một số nội dung vượt giới hạn ký tự.";
        if(r.EventTime==default||r.EventTime.ToUniversalTime()>DateTime.UtcNow.AddHours(2)||r.EventTime.ToUniversalTime()<DateTime.UtcNow.AddYears(-5))return "Thời gian không hợp lệ.";
        return null;
    }
    private static DateTime Utc(DateTime t)=>t.Kind==DateTimeKind.Utc?t:t.Kind==DateTimeKind.Unspecified?DateTime.SpecifyKind(t,DateTimeKind.Utc):t.ToUniversalTime();
    private static void Copy(ItemReport x,ReportRequest r){x.Type=r.Type;x.ItemName=r.ItemName.Trim();x.Description=r.Description?.Trim()??"";x.Color=r.Color?.Trim();x.Brand=r.Brand?.Trim();x.LocationDetail=r.LocationDetail?.Trim();x.CategoryId=r.CategoryId;x.LocationId=r.LocationId;x.EventTime=Utc(r.EventTime);if(r.Type=="LOST") x.PrivateIdentifier=null; else if(!string.IsNullOrWhiteSpace(r.PrivateIdentifier)) x.PrivateIdentifier=r.PrivateIdentifier.Trim();x.UpdatedAt=DateTime.UtcNow;}

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery]string? q,[FromQuery]string? type,[FromQuery]int? categoryId,[FromQuery]int? locationId,[FromQuery]int page=1,[FromQuery]int pageSize=12)
    {
        page=Math.Max(page,1);pageSize=Math.Clamp(pageSize,1,40);
        var query=db.Reports.AsNoTracking().Include(x=>x.Images).Include(x=>x.Category).Include(x=>x.Location).Where(x=>x.Status=="OPEN");
        if(type is "LOST" or "FOUND")query=query.Where(x=>x.Type==type);
        if(categoryId.HasValue)query=query.Where(x=>x.CategoryId==categoryId);
        if(locationId.HasValue)query=query.Where(x=>x.LocationId==locationId);
        if(!string.IsNullOrWhiteSpace(q)) {var term=$"%{q.Trim()}%";query=query.Where(x=>EF.Functions.ILike(x.ItemName,term)||EF.Functions.ILike(x.Description,term)||EF.Functions.ILike(x.LocationDetail??"",term));}
        var total=await query.CountAsync();
        var items=await query.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new {total,page,pageSize,items=items.Select(PublicReport)});
    }
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog()=>Ok(new {categories=await db.Categories.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),locations=await db.Locations.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});

    [Authorize, HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var items=await db.Reports.AsNoTracking().Include(x=>x.Images).Include(x=>x.Location).Include(x=>x.Category).Where(x=>x.UserId==UserId).OrderByDescending(x=>x.CreatedAt).ToListAsync();
        return Ok(items.Select(PublicReport));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var r=await db.Reports.AsNoTracking().Include(x=>x.Images).Include(x=>x.Location).Include(x=>x.Category).FirstOrDefaultAsync(x=>x.Id==id);
        if(r is null||r.Status!="OPEN"&&r.UserId!=UserId&&!Admin)return NotFound(new {message="Không tìm thấy bài."});
        return Ok(PublicReport(r));
    }

    [Authorize,HttpPost]
    public async Task<IActionResult> Create(ReportRequest r)
    {
        var validation=Check(r);if(validation is not null)return BadRequest(new {message=validation});
        if(r.CategoryId.HasValue&&!await db.Categories.AnyAsync(x=>x.Id==r.CategoryId))return BadRequest(new {message="Nhóm đồ không tồn tại."});
        if(r.LocationId.HasValue&&!await db.Locations.AnyAsync(x=>x.Id==r.LocationId))return BadRequest(new {message="Địa điểm không tồn tại."});
        var entity=new ItemReport{UserId=UserId!.Value};Copy(entity,r);
        db.Reports.Add(entity);
        await db.SaveChangesAsync();
        await engine.MatchAsync(entity.Id);
        return CreatedAtAction(nameof(Detail),new{id=entity.Id},new {id=entity.Id,message="Đăng tin thành công."});
    }
    [Authorize,HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id,ReportRequest r)
    {
        var entity=await db.Reports.FindAsync(id);
        if(entity is null)return NotFound();
        if(entity.UserId!=UserId&&!Admin)return Forbid();
        if(entity.Status!="OPEN")return Conflict(new {message="Chỉ chỉnh sửa bài đang mở."});
        if(await db.CustodyRecords.AnyAsync(c=>c.ReportId==id&&c.Status=="RECEIVED") ||
           await db.Claims.AnyAsync(c=>c.FoundReportId==id&&c.Status=="APPROVED"))
            return Conflict(new{message="Bài đang có đồ ký gửi hoặc yêu cầu bàn giao đã duyệt; không thể sửa đặc điểm."});
        var validation=Check(r);if(validation is not null)return BadRequest(new {message=validation});
        if(entity.Type!=r.Type)return BadRequest(new {message="Không thể đổi loại LOST/FOUND của bài đã đăng."});
        Copy(entity,r);
        var previous=await db.Matches.Where(x=>(x.LostReportId==id||x.FoundReportId==id)&&x.Status=="SUGGESTED").ToListAsync();
        db.Matches.RemoveRange(previous);
        await db.SaveChangesAsync();
        await engine.MatchAsync(id);
        return Ok(new {message="Đã cập nhật bài."});
    }
    [Authorize,HttpPatch("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        var entity=await db.Reports.FindAsync(id);
        if(entity is null)return NotFound();
        if(entity.UserId!=UserId&&!Admin)return Forbid();
        if(entity.Status!="OPEN")return Conflict(new {message="Bài không còn mở."});
        if(await db.Claims.AnyAsync(c=>c.FoundReportId==id&&c.Status=="APPROVED") ||
           await db.CustodyRecords.AnyAsync(c=>c.ReportId==id&&c.Status=="RECEIVED"))
            return Conflict(new{message="Cần hoàn tất hoặc xử lý hồ sơ bàn giao/ký gửi trước khi đóng bài."});
        entity.Status="CLOSED";entity.UpdatedAt=DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog{ActorId=UserId!.Value,Action="REPORT_CLOSED",TargetType="REPORT",TargetId=id});
        await db.SaveChangesAsync();return Ok(new {message="Đã đóng bài."});
    }
    [Authorize,HttpGet("{id:guid}/matches")]
    public async Task<IActionResult> Matches(Guid id)
    {
        var report=await db.Reports.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);
        if(report is null)return NotFound();
        if(report.UserId!=UserId&&!Admin)return Forbid();
        // Guard both ends: malformed/old match rows must never recommend LOST to LOST or FOUND to FOUND.
        if(report.Type is not ("LOST" or "FOUND"))return BadRequest(new {message="Loại bài không hợp lệ."});
        var oppositeType=report.Type=="LOST"?"FOUND":"LOST";
        var matches=await db.Matches.AsNoTracking().Where(x=>(x.LostReportId==id||x.FoundReportId==id)&&x.Status=="SUGGESTED").OrderByDescending(x=>x.Score).ToListAsync();
        var ids=matches.Select(x=>x.LostReportId==id?x.FoundReportId:x.LostReportId).ToList();
        var related=await db.Reports.AsNoTracking().Include(x=>x.Images).Include(x=>x.Category).Include(x=>x.Location).Where(x=>ids.Contains(x.Id) && x.Status=="OPEN" && x.Type==oppositeType).ToDictionaryAsync(x=>x.Id);
        var output=matches.Select(x=>new {x.Id,x.Score,x.Coverage,x.ComparedFeatures,x.Status,report=related.GetValueOrDefault(x.LostReportId==id?x.FoundReportId:x.LostReportId)}).Where(x=>x.report is not null).Select(x=>new {x.Id,x.Score,x.Coverage,x.ComparedFeatures,x.Status,report=PublicReport(x.report!)});
        return Ok(output);
    }
    [Authorize,HttpPatch("matches/{matchId:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid matchId)
    {
        var match=await db.Matches.FindAsync(matchId);
        if(match is null)return NotFound();
        var owner=await db.Reports.AnyAsync(x=>(x.Id==match.LostReportId||x.Id==match.FoundReportId)&&x.UserId==UserId);
        if(!owner&&!Admin)return Forbid();
        match.Status="DISMISSED";await db.SaveChangesAsync();return Ok(new {message="Đã ẩn gợi ý."});
    }
}
