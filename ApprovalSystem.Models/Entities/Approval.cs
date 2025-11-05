using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان الموافقة على الطلب
/// </summary>
public class Approval
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public int Stage { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, approved, rejected, escalated

    [MaxLength(1000)]
    public string? Comments { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Request")]
    public Guid RequestId { get; set; }

    [Required]
    [ForeignKey("Approver")]
    public string ApproverId { get; set; } = string.Empty;

    // Navigation Properties
    public virtual Request Request { get; set; } = null!;
    public virtual User Approver { get; set; } = null!;

    // Additional Properties
    [MaxLength(255)]
    public string? RejectionReason { get; set; }

    public bool IsOverdue => Status == "pending" && CreatedAt.AddDays(3) < DateTime.UtcNow; // 3 days overdue

    public TimeSpan? ProcessingTime => ApprovedAt?.Subtract(CreatedAt);

    public void Approve(string? comments = null)
    {
        Status = "approved";
        Comments = comments;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(string reason, string? comments = null)
    {
        Status = "rejected";
        Comments = comments;
        RejectionReason = reason;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Escalate(string reason)
    {
        Status = "escalated";
        Comments = reason;
        ApprovedAt = DateTime.UtcNow;
    }
}
