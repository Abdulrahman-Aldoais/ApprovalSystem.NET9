using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان الطلب الرئيسي
/// </summary>
public class Request
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(15,2)")]
    public decimal? Amount { get; set; }

    // JSON data for flexible request fields
    public string? Data { get; set; } // Serialized JSON

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // pending, in_progress, approved, rejected, cancelled

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "medium"; // low, medium, high, urgent

    public DateTime? DueDate { get; set; }

    [MaxLength(100)]
    public string? WorkflowInstanceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    [Required]
    [ForeignKey("Requester")]
    public string RequesterId { get; set; } = string.Empty;

    [Required]
    [ForeignKey("RequestType")]
    public Guid RequestTypeId { get; set; }

    [ForeignKey("ApprovalMatrix")]
    public Guid? ApprovalMatrixId { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User Requester { get; set; } = null!;
    public virtual RequestType RequestType { get; set; } = null!;
    public virtual ApprovalMatrix? ApprovalMatrix { get; set; }

    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public virtual ICollection<RequestAudit> RequestAudits { get; set; } = new List<RequestAudit>();
    public virtual ICollection<WorkflowTracking> WorkflowTrackings { get; set; } = new List<WorkflowTracking>();

    // Helper Properties
    public T? GetData<T>() where T : class
    {
        if (string.IsNullOrEmpty(Data))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(Data);
        }
        catch
        {
            return null;
        }
    }

    public void SetData<T>(T data)
    {
        if (data == null)
        {
            Data = null;
        }
        else
        {
            Data = JsonSerializer.Serialize(data);
        }
    }

    public bool IsOverdue => DueDate.HasValue && DateTime.UtcNow > DueDate.Value && Status != "approved" && Status != "rejected";
}
