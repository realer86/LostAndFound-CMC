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

// Sprint 2 #13: hội thoại ẩn danh theo bài đăng. Chỉ hai thành viên được đọc.
public class ChatContact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public ItemReport? Report { get; set; }
    public Guid OwnerId { get; set; }
    public AppUser? Owner { get; set; }
    public Guid VisitorId { get; set; }
    public AppUser? Visitor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }
}

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContactId { get; set; }
    public ChatContact? Contact { get; set; }
    public Guid ReportId { get; set; }
    public Guid SenderId { get; set; }
    public AppUser? Sender { get; set; }
    public Guid RecipientId { get; set; }
    public AppUser? Recipient { get; set; }
    [MaxLength(1500)] public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Directory is a public CAMPUS listing, not users' private contact details.
public class CampusDirectoryEntry
{
    public int Id { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(200)] public string Location { get; set; } = "";
    [MaxLength(40)] public string InternalPhone { get; set; } = "";
    [MaxLength(140)] public string OfficeHours { get; set; } = "";
    [MaxLength(30)] public string Category { get; set; } = "Khác";
    public bool IsActive { get; set; } = true;
}
