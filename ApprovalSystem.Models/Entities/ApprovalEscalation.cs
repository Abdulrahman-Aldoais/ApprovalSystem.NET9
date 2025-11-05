using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان تصعيد الموافقات
/// </summary>
public class ApprovalEscalation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // pending, escalated, resolved

    [MaxLength(1000)]
    public string? Reason { get; set; }

    [MaxLength(255)]
    public string? EscalatedTo { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EscalatedAt { get; set; }

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    [Required]
    [ForeignKey("Request")]
    public Guid RequestId { get; set; }

    [Required]
    [ForeignKey("Approval")]
    public Guid ApprovalId { get; set; }

    [ForeignKey("EscalatedBy")]
    public string? EscalatedById { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Request Request { get; set; } = null!;
    public virtual Approval Approval { get; set; } = null!;
    public virtual User? EscalatedBy { get; set; }

    public void Escalate(string reason, string escalatedTo, string escalatedById)
    {
        Status = "escalated";
        Reason = reason;
        EscalatedTo = escalatedTo;
        EscalatedById = escalatedById;
        EscalatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        Status = "resolved";
        ResolvedAt = DateTime.UtcNow;
    }
}
