using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApprovalSystem.Models.Entities;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة المسارات العملية (Workflow)
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// إنشاء مسار عمل جديد
    /// </summary>
    Task<string?> CreateWorkflowAsync(string workflowName, Guid tenantId, Dictionary<string, object> config);

    /// <summary>
    /// تشغيل مسار عمل للطلب
    /// </summary>
    Task<string?> StartWorkflowAsync(Guid requestId, string workflowName, Guid tenantId);

    /// <summary>
    /// إيقاف مسار عمل
    /// </summary>
    Task<bool> StopWorkflowAsync(string workflowInstanceId);

    /// <summary>
    /// تحديث حالة مسار العمل
    /// </summary>
    Task<bool> UpdateWorkflowStatusAsync(string workflowInstanceId, string status, Dictionary<string, object>? data = null);

    /// <summary>
    /// الحصول على تفاصيل مسار عمل
    /// </summary>
    Task<WorkflowInstance?> GetWorkflowAsync(string workflowInstanceId);

    /// <summary>
    /// الحصول على تتبع مسار العمل
    /// </summary>
    Task<List<WorkflowStep>> GetWorkflowStepsAsync(string workflowInstanceId);

    /// <summary>
    /// تنفيذ خطوة في مسار العمل
    /// </summary>
    Task<bool> ExecuteWorkflowStepAsync(string workflowInstanceId, string stepName, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// الحصول على المسارات المتاحة
    /// </summary>
    Task<List<string>> GetAvailableWorkflowsAsync(Guid tenantId);

    /// <summary>
    /// التحقق من حالة مسار العمل
    /// </summary>
    Task<string?> GetWorkflowStatusAsync(string workflowInstanceId);

    /// <summary>
    /// إعادة تشغيل مسار العمل
    /// </summary>
    Task<bool> RestartWorkflowAsync(string workflowInstanceId);

    /// <summary>
    /// إرسال بيانات لمسار العمل
    /// </summary>
    Task<bool> SendWorkflowDataAsync(string workflowInstanceId, string dataType, object data);

    /// <summary>
    /// الحصول على إحصائيات المسارات
    /// </summary>
    Task<WorkflowStats> GetWorkflowStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// تنظيف المسارات المنتهية
    /// </summary>
    Task<int> CleanupCompletedWorkflowsAsync(Guid tenantId, int daysToKeep = 30);

    /// <summary>
    /// الحصول على المسارات المتأخرة
    /// </summary>
    Task<List<WorkflowInstance>> GetDelayedWorkflowsAsync(Guid tenantId, int maxDelayHours = 24);
}

/// <summary>
/// نموذج مسار العمل
/// </summary>
public class WorkflowInstance
{
    public string InstanceId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string Status { get; set; } = "started"; // started, running, completed, failed, cancelled, suspended
    public Guid RequestId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
}

/// <summary>
/// نموذج خطوة مسار العمل
/// </summary>
public class WorkflowStep
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty; // task, decision, parallel, merge, etc.
    public string Status { get; set; } = "pending"; // pending, running, completed, failed, skipped
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? NextStepId { get; set; }
}

/// <summary>
/// نموذج إحصائيات المسارات
/// </summary>
public class WorkflowStats
{
    public int TotalWorkflows { get; set; }
    public int ActiveWorkflows { get; set; }
    public int CompletedWorkflows { get; set; }
    public int FailedWorkflows { get; set; }
    public double AverageExecutionTime { get; set; } // in hours
    public int FailedWorkflowsToday { get; set; }
    public int WorkflowsByType { get; set; } // count for approval workflows specifically
    public Dictionary<string, int> WorkflowsByStatus { get; set; } = new();

    // Computed properties
    public double SuccessRate => TotalWorkflows > 0 ? (double)CompletedWorkflows / TotalWorkflows * 100 : 0;
    public double FailureRate => TotalWorkflows > 0 ? (double)FailedWorkflows / TotalWorkflows * 100 : 0;
}

/// <summary>
/// نموذج مهمة مسارات العمل
/// </summary>
public class WorkflowTask
{
    public string TaskId { get; set; } = string.Empty;
    public string WorkflowInstanceId { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// نموذج حدث مسارات العمل
/// </summary>
public class WorkflowEvent
{
    public string EventId { get; set; } = string.Empty;
    public string WorkflowInstanceId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // started, step_completed, completed, failed, etc.
    public string EventData { get; set; } = string.Empty; // JSON
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Source { get; set; } // system, user, timer, etc.
}
