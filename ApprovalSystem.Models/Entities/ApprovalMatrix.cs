using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان مصفوفة الموافقات
/// </summary>
public class ApprovalMatrix
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // JSON configuration for approval rules
    public string? Rules { get; set; } // Serialized JSON

    // JSON configuration for conditions
    public string? Conditions { get; set; } // Serialized JSON

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();

    // Helper Properties
    public T? GetRules<T>() where T : class
    {
        if (string.IsNullOrEmpty(Rules))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(Rules);
        }
        catch
        {
            return null;
        }
    }

    public void SetRules<T>(T data)
    {
        if (data == null)
        {
            Rules = null;
        }
        else
        {
            Rules = JsonSerializer.Serialize(data);
        }
    }

    public T? GetConditions<T>() where T : class
    {
        if (string.IsNullOrEmpty(Conditions))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(Conditions);
        }
        catch
        {
            return null;
        }
    }

    public void SetConditions<T>(T data)
    {
        if (data == null)
        {
            Conditions = null;
        }
        else
        {
            Conditions = JsonSerializer.Serialize(data);
        }
    }
}
