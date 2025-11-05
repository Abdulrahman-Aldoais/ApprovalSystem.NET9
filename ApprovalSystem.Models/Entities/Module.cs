using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان الوحدة/القسم (Module)
/// </summary>
public class Module
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();

    // Color for UI
    [MaxLength(20)]
    public string? Color { get; set; }

    // Sort Order
    public int SortOrder { get; set; } = 0;

    // Optional manager assignment
    [MaxLength(100)]
    public string? Manager { get; set; }
}
