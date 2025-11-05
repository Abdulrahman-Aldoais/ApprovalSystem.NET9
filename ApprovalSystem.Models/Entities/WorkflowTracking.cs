using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان تتبع مسارات العمل (Workflow Tracking)
/// </summary>
public class WorkflowTracking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public string? CurrentStage { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "active"; // active, completed, cancelled, paused

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent

    public DateTime? CompletedAt { get; set; }

    public bool SlaBreachAlert { get; set; } = false;

    public int EscalationCount { get; set; } = 0;

    public DateTime? Deadline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    [Required]
    [ForeignKey("Request")]
    public Guid RequestId { get; set; }

    [MaxLength(100)]
    public string? WorkflowInstanceId { get; set; }

    [ForeignKey("AssignedToUser")]
    public string? AssignedToUserId { get; set; }

    // JSON metadata for workflow tracking
    public string? Metadata { get; set; } // Serialized JSON

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Request Request { get; set; } = null!;
    public virtual User? AssignedToUser { get; set; }

    // Helper Properties
    public T? GetMetadata<T>() where T : class
    {
        if (string.IsNullOrEmpty(Metadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(Metadata);
        }
        catch
        {
            return null;
        }
    }

    public void SetMetadata<T>(T data)
    {
        if (data == null)
        {
            Metadata = null;
        }
        else
        {
            Metadata = JsonSerializer.Serialize(data);
        }
    }

    public bool IsOverdue => Deadline.HasValue && DateTime.UtcNow > Deadline.Value && Status != "completed";

    public TimeSpan? ProcessingTime => CompletedAt?.Subtract(CreatedAt);
}
