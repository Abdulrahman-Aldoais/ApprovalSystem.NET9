using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان سجل مراجعة الطلبات (Request Audit Trail)
/// </summary>
public class RequestAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty; // create, update, approve, reject, escalate, etc.

    [MaxLength(50)]
    public string? FromStatus { get; set; }

    [MaxLength(50)]
    public string? ToStatus { get; set; }

    // JSON metadata for audit details
    public string? Metadata { get; set; } // Serialized JSON

    [MaxLength(45)]
    public string? IpAddress { get; set; } // IPv6 addresses can be up to 45 chars

    [MaxLength(1000)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    [Required]
    [ForeignKey("Request")]
    public Guid RequestId { get; set; }

    [ForeignKey("Actor")]
    public string? ActorId { get; set; }

    [MaxLength(255)]
    public string? ActorName { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Request Request { get; set; } = null!;
    public virtual User? Actor { get; set; }

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

    public string GetDescription()
    {
        return ActionType switch
        {
            "created" => $"تم إنشاء الطلب من قبل {ActorName}",
            "updated" => $"تم تحديث الطلب من {FromStatus} إلى {ToStatus} من قبل {ActorName}",
            "approved" => $"تم الموافقة على الطلب من قبل {ActorName}",
            "rejected" => $"تم رفض الطلب من قبل {ActorName}",
            "escalated" => $"تم تصعيد الطلب من قبل {ActorName}",
            "cancelled" => $"تم إلغاء الطلب من قبل {ActorName}",
            _ => ActionType
        };
    }
}
