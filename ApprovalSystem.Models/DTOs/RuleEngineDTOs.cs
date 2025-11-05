using System.ComponentModel.DataAnnotations;

namespace ApprovalSystem.Models.DTOs;

/// <summary>
/// DTO لقاعدة تقييم الموافقة
/// </summary>
public class ApprovalRuleDto
{
    [Required]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    public string Operator { get; set; } = string.Empty;

    [Required]
    public object Value { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public int Priority { get; set; } = 1;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// DTO لنتيجة تقييم القواعد
/// </summary>
public class RuleEvaluationResultDto
{
    public bool IsValid { get; set; }
    public string? ResultAction { get; set; }
    public List<string> MatchedRules { get; set; } = new();
    public Dictionary<string, object> EvaluationData { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public object? Data { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO لطلب تقييم القواعد
/// </summary>
public class RuleEvaluationRequestDto
{
    [Required]
    public List<ApprovalRuleDto> Rules { get; set; } = new();

    [Required]
    public Dictionary<string, object> RequestData { get; set; } = new();

    public string? EvaluationType { get; set; }

    public object? Context { get; set; }
}

/// <summary>
/// DTO لمحرك القواعد
/// </summary>
public class RuleEngineDto
{
    public List<ApprovalRuleDto> Rules { get; set; } = new();
    public Dictionary<string, object> GlobalVariables { get; set; } = new();
    public string? DefaultAction { get; set; }
    public bool StopOnFirstMatch { get; set; } = false;
    public int MaxIterations { get; set; } = 100;
    public string LogLevel { get; set; } = "Information";
}

/// <summary>
/// DTO لنتائج محرك القواعد
/// </summary>
public class RuleEngineResultDto
{
    public bool IsSuccessful { get; set; }
    public object? Result { get; set; }
    public string? SelectedAction { get; set; }
    public List<string> MatchedRules { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public int Iterations { get; set; }
}

/// <summary>
/// DTO للحالة في Workflow
/// </summary>
public class WorkflowStateDto
{
    public string? CurrentState { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new();
    public List<string> CompletedActivities { get; set; } = new();
    public string? CurrentActivity { get; set; }
    public DateTime? LastActivityTime { get; set; }
    public int ExecutionCount { get; set; }
    public string? SuspendReason { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFaulted { get; set; }
    public string? FaultReason { get; set; }
}

/// <summary>
/// DTO لمتغيرات Workflow
/// </summary>
public class WorkflowVariableDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public object? Value { get; set; }

    [Required]
    public string Type { get; set; } = "object";

    public string? Description { get; set; }

    public bool IsInput { get; set; } = false;

    public bool IsOutput { get; set; } = false;

    public bool IsRequired { get; set; } = false;

    public object? DefaultValue { get; set; }

    public string? DataType { get; set; }
}

/// <summary>
/// DTO لإعدادات التكوين
/// </summary>
public class ConfigurationServiceDto
{
    public string? ConnectionString { get; set; }
    public string? DatabaseProvider { get; set; }
    public bool EnableCaching { get; set; } = true;
    public int CacheTimeoutMinutes { get; set; } = 30;
    public bool EnableLogging { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}
