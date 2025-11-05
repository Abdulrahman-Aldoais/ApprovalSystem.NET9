using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// نموذج إعدادات الـ Workflow الديناميكية
/// يدعم تكوين الشروط والقوانين بشكل ديناميكي لكل tenant
/// </summary>
[Table("WorkflowConfigurations")]
public class WorkflowConfiguration
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// معرف المستأجر (Multi-tenancy)
    /// </summary>
    [Required]
    public int TenantId { get; set; }

    /// <summary>
    /// اسم الـ Workflow
    /// </summary>
    [Required]
    [StringLength(100)]
    public string WorkflowName { get; set; } = string.Empty;

    /// <summary>
    /// وصف الـ Workflow
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// نوع الطلب المرتبط
    /// </summary>
    [Required]
    public Guid RequestTypeId { get; set; }

    /// <summary>
    /// تعريف الـ Workflow بصيغة JSON
    /// يتضمن الأنشطة والانتقالات والشروط
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string WorkflowDefinition { get; set; } = "{}";

    /// <summary>
    /// قواعد التقييم التلقائي للطلبات
    /// مثل: [{"field": "Amount", "operator": "GreaterThan", "value": 1000, "action": "RequireApproval"}]
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string EvaluationRules { get; set; } = "[]";

    /// <summary>
    /// إعدادات التصعيد
    /// مثل: {"enableEscalation": true, "escalationTimeHours": 24, "escalationLevels": [...]}
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string EscalationSettings { get; set; } = "{}";

    /// <summary>
    /// إعدادات الإشعارات لهذا الـ Workflow
    /// مثل: {"emailNotifications": true, "smsNotifications": false, "templates": {...}}
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string NotificationSettings { get; set; } = "{}";

    /// <summary>
    /// شروط بدء الـ Workflow
    /// مثل: [{"field": "Priority", "operator": "Equals", "value": "High"}]
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string StartConditions { get; set; } = "[]";

    /// <summary>
    /// شروط إكمال الـ Workflow
    /// مثل: [{"requiredApprovals": 2, "allLevelsRequired": true}]
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string CompletionConditions { get; set; } = "[]";

    /// <summary>
    /// البيانات الافتراضية للـ Workflow
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string DefaultData { get; set; } = "{}";

    /// <summary>
    /// أولوية الـ Workflow
    /// 1 = منخفضة، 2 = متوسطة، 3 = عالية، 4 = حرجة
    /// </summary>
    [Range(1, 4)]
    public int Priority { get; set; } = 2;

    /// <summary>
    /// هل الـ Workflow نشط؟
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// هل يتطلب موافقة يدوية دائماً؟
    /// </summary>
    public bool RequiresManualApproval { get; set; } = true;

    /// <summary>
    /// هل يدعم الموافقة المتوازية؟
    /// </summary>
    public bool SupportsParallelApproval { get; set; } = false;

    /// <summary>
    /// الحد الأقصى لوقت تنفيذ الـ Workflow (بالساعات)
    /// </summary>
    public int? MaxExecutionTimeHours { get; set; }

    /// <summary>
    /// عدد مرات إعادة المحاولة المسموحة
    /// </summary>
    [Range(0, 10)]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// الحالة الحالية للإعداد
    /// Draft, Published, Archived
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// رقم الإصدار
    /// </summary>
    [Required]
    [StringLength(10)]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// تاريخ الإنشاء
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تحديث
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// المستخدم الذي أنشأ الإعداد
    /// </summary>
    [Required]
    [StringLength(450)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// المستخدم الذي حدث الإعداد
    /// </summary>
    [StringLength(450)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// هل الإعداد محذوف؟ (Soft Delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// تاريخ الحذف
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// المستخدم الذي حذف الإعداد
    /// </summary>
    [StringLength(450)]
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual RequestType RequestType { get; set; } = null!;

    // Helper Methods لتحويل JSON

    /// <summary>
    /// تحويل WorkflowDefinition من JSON إلى كائن
    /// </summary>
    public T? GetWorkflowDefinition<T>() where T : class
    {
        if (string.IsNullOrEmpty(WorkflowDefinition) || WorkflowDefinition == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(WorkflowDefinition);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// تحديث WorkflowDefinition بكائن جديد
    /// </summary>
    public void SetWorkflowDefinition<T>(T definition) where T : class
    {
        WorkflowDefinition = JsonSerializer.Serialize(definition);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// تحويل EvaluationRules من JSON إلى قائمة
    /// </summary>
    public List<T>? GetEvaluationRules<T>() where T : class
    {
        if (string.IsNullOrEmpty(EvaluationRules) || EvaluationRules == "[]")
            return new List<T>();

        try
        {
            return JsonSerializer.Deserialize<List<T>>(EvaluationRules);
        }
        catch
        {
            return new List<T>();
        }
    }

    /// <summary>
    /// تحديث EvaluationRules بقائمة جديدة
    /// </summary>
    public void SetEvaluationRules<T>(List<T> rules) where T : class
    {
        EvaluationRules = JsonSerializer.Serialize(rules);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// تحويل إعدادات التصعيد من JSON
    /// </summary>
    public T? GetEscalationSettings<T>() where T : class
    {
        if (string.IsNullOrEmpty(EscalationSettings) || EscalationSettings == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(EscalationSettings);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// تحديث إعدادات التصعيد
    /// </summary>
    public void SetEscalationSettings<T>(T settings) where T : class
    {
        EscalationSettings = JsonSerializer.Serialize(settings);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// التحقق من صحة الإعدادات
    /// </summary>
    public List<string> ValidateConfiguration()
    {
        var errors = new List<string>();

        // التحقق من JSON الأساسي
        if (!IsValidJson(WorkflowDefinition))
            errors.Add("WorkflowDefinition contains invalid JSON");

        if (!IsValidJson(EvaluationRules))
            errors.Add("EvaluationRules contains invalid JSON");

        if (!IsValidJson(EscalationSettings))
            errors.Add("EscalationSettings contains invalid JSON");

        if (!IsValidJson(NotificationSettings))
            errors.Add("NotificationSettings contains invalid JSON");

        // التحقق من الحقول المطلوبة
        if (string.IsNullOrEmpty(WorkflowName))
            errors.Add("WorkflowName is required");

        if (TenantId <= 0)
            errors.Add("Valid TenantId is required");

        if (RequestTypeId <= 0)
            errors.Add("Valid RequestTypeId is required");

        // التحقق من حالة الإعداد
        var validStatuses = new[] { "Draft", "Published", "Archived" };
        if (!validStatuses.Contains(Status))
            errors.Add("Status must be Draft, Published, or Archived");

        return errors;
    }

    /// <summary>
    /// التحقق من صحة JSON
    /// </summary>
    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return true;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// إنشاء نسخة من الإعداد
    /// </summary>
    public WorkflowConfiguration Clone()
    {
        return new WorkflowConfiguration
        {
            TenantId = TenantId,
            WorkflowName = $"{WorkflowName} - Copy",
            Description = Description,
            RequestTypeId = RequestTypeId,
            WorkflowDefinition = WorkflowDefinition,
            EvaluationRules = EvaluationRules,
            EscalationSettings = EscalationSettings,
            NotificationSettings = NotificationSettings,
            StartConditions = StartConditions,
            CompletionConditions = CompletionConditions,
            DefaultData = DefaultData,
            Priority = Priority,
            IsActive = false,
            RequiresManualApproval = RequiresManualApproval,
            SupportsParallelApproval = SupportsParallelApproval,
            MaxExecutionTimeHours = MaxExecutionTimeHours,
            MaxRetryCount = MaxRetryCount,
            Status = "Draft",
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = CreatedBy
        };
    }
}