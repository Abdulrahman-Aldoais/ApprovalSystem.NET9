using ApprovalSystem.Models.Validation;
using System.ComponentModel.DataAnnotations;

namespace ApprovalSystem.Models.DTOs;

/// <summary>
/// DTO لإنشاء إعداد Workflow جديد
/// </summary>
public class CreateWorkflowConfigurationDto
{
    [Required]
    [StringLength(100)]
    public string WorkflowName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid RequestTypeId { get; set; }

    public object? WorkflowDefinition { get; set; }

    [ValidEvaluationRules]
    public List<EvaluationRuleDto> EvaluationRules { get; set; } = new();

    [ValidEscalationSettings]
    public EscalationSettingsDto? EscalationSettings { get; set; }

    [ValidNotificationSettings]
    public NotificationSettingsDto? NotificationSettings { get; set; }

    [ValidConditions]
    public List<ConditionDto> StartConditions { get; set; } = new();

    [ValidConditions]
    public List<ConditionDto> CompletionConditions { get; set; } = new();

    public object? DefaultData { get; set; }

    [ValidWorkflowPriority]
    [Range(1, 4)]
    public int Priority { get; set; } = 2;

    public bool IsActive { get; set; } = true;

    public bool RequiresManualApproval { get; set; } = true;

    public bool SupportsParallelApproval { get; set; } = false;

    [ValidExecutionTime]
    public int? MaxExecutionTimeHours { get; set; }

    [ValidRetryCount]
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;
}

/// <summary>
/// DTO لتحديث إعداد Workflow
/// </summary>
public class UpdateWorkflowConfigurationDto
{
    [StringLength(100)]
    public string? WorkflowName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public object? WorkflowDefinition { get; set; }

    [ValidEvaluationRules]
    public List<EvaluationRuleDto>? EvaluationRules { get; set; }

    [ValidEscalationSettings]
    public EscalationSettingsDto? EscalationSettings { get; set; }

    [ValidNotificationSettings]
    public NotificationSettingsDto? NotificationSettings { get; set; }

    [ValidConditions]
    public List<ConditionDto>? StartConditions { get; set; }

    [ValidConditions]
    public List<ConditionDto>? CompletionConditions { get; set; }

    public object? DefaultData { get; set; }

    [ValidWorkflowPriority]
    [Range(1, 4)]
    public int? Priority { get; set; }

    public bool? IsActive { get; set; }

    public bool? RequiresManualApproval { get; set; }

    public bool? SupportsParallelApproval { get; set; }

    [ValidExecutionTime]
    public int? MaxExecutionTimeHours { get; set; }

    [ValidRetryCount]
    [Range(0, 10)]
    public int? MaxRetryCount { get; set; }

    [ValidWorkflowStatus]
    [StringLength(20)]
    public string? Status { get; set; }
}

/// <summary>
/// DTO لعرض إعداد Workflow
/// </summary>
public class WorkflowConfigurationDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid RequestTypeId { get; set; }
    public string RequestTypeName { get; set; } = string.Empty;
    public object? WorkflowDefinition { get; set; }
    public List<EvaluationRuleDto> EvaluationRules { get; set; } = new();
    public EscalationSettingsDto? EscalationSettings { get; set; }
    public NotificationSettingsDto? NotificationSettings { get; set; }
    public List<ConditionDto> StartConditions { get; set; } = new();
    public List<ConditionDto> CompletionConditions { get; set; } = new();
    public object? DefaultData { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresManualApproval { get; set; }
    public bool SupportsParallelApproval { get; set; }
    public int? MaxExecutionTimeHours { get; set; }
    public int MaxRetryCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// DTO لقاعدة التقييم
/// </summary>
public class EvaluationRuleDto
{
    [Required]
    [StringLength(50)]
    public string Field { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Operator { get; set; } = string.Empty;

    [Required]
    public object Value { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public int Priority { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// DTO لإعدادات التصعيد
/// </summary>
public class EscalationSettingsDto
{
    public bool EnableEscalation { get; set; } = false;

    public int EscalationTimeHours { get; set; } = 24;

    public List<EscalationLevelDto> EscalationLevels { get; set; } = new();

    public bool NotifyOnEscalation { get; set; } = true;

    public string? EscalationMessage { get; set; }
}

/// <summary>
/// DTO لمستوى التصعيد
/// </summary>
public class EscalationLevelDto
{
    public int Level { get; set; }

    public int TimeoutHours { get; set; }

    public List<string> EscalationUsers { get; set; } = new();

    public List<string> EscalationRoles { get; set; } = new();

    public string? Action { get; set; }
}

/// <summary>
/// DTO لإعدادات الإشعارات
/// </summary>
public class NotificationSettingsDto
{
    public bool EmailNotifications { get; set; } = true;

    public bool SmsNotifications { get; set; } = false;

    public bool InAppNotifications { get; set; } = true;

    public bool RealTimeNotifications { get; set; } = true;

    public NotificationTemplatesDto Templates { get; set; } = new();

    public List<string> NotificationEvents { get; set; } = new();

    public NotificationScheduleDto? Schedule { get; set; }
}

/// <summary>
/// DTO لقوالب الإشعارات
/// </summary>
public class NotificationTemplatesDto
{
    public string? RequestSubmitted { get; set; }
    public string? RequestApproved { get; set; }
    public string? RequestRejected { get; set; }
    public string? RequestEscalated { get; set; }
    public string? RequestCompleted { get; set; }
}

/// <summary>
/// DTO لجدولة الإشعارات
/// </summary>
public class NotificationScheduleDto
{
    public bool EnableScheduling { get; set; } = false;
    public List<int> SendHours { get; set; } = new();
    public List<int> SendDays { get; set; } = new();
    public string? TimeZone { get; set; }
}

/// <summary>
/// DTO للشروط
/// </summary>
public class ConditionDto
{
    [Required]
    [StringLength(50)]
    public string Field { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Operator { get; set; } = string.Empty;

    [Required]
    public object Value { get; set; } = string.Empty;

    [StringLength(10)]
    public string LogicalOperator { get; set; } = "AND";

    public int GroupId { get; set; } = 0;
}

/// <summary>
/// DTO لنتيجة تقييم القاعدة - تم نقله إلى RuleEngineDTOs.cs لتجنب التكرار
/// استخدام ApprovalSystem.Models.DTOs.RuleEvaluationResultDto بدلاً من هذا التعريف
/// </summary>

/// <summary>
/// DTO لطلب تقييم الشروط
/// </summary>
public class EvaluateConditionsDto
{
    [Required]
    public int WorkflowConfigurationId { get; set; }

    [Required]
    public Dictionary<string, object> RequestData { get; set; } = new();

    public string? EvaluationType { get; set; } = "StartConditions";
}

/// <summary>
/// DTO لنتيجة التحقق من صحة الإعدادات
/// </summary>
public class ValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
}

/// <summary>
/// DTO لإحصائيات Workflow
/// </summary>
public class WorkflowStatisticsDto
{
    public int TotalConfigurations { get; set; }
    public int ActiveConfigurations { get; set; }
    public int DraftConfigurations { get; set; }
    public int ArchivedConfigurations { get; set; }
    public Dictionary<string, int> ConfigurationsByRequestType { get; set; } = new();
    public Dictionary<string, int> ConfigurationsByPriority { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}