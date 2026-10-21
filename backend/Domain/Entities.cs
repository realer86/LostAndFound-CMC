using System.ComponentModel.DataAnnotations;

namespace LostFound.Api.Domain;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(120)] public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [MaxLength(16)] public string Role { get; set; } = "USER";
    public DateTime? SuspendedUntil { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    [MaxLength(100)] public string? VerifyTokenHash { get; set; }
    public DateTime? VerifyExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Category
{
    public int Id { get; set; }
    [MaxLength(80)] public string Name { get; set; } = "";
    public int? ParentId { get; set; }
}
public class CampusLocation
{
    public int Id { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
    public int? ParentId { get; set; }
}
public class ItemReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    [MaxLength(8)] public string Type { get; set; } = "LOST";
    [MaxLength(16)] public string Status { get; set; } = "OPEN";
    [MaxLength(140)] public string ItemName { get; set; } = "";
    [MaxLength(3500)] public string Description { get; set; } = "";
    [MaxLength(200)] public string? PrivateIdentifier { get; set; }
    [MaxLength(80)] public string? Color { get; set; }
    [MaxLength(80)] public string? Brand { get; set; }
    [MaxLength(300)] public string? LocationDetail { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? LocationId { get; set; }
    public CampusLocation? Location { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime? ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? ExpirationNotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ItemImage> Images { get; set; } = new();
}
public class ItemImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public ItemReport? Report { get; set; }
    [MaxLength(500)] public string Url { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Báo cáo/phiếu phản ánh của người dùng (tách từ Final, chưa có UI xử lý ở Sprint 1).
public class CaseTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterId { get; set; }
    public AppUser? Reporter { get; set; }
    public Guid? ReportId { get; set; }
    public Guid? RoomId { get; set; }
    [MaxLength(24)] public string Type { get; set; } = "REPORT_ABUSE";
    [MaxLength(2000)] public string Reason { get; set; } = "";
    [MaxLength(24)] public string Status { get; set; } = "OPEN";
    [MaxLength(1500)] public string? Resolution { get; set; }
    public Guid? ResolvedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
