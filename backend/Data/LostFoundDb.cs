using LostFound.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LostFound.Api.Data;

// Trích từ LostFoundDb.cs của bản Final: các bảng trong phạm vi thiết kế Sprint 1.
public class LostFoundDb(DbContextOptions<LostFoundDb> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CampusLocation> Locations => Set<CampusLocation>();
    public DbSet<ItemReport> Reports => Set<ItemReport>();
    public DbSet<ItemImage> Images => Set<ItemImage>();
    public DbSet<CaseTicket> CaseTickets => Set<CaseTicket>();
    public DbSet<ClaimRequest> Claims => Set<ClaimRequest>();
    public DbSet<HandoverRoom> HandoverRooms => Set<HandoverRoom>();
    public DbSet<HandoverMessage> HandoverMessages => Set<HandoverMessage>();
    public DbSet<HandoverAppointment> HandoverAppointments => Set<HandoverAppointment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CustodyRecord> CustodyRecords => Set<CustodyRecord>();

    public DbSet<SupportThread> SupportThreads => Set<SupportThread>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    public DbSet<ChatContact> Contacts => Set<ChatContact>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<CampusDirectoryEntry> Directories => Set<CampusDirectoryEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<SupportThread>().HasIndex(x => x.CaseId).IsUnique();
        b.Entity<SupportThread>().HasIndex(x => x.CustodyId).IsUnique();
        b.Entity<SupportThread>().HasIndex(x => new { x.UserId, x.LastMessageAt });
        b.Entity<SupportThread>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SupportThread>().HasOne<CaseTicket>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SupportThread>().HasOne<CustodyRecord>().WithMany().HasForeignKey(x => x.CustodyId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SupportMessage>().HasIndex(x => new { x.ThreadId, x.CreatedAt });
        b.Entity<SupportMessage>().HasOne<SupportThread>().WithMany().HasForeignKey(x => x.ThreadId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SupportMessage>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ChatContact>().ToTable("Contacts").HasIndex(x => new { x.ReportId, x.VisitorId }).IsUnique();
        b.Entity<ChatContact>().HasIndex(x => new { x.OwnerId, x.LastMessageAt });
        b.Entity<ChatContact>().HasOne(x => x.Report).WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ChatContact>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ChatContact>().HasOne(x => x.Visitor).WithMany().HasForeignKey(x => x.VisitorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ChatMessage>().ToTable("Messages").HasIndex(x => new { x.ContactId, x.CreatedAt });
        b.Entity<ChatMessage>().HasIndex(x => new { x.SenderId, x.CreatedAt });
        b.Entity<ChatMessage>().HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ChatMessage>().HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ChatMessage>().HasOne(x => x.Recipient).WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CampusDirectoryEntry>().ToTable("Directories");
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.GoogleSubject).IsUnique();
        b.Entity<CaseTicket>().HasIndex(x => new { x.Status, x.CreatedAt });
        b.Entity<ClaimRequest>().ToTable("ReportClaims").HasIndex(x => new { x.FoundReportId, x.ClaimantId });
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
        b.Entity<CustodyRecord>().HasIndex(x => x.ReportId).IsUnique();
        b.Entity<CustodyRecord>().HasOne<ItemReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<CaseTicket>().HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CaseTicket>().HasOne<ItemReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<ItemReport>().HasIndex(x => new { x.Type, x.Status, x.EventTime });
        b.Entity<ItemReport>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemReport>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemReport>().HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemImage>().HasOne(x => x.Report).WithMany(x => x.Images).HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Category>().HasOne<Category>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CampusLocation>().HasOne<CampusLocation>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
