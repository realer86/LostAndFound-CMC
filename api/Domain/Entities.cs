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
public class MatchResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LostReportId { get; set; }
    public Guid FoundReportId { get; set; }
    public double Score { get; set; }
    public double Coverage { get; set; }
    public int ComparedFeatures { get; set; }
    public string ScoreDetails { get; set; } = "{}";
    [MaxLength(18)] public string Status { get; set; } = "SUGGESTED";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class ClaimRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FoundReportId { get; set; }
    public Guid ClaimantId { get; set; }
    [MaxLength(4000)] public string Evidence { get; set; } = "";
    [MaxLength(16)] public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? HandedOverAt { get; set; }
}
// A room is available only to the claimant and finder of an APPROVED claim.
// The verified profiles show display names, not contact details.
public class HandoverRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public ClaimRequest? Claim { get; set; }
    [MaxLength(24)] public string Status { get; set; } = "ARRANGING";
    public DateTime? HolderConfirmedAt { get; set; }
    public DateTime? ClaimantConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class HandoverMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public HandoverRoom? Room { get; set; }
    public Guid SenderId { get; set; }
    [MaxLength(1500)] public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class HandoverAppointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public HandoverRoom? Room { get; set; }
    public Guid ProposedById { get; set; }
    [MaxLength(120)] public string Place { get; set; } = "";
    public DateTime MeetingAt { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "PROPOSED";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
}
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    [MaxLength(60)] public string Type { get; set; } = "INFO";
    [MaxLength(500)] public string Content { get; set; } = "";
    public Guid? ReportId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorId { get; set; }
    [MaxLength(70)] public string Action { get; set; } = "";
    [MaxLength(70)] public string TargetType { get; set; } = "";
    public Guid TargetId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Flags and disputes are visible only to the reporter and the admin portal.
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
// An official staffed desk can accept physical custody; no direct address disclosure.
public class CustodyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public Guid RequestedById { get; set; }
    [MaxLength(120)] public string Desk { get; set; } = "";
    [MaxLength(24)] public string Status { get; set; } = "REQUESTED";
    [MaxLength(120)] public string? ReceiptCode { get; set; }
    public Guid? AcceptedByAdminId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
