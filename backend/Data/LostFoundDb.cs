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
    public DbSet<AnonymousMessage> Messages => Set<AnonymousMessage>();
    public DbSet<OfficeDirectoryEntry> OfficeDirectory => Set<OfficeDirectoryEntry>();
    public DbSet<ItemClaim> Claims => Set<ItemClaim>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<AnonymousMessage>().ToTable("Messages", t =>
            t.HasCheckConstraint("CK_Messages_DifferentUsers", "\"SenderId\" <> \"RecipientId\""));
        b.Entity<AnonymousMessage>().HasIndex(x => new { x.ReportId, x.CreatedAt });
        b.Entity<AnonymousMessage>().HasIndex(x => new { x.RecipientId, x.CreatedAt });
        b.Entity<AnonymousMessage>().HasOne<ItemReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AnonymousMessage>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AnonymousMessage>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<OfficeDirectoryEntry>().HasIndex(x => x.Name).IsUnique();
        b.Entity<OfficeDirectoryEntry>().ToTable("OfficeDirectory", t =>
            t.HasCheckConstraint("CK_OfficeDirectory_Type", "\"Type\" IN ('SECURITY', 'MANAGEMENT')"));
        b.Entity<ItemClaim>().HasIndex(x => new { x.ItemId, x.ClaimerId }).IsUnique();
        b.Entity<ItemClaim>().HasIndex(x => x.ItemId)
            .HasDatabaseName("UX_Claims_OnlyOneApproved").IsUnique().HasFilter("\"Status\" = 'APPROVED'");
        b.Entity<ItemClaim>().HasOne<ItemReport>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemClaim>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.ClaimerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ItemClaim>().ToTable("Claims", t =>
            t.HasCheckConstraint("CK_Claims_Status", "\"Status\" IN ('PENDING', 'APPROVED', 'REJECTED')"));
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
