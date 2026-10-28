using LostFound.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Data;

public class LostFoundDb(DbContextOptions<LostFoundDb> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CampusLocation> Locations => Set<CampusLocation>();
    public DbSet<ItemReport> Reports => Set<ItemReport>();
    public DbSet<ItemImage> Images => Set<ItemImage>();
    public DbSet<MatchResult> Matches => Set<MatchResult>();
    public DbSet<ClaimRequest> Claims => Set<ClaimRequest>();
    public DbSet<HandoverRoom> HandoverRooms => Set<HandoverRoom>();
    public DbSet<HandoverMessage> HandoverMessages => Set<HandoverMessage>();
    public DbSet<HandoverAppointment> HandoverAppointments => Set<HandoverAppointment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CaseTicket> CaseTickets => Set<CaseTicket>();
    public DbSet<CustodyRecord> CustodyRecords => Set<CustodyRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<CaseTicket>().HasIndex(x=>new{x.Status,x.CreatedAt});
        b.Entity<CaseTicket>().HasOne(x=>x.Reporter).WithMany().HasForeignKey(x=>x.ReporterId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CustodyRecord>().HasIndex(x=>x.ReportId).IsUnique();
        b.Entity<CustodyRecord>().HasOne<ItemReport>().WithMany().HasForeignKey(x=>x.ReportId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemReport>().HasIndex(x => new { x.Type, x.Status, x.EventTime });
        b.Entity<ItemReport>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemReport>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemReport>().HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemImage>().HasOne(x => x.Report).WithMany(x => x.Images).HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MatchResult>().HasIndex(x => new { x.LostReportId, x.FoundReportId }).IsUnique();
        b.Entity<MatchResult>().HasOne<ItemReport>().WithMany().HasForeignKey(x => x.LostReportId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MatchResult>().HasOne<ItemReport>().WithMany().HasForeignKey(x => x.FoundReportId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClaimRequest>().HasIndex(x => new { x.FoundReportId, x.ClaimantId });
        b.Entity<ClaimRequest>().HasOne<ItemReport>().WithMany().HasForeignKey(x => x.FoundReportId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ClaimRequest>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.ClaimantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HandoverRoom>().HasIndex(x => x.ClaimId).IsUnique();
        b.Entity<HandoverRoom>().HasOne(x => x.Claim).WithMany().HasForeignKey(x => x.ClaimId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HandoverMessage>().HasIndex(x => new { x.RoomId, x.CreatedAt });
        b.Entity<HandoverMessage>().HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<HandoverMessage>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HandoverAppointment>().HasIndex(x => new { x.RoomId, x.CreatedAt });
        b.Entity<HandoverAppointment>().HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<HandoverAppointment>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.ProposedById).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Notification>().HasIndex(x => new { x.UserId, x.CreatedAt });
        b.Entity<Notification>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<AuditLog>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Category>().HasOne<Category>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CampusLocation>().HasOne<CampusLocation>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
