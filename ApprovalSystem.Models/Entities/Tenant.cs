using System.ComponentModel.DataAnnotations;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان المؤسسة المتعددة (Multi-tenant)
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Identifier { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    public string? PrimaryColor { get; set; }

    [MaxLength(20)]
    public string? SecondaryColor { get; set; }

    [MaxLength(255)]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
    public virtual ICollection<Request>? Requests { get; set; } = new List<Request>();
    public virtual ICollection<ApprovalMatrix> ApprovalMatrices { get; set; } = new List<ApprovalMatrix>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<WorkflowConfiguration> WorkflowConfigurations { get; set; } = new List<WorkflowConfiguration>();

    // Analytics and Audit Trail
    public virtual ICollection<RequestAudit> RequestAudits { get; set; } = new List<RequestAudit>();
    public virtual ICollection<WorkflowTracking> WorkflowTrackings { get; set; } = new List<WorkflowTracking>();
    public virtual ICollection<ApprovalEscalation> ApprovalEscalations { get; set; } = new List<ApprovalEscalation>();
}
