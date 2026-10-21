using System.Security.Claims;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Controllers;

[Authorize,ApiController,Route("api/uploads")]
public class UploadsController(LostFoundDb db,IWebHostEnvironment env):ControllerBase
{
    private Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost("reports/{reportId:guid}")]
    [RequestSizeLimit(5_500_000)]
    public async Task<IActionResult> Upload(Guid reportId,IFormFile file)
    {
        var report=await db.Reports.FindAsync(reportId);
        if(report is null)return NotFound();
        if(report.UserId!=UserId)return Forbid();
        if(report.Status!="OPEN")return Conflict(new {message="Chỉ thêm ảnh cho bài đang mở."});
        if(file is null||file.Length==0||file.Length>5_000_000)return BadRequest(new {message="Ảnh phải từ 1 byte đến 5 MB."});
        if(await db.Images.CountAsync(x=>x.ReportId==reportId)>=4)return BadRequest(new {message="Mỗi bài tối đa 4 ảnh."});
        await using var stream=file.OpenReadStream();
        var header=new byte[12];var read=await stream.ReadAsync(header.AsMemory());
        var jpeg=read>=3 && header[0]==0xff&&header[1]==0xd8&&header[2]==0xff;
        var png=read>=8 && header.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10});
        var webp=read>=12 && System.Text.Encoding.ASCII.GetString(header,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(header,8,4)=="WEBP";
        if(!jpeg&&!png&&!webp)return BadRequest(new{message="Chỉ hỗ trợ ảnh JPG, PNG, WebP hợp lệ."});
        var ext=jpeg?"jpg":png?"png":"webp";
        var root=Path.Combine(env.ContentRootPath,"wwwroot","uploads");
        Directory.CreateDirectory(root);
        var fileName=$"{Guid.NewGuid():N}.{ext}";
        var diskPath=Path.Combine(root,fileName);
        stream.Position=0;
        await using(var output=System.IO.File.Create(diskPath)){await stream.CopyToAsync(output);}
        var image=new ItemImage{ReportId=reportId,Url="/uploads/"+fileName};
        db.Images.Add(image);await db.SaveChangesAsync();return Ok(new {url=image.Url});
    }
}
