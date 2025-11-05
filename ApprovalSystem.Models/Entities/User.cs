using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان المستخدم مع تكامل ASP.NET Core Identity
/// </summary>
public class User : IdentityUser
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "User";

    [MaxLength(100)]
    public string? Department { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Request> SubmittedRequests { get; set; } = new List<Request>();
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<RequestAudit> RequestAudits { get; set; } = new List<RequestAudit>();

    // User Preferences
    [MaxLength(20)]
    public string? PreferredLanguage { get; set; } = "ar";

    [MaxLength(50)]
    public string? TimeZone { get; set; } = "Arabia Standard Time";

    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; } = false;
}
