using ApprovalSystem.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ApprovalSystem.Models.Validation;

/// <summary>
/// التحقق من صحة JSON للحقول المخصصة
/// </summary>
public class ValidJsonAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return true; // null أو empty مسموح
        }

        try
        {
            JsonDocument.Parse(value.ToString()!);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must contain valid JSON format.";
    }
}

/// <summary>
/// التحقق من صحة قوانين التقييم
/// </summary>
public class ValidEvaluationRulesAttribute : ValidationAttribute
{
    private readonly List<string> _supportedOperators = new()
    {
        "Equals", "NotEquals", "GreaterThan", "LessThan", 
        "GreaterThanOrEqual", "LessThanOrEqual", "Contains", 
        "NotContains", "StartsWith", "EndsWith", "IsNull", 
        "IsNotNull", "In", "NotIn", "Between", "Regex", 
        "IsEmpty", "IsNotEmpty", "HasValue"
    };

    private readonly List<string> _supportedActions = new()
    {
        "HighPriorityApproval", "AutoApprove", "RequireApproval", 
        "EscalateApproval", "RejectRequest", "RequestMoreInfo", 
        "SetPriority", "AssignToSpecialist"
    };

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // null مسموح
        }

        if (value is not List<EvaluationRuleDto> rules)
        {
            return false;
        }

        foreach (var rule in rules)
        {
            // التحقق من الحقول المطلوبة
            if (string.IsNullOrEmpty(rule.Field) || 
                string.IsNullOrEmpty(rule.Operator) || 
                string.IsNullOrEmpty(rule.Action))
            {
                return false;
            }

            // التحقق من صحة العملية
            if (!_supportedOperators.Contains(rule.Operator, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // التحقق من صحة الإجراء
            if (!_supportedActions.Contains(rule.Action))
            {
                return false;
            }

            // التحقق من أولوية القاعدة
            if (rule.Priority < 1 || rule.Priority > 5)
            {
                return false;
            }
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} contains invalid evaluation rules. Please check operators, actions, and priorities.";
    }
}

/// <summary>
/// التحقق من صحة الشروط
/// </summary>
public class ValidConditionsAttribute : ValidationAttribute
{
    private readonly List<string> _supportedOperators = new()
    {
        "Equals", "NotEquals", "GreaterThan", "LessThan", 
        "GreaterThanOrEqual", "LessThanOrEqual", "Contains", 
        "NotContains", "StartsWith", "EndsWith", "IsNull", 
        "IsNotNull", "In", "NotIn", "Between", "Regex", 
        "IsEmpty", "IsNotEmpty", "HasValue"
    };

    private readonly List<string> _supportedLogicalOperators = new() { "AND", "OR" };

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // null مسموح
        }

        if (value is not List<ConditionDto> conditions)
        {
            return false;
        }

        foreach (var condition in conditions)
        {
            // التحقق من الحقول المطلوبة
            if (string.IsNullOrEmpty(condition.Field) || 
                string.IsNullOrEmpty(condition.Operator))
            {
                return false;
            }

            // التحقق من صحة العملية
            if (!_supportedOperators.Contains(condition.Operator, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // التحقق من صحة العامل المنطقي
            if (!_supportedLogicalOperators.Contains(condition.LogicalOperator.ToUpper()))
            {
                return false;
            }

            // التحقق من GroupId
            if (condition.GroupId < 0)
            {
                return false;
            }
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} contains invalid conditions. Please check operators and logical operators.";
    }
}

/// <summary>
/// التحقق من صحة إعدادات التصعيد
/// </summary>
public class ValidEscalationSettingsAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // null مسموح
        }

        if (value is not EscalationSettingsDto settings)
        {
            return false;
        }

        // التحقق من وقت التصعيد
        if (settings.EnableEscalation && settings.EscalationTimeHours <= 0)
        {
            return false;
        }

        // التحقق من مستويات التصعيد
        if (settings.EscalationLevels != null)
        {
            foreach (var level in settings.EscalationLevels)
            {
                if (level.Level <= 0 || level.TimeoutHours <= 0)
                {
                    return false;
                }

                // يجب أن يكون هناك مستخدمين أو أدوار للتصعيد
                if (!level.EscalationUsers.Any() && !level.EscalationRoles.Any())
                {
                    return false;
                }
            }

            // التحقق من ترتيب المستويات
            var sortedLevels = settings.EscalationLevels.OrderBy(l => l.Level).ToList();
            for (int i = 0; i < sortedLevels.Count; i++)
            {
                if (sortedLevels[i].Level != i + 1)
                {
                    return false; // المستويات يجب أن تكون متتالية
                }
            }
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} contains invalid escalation settings. Please check escalation time and levels.";
    }
}

/// <summary>
/// التحقق من صحة إعدادات الإشعارات
/// </summary>
public class ValidNotificationSettingsAttribute : ValidationAttribute
{
    private readonly List<string> _supportedEvents = new()
    {
        "RequestSubmitted", "RequestApproved", "RequestRejected", 
        "RequestEscalated", "RequestCompleted", "RequestReturned",
        "ApprovalRequired", "DeadlineApproaching", "DeadlinePassed"
    };

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // null مسموح
        }

        if (value is not NotificationSettingsDto settings)
        {
            return false;
        }

        // التحقق من أن هناك على الأقل نوع واحد من الإشعارات مفعل
        if (!settings.EmailNotifications && 
            !settings.SmsNotifications && 
            !settings.InAppNotifications && 
            !settings.RealTimeNotifications)
        {
            return false;
        }

        // التحقق من صحة الأحداث
        if (settings.NotificationEvents != null)
        {
            foreach (var eventName in settings.NotificationEvents)
            {
                if (!_supportedEvents.Contains(eventName))
                {
                    return false;
                }
            }
        }

        // التحقق من إعدادات الجدولة
        if (settings.Schedule != null && settings.Schedule.EnableScheduling)
        {
            if (settings.Schedule.SendHours != null)
            {
                foreach (var hour in settings.Schedule.SendHours)
                {
                    if (hour < 0 || hour > 23)
                    {
                        return false;
                    }
                }
            }

            if (settings.Schedule.SendDays != null)
            {
                foreach (var day in settings.Schedule.SendDays)
                {
                    if (day < 1 || day > 7)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} contains invalid notification settings. Please check events and scheduling.";
    }
}

/// <summary>
/// التحقق من صحة حالة الـ Workflow
/// </summary>
public class ValidWorkflowStatusAttribute : ValidationAttribute
{
    private readonly List<string> _validStatuses = new() { "Draft", "Published", "Archived" };

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return true; // null أو empty مسموح
        }

        return _validStatuses.Contains(value.ToString()!, StringComparer.OrdinalIgnoreCase);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be one of: Draft, Published, or Archived.";
    }
}

/// <summary>
/// التحقق من صحة رقم الإصدار
/// </summary>
public class ValidVersionAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return true; // null أو empty مسموح
        }

        var version = value.ToString()!;
        
        // التحقق من تنسيق الإصدار (مثل: 1.0, 2.1, 10.5)
        var parts = version.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be in format 'Major.Minor' (e.g., '1.0', '2.1').";
    }
}

/// <summary>
/// التحقق من صحة أولوية الـ Workflow
/// </summary>
public class ValidWorkflowPriorityAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // null مسموح
        }

        if (value is int priority)
        {
            return priority >= 1 && priority <= 4;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between 1 (Low) and 4 (Critical).";
    }
}

/// <summary>
/// التحقق من صحة الحد الأقصى لوقت التنفيذ
/// </summary>
public class ValidExecutionTimeAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // null مسموح
        }

        if (value is int hours)
        {
            return hours > 0 && hours <= 8760; // سنة واحدة كحد أقصى
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between 1 and 8760 hours (1 year).";
    }
}

/// <summary>
/// التحقق من صحة عدد مرات إعادة المحاولة
/// </summary>
public class ValidRetryCountAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true; // null مسموح
        }

        if (value is int retryCount)
        {
            return retryCount >= 0 && retryCount <= 10;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be between 0 and 10.";
    }
}