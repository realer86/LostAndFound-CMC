using System.Security.Claims;
using System.Threading.RateLimiting;
using LostFound.Api.Data;
using LostFound.Api.Domain;
using LostFound.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<LostFoundDb>(options=>options.UseNpgsql(builder.Configuration.GetConnectionString("Main")));
builder.Services.AddScoped<MatchEngine>();
builder.Services.AddHostedService<ReportExpiryWorker>();
builder.Services.AddScoped<IPasswordHasher<AppUser>,PasswordHasher<AppUser>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options=>{
    options.Cookie.Name="lostfound.session";
    options.Cookie.HttpOnly=true;
    options.Cookie.SameSite=SameSiteMode.Strict;
    options.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan=TimeSpan.FromHours(12);
    options.Events.OnRedirectToLogin=context=>{context.Response.StatusCode=401;return Task.CompletedTask;};
    options.Events.OnRedirectToAccessDenied=context=>{context.Response.StatusCode=403;return Task.CompletedTask;};
})
.AddCookie("AdminCookie",options=>{
    // Dedicated admin browser session: public-site login cannot authorize admin API.
    options.Cookie.Name="lostfound.admin.session";
    options.Cookie.HttpOnly=true;
    options.Cookie.SameSite=SameSiteMode.Strict;
    options.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest; // localhost HTTP only
    options.ExpireTimeSpan=TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin=context=>{context.Response.StatusCode=401;return Task.CompletedTask;};
    options.Events.OnRedirectToAccessDenied=context=>{context.Response.StatusCode=403;return Task.CompletedTask;};
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options=>{
    options.RejectionStatusCode=429;
    options.AddFixedWindowLimiter("sensitive",config=>{config.PermitLimit=10;config.Window=TimeSpan.FromMinutes(1);config.QueueLimit=0;});
    options.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(ctx=>RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=150,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
});
var app=builder.Build();
app.Use(async(context,next)=>{
    // Refuse cross-origin browser mutations made using the session cookie.
    if(!HttpMethods.IsGet(context.Request.Method)&&!HttpMethods.IsHead(context.Request.Method)
      && context.Request.Headers.TryGetValue("Origin",out var origin))
    {
        var raw=origin.ToString();
        if(!Uri.TryCreate(raw,UriKind.Absolute,out var uri) ||
           !(uri.Host=="localhost"||uri.Host=="127.0.0.1") ||
           !(uri.Port==5173||uri.Port==5174||uri.Port==8080))
        {context.Response.StatusCode=403;await context.Response.WriteAsJsonAsync(new{message="Origin không được phép."});return;}
    }
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async(ctx,next)=>{
    if(ctx.User.Identity?.IsAuthenticated==true &&
       (ctx.Request.Path.StartsWithSegments("/api/reports") ||
        ctx.Request.Path.StartsWithSegments("/api/claims") ||
        ctx.Request.Path.StartsWithSegments("/api/handovers") ||
        ctx.Request.Path.StartsWithSegments("/api/operations") ||
        ctx.Request.Path.StartsWithSegments("/api/uploads")) &&
        !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method))
    {
        var raw=ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if(Guid.TryParse(raw,out var userId))
        {
            await using var scope=ctx.RequestServices.CreateAsyncScope();
            var db=scope.ServiceProvider.GetRequiredService<LostFoundDb>();
            var suspended=await db.Users.AnyAsync(u=>u.Id==userId && u.SuspendedUntil!=null && u.SuspendedUntil>DateTime.UtcNow);
            if(suspended){ctx.Response.StatusCode=403;await ctx.Response.WriteAsJsonAsync(new{message="Tài khoản đang bị hạn chế thao tác. Vui lòng liên hệ quản trị viên."});return;}
        }
    }
    await next();
});
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health",()=>Results.Ok(new{status="ok",app="LostFound Campus",utc=DateTime.UtcNow}));

await using(var scope=app.Services.CreateAsyncScope())
{
    var db=scope.ServiceProvider.GetRequiredService<LostFoundDb>();
    // EnsureCreated for zero-setup local demo; move to EF migrations for real releases.
    const int attempts=25;
    for(var i=0;i<attempts;i++)
    {
        try{await db.Database.EnsureCreatedAsync();break;}
        catch when(i<attempts-1){await Task.Delay(2000);}
    }
    // Incremental schema add-on, preserving the existing Docker database and users/reports.
    // EnsureCreatedAsync() does NOT add new tables to an already-created database.
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "HandoverRooms" (
          "Id" uuid PRIMARY KEY,
          "ClaimId" uuid NOT NULL UNIQUE REFERENCES "Claims"("Id") ON DELETE RESTRICT,
          "Status" character varying(24) NOT NULL,
          "HolderConfirmedAt" timestamp with time zone,
          "ClaimantConfirmedAt" timestamp with time zone,
          "CompletedAt" timestamp with time zone,
          "CreatedAt" timestamp with time zone NOT NULL
        );
        CREATE TABLE IF NOT EXISTS "HandoverMessages" (
          "Id" uuid PRIMARY KEY,
          "RoomId" uuid NOT NULL REFERENCES "HandoverRooms"("Id") ON DELETE CASCADE,
          "SenderId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
          "Content" character varying(1500) NOT NULL,
          "CreatedAt" timestamp with time zone NOT NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_HandoverMessages_RoomId_CreatedAt" ON "HandoverMessages"("RoomId", "CreatedAt");
        CREATE TABLE IF NOT EXISTS "HandoverAppointments" (
          "Id" uuid PRIMARY KEY,
          "RoomId" uuid NOT NULL REFERENCES "HandoverRooms"("Id") ON DELETE CASCADE,
          "ProposedById" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
          "Place" character varying(120) NOT NULL,
          "MeetingAt" timestamp with time zone NOT NULL,
          "Status" character varying(20) NOT NULL,
          "CreatedAt" timestamp with time zone NOT NULL,
          "RespondedAt" timestamp with time zone
        );
        CREATE INDEX IF NOT EXISTS "IX_HandoverAppointments_RoomId_CreatedAt" ON "HandoverAppointments"("RoomId", "CreatedAt");
    """);
    // Additive, idempotent schema upgrade: no Docker volume deletion required.
    await db.Database.ExecuteSqlRawAsync("""
        ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SuspendedUntil" timestamp with time zone;
        ALTER TABLE "Reports" ADD COLUMN IF NOT EXISTS "ExpiresAt" timestamp with time zone;
        ALTER TABLE "HandoverAppointments" ADD COLUMN IF NOT EXISTS "ReminderSentAt" timestamp with time zone;
        ALTER TABLE "Reports" ADD COLUMN IF NOT EXISTS "ExpirationNotifiedAt" timestamp with time zone;
        UPDATE "Reports" SET "ExpiresAt" = COALESCE("ExpiresAt", GREATEST("CreatedAt", now()) + interval '30 days') WHERE "Status"='OPEN';
        CREATE TABLE IF NOT EXISTS "CaseTickets" (
            "Id" uuid PRIMARY KEY, "ReporterId" uuid NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
            "ReportId" uuid, "RoomId" uuid,
            "Type" varchar(24) NOT NULL, "Reason" varchar(2000) NOT NULL,
            "Status" varchar(24) NOT NULL, "Resolution" varchar(1500),
            "ResolvedById" uuid, "CreatedAt" timestamptz NOT NULL, "ResolvedAt" timestamptz
        );
        CREATE INDEX IF NOT EXISTS "IX_CaseTickets_Status_CreatedAt" ON "CaseTickets"("Status", "CreatedAt");
        CREATE TABLE IF NOT EXISTS "CustodyRecords" (
            "Id" uuid PRIMARY KEY, "ReportId" uuid NOT NULL UNIQUE REFERENCES "Reports"("Id") ON DELETE RESTRICT,
            "RequestedById" uuid NOT NULL, "Desk" varchar(120) NOT NULL,
            "Status" varchar(24) NOT NULL, "ReceiptCode" varchar(120),
            "AcceptedByAdminId" uuid, "ReceivedAt" timestamptz,
            "ReleasedAt" timestamptz, "CreatedAt" timestamptz NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CustodyRecords_ReportId" ON "CustodyRecords"("ReportId");
    """);
    // Backfill rooms for previously APPROVED claims, without touching completed claims.
    var approvedIds=await db.Claims.AsNoTracking()
        .Where(c=>c.Status=="APPROVED" && c.HandedOverAt==null).Select(c=>c.Id).ToListAsync();
    var existingRoomClaims=await db.HandoverRooms.Select(r=>r.ClaimId).ToListAsync();
    foreach(var claimId in approvedIds.Except(existingRoomClaims))db.HandoverRooms.Add(new HandoverRoom{ClaimId=claimId});
    await db.SaveChangesAsync();
    if(!await db.Categories.AnyAsync()){
        db.Categories.AddRange(new[]{"Điện tử","Giấy tờ","Ví & túi","Chìa khóa","Trang sức","Sách & đồ dùng","Khác"}.Select(n=>new Category{Name=n}));
        db.Locations.AddRange(new[]{"Thư viện","Giảng đường A","Giảng đường B","Căn tin","Ký túc xá","Sân thể thao","Bãi xe","Khu vực khác"}.Select(n=>new CampusLocation{Name=n}));
        await db.SaveChangesAsync();
    }
    if(app.Environment.IsDevelopment())
    {
        var hasher=scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var users=new[]{("minh@campus.edu","Minh - Người mất đồ","Demo12345!","USER"),("lan@campus.edu","Lan - Người nhặt đồ","Demo12345!","USER"),("admin@campus.edu","Quản trị viên","Admin12345!","ADMIN")};
        foreach(var (email,name,password,role) in users)
        {
            if(await db.Users.AnyAsync(x=>x.Email==email))continue;
            var user=new AppUser{Email=email,FullName=name,Role=role,EmailVerifiedAt=DateTime.UtcNow};
            user.PasswordHash=hasher.HashPassword(user,password);db.Users.Add(user);
        }
        await db.SaveChangesAsync();
        if(!await db.Reports.AnyAsync())
        {
            var minh=await db.Users.FirstAsync(x=>x.Email=="minh@campus.edu");
            var lan=await db.Users.FirstAsync(x=>x.Email=="lan@campus.edu");
            var library=await db.Locations.FirstAsync(x=>x.Name=="Thư viện");
            var cat=await db.Categories.FirstAsync(x=>x.Name=="Ví & túi");
            db.Reports.AddRange(
                new ItemReport{UserId=minh.Id,Type="LOST",ItemName="Ví da màu đen",Description="Mất một chiếc ví đen có thẻ sinh viên gần thư viện",Color="đen",LocationId=library.Id,CategoryId=cat.Id,EventTime=DateTime.UtcNow.AddHours(-5)},
                new ItemReport{UserId=lan.Id,Type="FOUND",ItemName="Bóp đen",Description="Nhặt được bóp da gần thư viện",Color="đen",LocationId=library.Id,CategoryId=cat.Id,EventTime=DateTime.UtcNow.AddHours(-4),PrivateIdentifier="Một đặc điểm kín để xác minh"},
                new ItemReport{UserId=lan.Id,Type="FOUND",ItemName="Chùm chìa khóa",Description="Nhặt được chìa khóa ở khu giảng đường A",LocationDetail="Giảng đường A, tầng 1",EventTime=DateTime.UtcNow.AddDays(-1)});
            await db.SaveChangesAsync();
        }
        var engine=scope.ServiceProvider.GetRequiredService<MatchEngine>();
        foreach(var reportId in await db.Reports.Where(x=>x.Status=="OPEN").Select(x=>x.Id).ToListAsync())await engine.MatchAsync(reportId);
        // One-time notification backfill for matching rows from earlier prototype versions.
        // The original bell only notified scores >=80, while "Gợi ý phù hợp" showed >=55.
        var activeMatches=await db.Matches.AsNoTracking()
            .Where(x=>x.Status=="SUGGESTED" && x.Score>=55).ToListAsync();
        var pairIds=activeMatches.SelectMany(x=>new[]{x.LostReportId,x.FoundReportId}).Distinct().ToArray();
        var owners=await db.Reports.AsNoTracking().Where(x=>pairIds.Contains(x.Id) && x.Status=="OPEN")
            .Select(x=>new{x.Id,x.UserId}).ToDictionaryAsync(x=>x.Id,x=>x.UserId);
        var existingNotices=await db.Notifications.AsNoTracking()
            .Where(x=>x.Type=="MATCH" && x.ReportId!=null)
            .Select(x=>new{x.UserId,x.ReportId}).ToListAsync();
        var noticed=new HashSet<string>(existingNotices.Select(x=>$"{x.UserId}:{x.ReportId}"));
        foreach(var pair in activeMatches)
        {
            if(!owners.TryGetValue(pair.LostReportId,out var lostUser) ||
               !owners.TryGetValue(pair.FoundReportId,out var foundUser))continue;
            foreach(var (userId,reportId) in new[]{(lostUser,pair.LostReportId),(foundUser,pair.FoundReportId)})
            {
                if(!noticed.Add($"{userId}:{reportId}"))continue;
                db.Notifications.Add(new Notification{UserId=userId,ReportId=reportId,Type="MATCH",
                    Content="Có bài LOST–FOUND phù hợp. Hãy vào Gợi ý phù hợp và xác minh trước khi nhận đồ."});
            }
        }
        await db.SaveChangesAsync();
    }
}
app.Run();
