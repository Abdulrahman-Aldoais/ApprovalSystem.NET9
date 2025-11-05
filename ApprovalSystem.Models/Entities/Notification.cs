using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان الإشعار
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    // JSON data for additional notification metadata
    public string? Data { get; set; } // Serialized JSON

    public bool IsRead { get; set; } = false;

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "medium"; // low, medium, high, urgent

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "info"; // info, warning, success, error

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    [Required]
    [ForeignKey("User")]
    public string UserId { get; set; } = string.Empty;

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User User { get; set; } = null!;

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

    // Helper Methods
    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    public bool IsOverdue => Priority == "urgent" && CreatedAt.AddHours(2) < DateTime.UtcNow;

    public TimeSpan Age => DateTime.UtcNow.Subtract(CreatedAt);
}
