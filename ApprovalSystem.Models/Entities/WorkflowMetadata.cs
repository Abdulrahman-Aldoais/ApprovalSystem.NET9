using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان معلومات الـ Workflow metadata
/// يحتوي على معلومات إضافية للـ workflows
/// </summary>
public class WorkflowMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string WorkflowType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = "1.0";

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Tags { get; set; } // JSON array of tags

    [MaxLength(50)]
    public string Priority { get; set; } = "normal"; // low, normal, high, urgent

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    [MaxLength(100)]
    public string? Icon { get; set; }

    [MaxLength(100)]
    public string? Color { get; set; }

    public int MaxRetries { get; set; } = 3;

    public int TimeoutMinutes { get; set; } = 60;

    public bool RequiresManualApproval { get; set; } = false;

    public bool SendsNotifications { get; set; } = true;

    [MaxLength(100)]
    public string? NotificationTemplate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "active"; // active, inactive, draft, deprecated

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    // Foreign Keys
    [Required]
    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;

    // Additional properties for Elsa integration
    [MaxLength(100)]
    public string? WorkflowDefinitionId { get; set; }

    [MaxLength(100)]
    public string? WorkflowInstanceId { get; set; }

    [MaxLength(50)]
    public string? ExecutionId { get; set; }

    public string? ConfigurationJson { get; set; } // JSON configuration

    public string? VariablesJson { get; set; } // JSON variables

    // Helper methods
    public T? GetConfiguration<T>() where T : class
    {
        if (string.IsNullOrEmpty(ConfigurationJson))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(ConfigurationJson);
        }
        catch
        {
            return null;
        }
    }

    public void SetConfiguration<T>(T configuration)
    {
        if (configuration == null)
        {
            ConfigurationJson = null;
        }
        else
        {
            ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(configuration);
        }
    }

    public T? GetVariables<T>() where T : class
    {
        if (string.IsNullOrEmpty(VariablesJson))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(VariablesJson);
        }
        catch
        {
            return null;
        }
    }

    public void SetVariables<T>(T variables)
    {
        if (variables == null)
        {
            VariablesJson = null;
        }
        else
        {
            VariablesJson = System.Text.Json.JsonSerializer.Serialize(variables);
        }
    }
}
