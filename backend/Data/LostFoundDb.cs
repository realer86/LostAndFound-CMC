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

    public DbSet<ChatContact> Contacts => Set<ChatContact>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<CampusDirectoryEntry> Directories => Set<CampusDirectoryEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
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
        b.Entity<CaseTicket>().HasIndex(x => new { x.Status, x.CreatedAt });
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
