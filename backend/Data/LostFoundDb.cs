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

    protected override void OnModelCreating(ModelBuilder b)
    {
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
