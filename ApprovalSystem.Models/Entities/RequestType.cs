using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان نوع الطلب
/// </summary>
public class RequestType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // JSON configuration for amount thresholds
    public string? AmountThresholds { get; set; } // Serialized JSON

    // JSON configuration for required fields
    public string? RequiredFields { get; set; } // Serialized JSON

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    [Required]
    [ForeignKey("Module")]
    public Guid ModuleId { get; set; }

    [ForeignKey("ApprovalMatrix")]
    public Guid? ApprovalMatrixId { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual Module Module { get; set; } = null!;
    public virtual ApprovalMatrix? ApprovalMatrix { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    // Helper Properties
    public T? GetAmountThresholds<T>() where T : class
    {
        if (string.IsNullOrEmpty(AmountThresholds))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(AmountThresholds);
        }
        catch
        {
            return null;
        }
    }

    public void SetAmountThresholds<T>(T data)
    {
        if (data == null)
        {
            AmountThresholds = null;
        }
        else
        {
            AmountThresholds = JsonSerializer.Serialize(data);
        }
    }

    public T? GetRequiredFields<T>() where T : class
    {
        if (string.IsNullOrEmpty(RequiredFields))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(RequiredFields);
        }
        catch
        {
            return null;
        }
    }

    public void SetRequiredFields<T>(T data)
    {
        if (data == null)
        {
            RequiredFields = null;
        }
        else
        {
            RequiredFields = JsonSerializer.Serialize(data);
        }
    }
}
