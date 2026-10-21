using LostFound.Api.Data;
using LostFound.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Services;

// Local prototype scheduler. Checks every five minutes; restart-safe and idempotent.
public sealed class ReportExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<ReportExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give startup schema upgrade a chance to finish.
        try { await Task.Delay(TimeSpan.FromSeconds(35), stoppingToken); } catch(OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                await using var scope=scopeFactory.CreateAsyncScope();
                var db=scope.ServiceProvider.GetRequiredService<LostFoundDb>();
                var now=DateTime.UtcNow;
                var posts=await db.Reports.Where(x=>x.Status=="OPEN" && x.ExpiresAt!=null && x.ExpiresAt<=now.AddDays(3)).Take(500).ToListAsync(stoppingToken);
                foreach(var report in posts)
                {
                    var inProgress=await db.Claims.AnyAsync(c=>c.FoundReportId==report.Id && c.Status=="APPROVED",stoppingToken);
                    var held=await db.CustodyRecords.AnyAsync(c=>c.ReportId==report.Id && c.Status=="RECEIVED",stoppingToken);
                    if(inProgress||held) continue;
                    if(report.ExpiresAt<=now)
                    {
                        report.Status="EXPIRED"; report.UpdatedAt=now;
                        db.Notifications.Add(new Notification{UserId=report.UserId,Type="EXPIRY",ReportId=report.Id,Content=$"Bài '{report.ItemName}' đã hết hạn. Bạn có thể gia hạn từ Bài của tôi."});
                    }
                    else if(report.ExpirationNotifiedAt is null)
                    {
                        report.ExpirationNotifiedAt=now;
                        db.Notifications.Add(new Notification{UserId=report.UserId,Type="EXPIRY",ReportId=report.Id,Content=$"Bài '{report.ItemName}' sắp hết hạn. Gia hạn nếu vẫn cần tìm đồ."});
                    }
                }
                // Remind both participants once, before a mutually accepted appointment.
                var upcoming=await db.HandoverAppointments.Where(a=>a.Status=="ACCEPTED" && a.ReminderSentAt==null &&
                    a.MeetingAt>now && a.MeetingAt<=now.AddHours(1)).Take(200).ToListAsync(stoppingToken);
                foreach(var appointment in upcoming)
                {
                    var room=await db.HandoverRooms.FirstOrDefaultAsync(x=>x.Id==appointment.RoomId,stoppingToken);
                    if(room is null || room.Status is "DISPUTED" or "CANCELLED" or "COMPLETED")continue;
                    var claim=await db.Claims.FirstOrDefaultAsync(x=>x.Id==room.ClaimId,stoppingToken);
                    if(claim is null || claim.Status!="APPROVED")continue;
                    var found=await db.Reports.FirstOrDefaultAsync(x=>x.Id==claim.FoundReportId,stoppingToken);
                    if(found is null)continue;
                    appointment.ReminderSentAt=now;
                    foreach(var uid in new[]{found.UserId,claim.ClaimantId}.Distinct())
                      db.Notifications.Add(new Notification{UserId=uid,ReportId=found.Id,Type="REMINDER",
                        Content=$"Sắp đến lịch nhận '{found.ItemName}' tại {appointment.Place}. Kiểm tra Phòng bàn giao để xác nhận."});
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { return; }
            catch(Exception ex) { logger.LogWarning(ex,"Periodic expiry check unsuccessful; will retry later"); }
        } while(await timer.WaitForNextTickAsync(stoppingToken));
    }
}
