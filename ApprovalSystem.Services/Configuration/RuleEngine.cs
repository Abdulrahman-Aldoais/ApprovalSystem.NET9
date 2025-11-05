using ApprovalSystem.Models.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApprovalSystem.Services.Configuration;

/// <summary>
/// محرك القوانين الديناميكي لتقييم الشروط والقوانين
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// تقييم قائمة من القوانين ضد البيانات المُرسلة
    /// </summary>
    Task<RuleEvaluationResultDto> EvaluateRulesAsync(List<EvaluationRuleDto> rules, Dictionary<string, object> data);

    /// <summary>
    /// تقييم شروط محددة
    /// </summary>
    Task<bool> EvaluateConditionsAsync(List<ConditionDto> conditions, Dictionary<string, object> data);

    /// <summary>
    /// التحقق من صحة قاعدة واحدة
    /// </summary>
    bool ValidateRule(EvaluationRuleDto rule);

    /// <summary>
    /// التحقق من صحة شرط واحد
    /// </summary>
    bool ValidateCondition(ConditionDto condition);

    /// <summary>
    /// الحصول على العمليات المدعومة
    /// </summary>
    List<string> GetSupportedOperators();

    /// <summary>
    /// الحصول على الحقول المدعومة للتقييم
    /// </summary>
    List<string> GetSupportedFields();
}

/// <summary>
/// تنفيذ محرك القوانين
/// </summary>
public class RuleEngine : IRuleEngine
{
    private readonly List<string> _supportedOperators = new()
    {
        "Equals", "NotEquals", "GreaterThan", "LessThan", 
        "GreaterThanOrEqual", "LessThanOrEqual", "Contains", 
        "NotContains", "StartsWith", "EndsWith", "IsNull", 
        "IsNotNull", "In", "NotIn", "Between", "Regex", 
        "IsEmpty", "IsNotEmpty", "HasValue"
    };

    private readonly List<string> _supportedFields = new()
    {
        "Amount", "Priority", "RequestType", "Department", 
        "SubmittedBy", "SubmittedDate", "Category", "Status",
        "UrgencyLevel", "BusinessUnit", "CostCenter", "Project",
        "Vendor", "Currency", "Country", "Region", "Tags"
    };

    private readonly Dictionary<string, string> _operatorActions = new()
    {
        ["HighPriorityApproval"] = "Require high-level approval",
        ["AutoApprove"] = "Automatically approve",
        ["RequireApproval"] = "Require manual approval",
        ["EscalateApproval"] = "Escalate to higher level",
        ["RejectRequest"] = "Automatically reject",
        ["RequestMoreInfo"] = "Request additional information",
        ["SetPriority"] = "Set specific priority level",
        ["AssignToSpecialist"] = "Assign to specialist team"
    };

    /// <summary>
    /// تقييم قائمة من القوانين
    /// </summary>
    public async Task<RuleEvaluationResultDto> EvaluateRulesAsync(List<EvaluationRuleDto> rules, Dictionary<string, object> data)
    {
        var result = new RuleEvaluationResultDto { IsValid = true };

        if (rules == null || !rules.Any())
        {
            return result;
        }

        var activeRules = rules.Where(r => r.IsActive).OrderBy(r => r.Priority).ToList();

        foreach (var rule in activeRules)
        {
            try
            {
                var ruleResult = await EvaluateRuleAsync(rule, data);
                
                if (ruleResult)
                {
                    result.MatchedRules.Add($"{rule.Field} {rule.Operator} {rule.Value} -> {rule.Action}");
                    result.ResultAction = rule.Action;
                    result.EvaluationData.Add($"Rule_{rule.Field}", new { 
                        Matched = true, 
                        Action = rule.Action,
                        Priority = rule.Priority,
                        Description = rule.Description
                    });

                    // إذا كان هناك إجراء عالي الأولوية، نتوقف
                    if (rule.Priority >= 3)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error evaluating rule {rule.Field}: {ex.Message}");
            }
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    /// <summary>
    /// تقييم شروط محددة
    /// </summary>
    public async Task<bool> EvaluateConditionsAsync(List<ConditionDto> conditions, Dictionary<string, object> data)
    {
        if (conditions == null || !conditions.Any())
        {
            return true;
        }

        var groups = conditions.GroupBy(c => c.GroupId);
        var groupResults = new List<bool>();

        foreach (var group in groups)
        {
            var groupConditions = group.ToList();
            var groupResult = await EvaluateConditionGroupAsync(groupConditions, data);
            groupResults.Add(groupResult);
        }

        // تجميع نتائج المجموعات باستخدام OR
        return groupResults.Any(r => r);
    }

    /// <summary>
    /// تقييم مجموعة شروط
    /// </summary>
    private async Task<bool> EvaluateConditionGroupAsync(List<ConditionDto> conditions, Dictionary<string, object> data)
    {
        if (!conditions.Any())
            return true;

        var results = new List<bool>();

        foreach (var condition in conditions)
        {
            var result = await EvaluateConditionAsync(condition, data);
            results.Add(result);
        }

        // تجميع النتائج حسب LogicalOperator
        var firstCondition = conditions.First();
        if (firstCondition.LogicalOperator.ToUpper() == "OR")
        {
            return results.Any(r => r);
        }
        else // AND
        {
            return results.All(r => r);
        }
    }

    /// <summary>
    /// تقييم قاعدة واحدة
    /// </summary>
    private async Task<bool> EvaluateRuleAsync(EvaluationRuleDto rule, Dictionary<string, object> data)
    {
        if (!data.ContainsKey(rule.Field))
        {
            return false;
        }

        var fieldValue = data[rule.Field];
        var ruleValue = rule.Value;

        return rule.Operator.ToLower() switch
        {
            "equals" => CompareValues(fieldValue, ruleValue, "equals"),
            "notequals" => !CompareValues(fieldValue, ruleValue, "equals"),
            "greaterthan" => CompareValues(fieldValue, ruleValue, "greaterthan"),
            "lessthan" => CompareValues(fieldValue, ruleValue, "lessthan"),
            "greaterthanorequal" => CompareValues(fieldValue, ruleValue, "greaterthanorequal"),
            "lessthanorequal" => CompareValues(fieldValue, ruleValue, "lessthanorequal"),
            "contains" => fieldValue?.ToString()?.Contains(ruleValue?.ToString() ?? "") ?? false,
            "notcontains" => !(fieldValue?.ToString()?.Contains(ruleValue?.ToString() ?? "") ?? false),
            "startswith" => fieldValue?.ToString()?.StartsWith(ruleValue?.ToString() ?? "") ?? false,
            "endswith" => fieldValue?.ToString()?.EndsWith(ruleValue?.ToString() ?? "") ?? false,
            "isnull" => fieldValue == null,
            "isnotnull" => fieldValue != null,
            "in" => IsValueInList(fieldValue, ruleValue),
            "notin" => !IsValueInList(fieldValue, ruleValue),
            "between" => IsBetween(fieldValue, ruleValue),
            "regex" => IsRegexMatch(fieldValue?.ToString(), ruleValue?.ToString()),
            "isempty" => string.IsNullOrEmpty(fieldValue?.ToString()),
            "isnotempty" => !string.IsNullOrEmpty(fieldValue?.ToString()),
            "hasvalue" => fieldValue != null && !string.IsNullOrEmpty(fieldValue.ToString()),
            _ => false
        };
    }

    /// <summary>
    /// تقييم شرط واحد
    /// </summary>
    private async Task<bool> EvaluateConditionAsync(ConditionDto condition, Dictionary<string, object> data)
    {
        return await EvaluateRuleAsync(new EvaluationRuleDto
        {
            Field = condition.Field,
            Operator = condition.Operator,
            Value = condition.Value,
            Action = "Condition"
        }, data);
    }

    /// <summary>
    /// مقارنة القيم
    /// </summary>
    private bool CompareValues(object? fieldValue, object? ruleValue, string operation)
    {
        if (fieldValue == null || ruleValue == null)
        {
            return operation == "equals" ? fieldValue == ruleValue : fieldValue != ruleValue;
        }

        // محاولة تحويل إلى أرقام
        if (decimal.TryParse(fieldValue.ToString(), out var fieldDecimal) && 
            decimal.TryParse(ruleValue.ToString(), out var ruleDecimal))
        {
            return operation switch
            {
                "equals" => fieldDecimal == ruleDecimal,
                "greaterthan" => fieldDecimal > ruleDecimal,
                "lessthan" => fieldDecimal < ruleDecimal,
                "greaterthanorequal" => fieldDecimal >= ruleDecimal,
                "lessthanorequal" => fieldDecimal <= ruleDecimal,
                _ => false
            };
        }

        // محاولة تحويل إلى تاريخ
        if (DateTime.TryParse(fieldValue.ToString(), out var fieldDate) && 
            DateTime.TryParse(ruleValue.ToString(), out var ruleDate))
        {
            return operation switch
            {
                "equals" => fieldDate.Date == ruleDate.Date,
                "greaterthan" => fieldDate > ruleDate,
                "lessthan" => fieldDate < ruleDate,
                "greaterthanorequal" => fieldDate >= ruleDate,
                "lessthanorequal" => fieldDate <= ruleDate,
                _ => false
            };
        }

        // مقارنة النصوص
        var fieldStr = fieldValue.ToString();
        var ruleStr = ruleValue.ToString();

        return operation switch
        {
            "equals" => string.Equals(fieldStr, ruleStr, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    /// <summary>
    /// التحقق من وجود القيمة في قائمة
    /// </summary>
    private bool IsValueInList(object? fieldValue, object? ruleValue)
    {
        if (fieldValue == null || ruleValue == null)
            return false;

        try
        {
            var list = JsonSerializer.Deserialize<List<object>>(ruleValue.ToString()!);
            return list?.Any(item => item.ToString() == fieldValue.ToString()) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// التحقق من وجود القيمة بين نطاق
    /// </summary>
    private bool IsBetween(object? fieldValue, object? ruleValue)
    {
        if (fieldValue == null || ruleValue == null)
            return false;

        try
        {
            var range = JsonSerializer.Deserialize<List<decimal>>(ruleValue.ToString()!);
            if (range?.Count != 2)
                return false;

            if (decimal.TryParse(fieldValue.ToString(), out var value))
            {
                return value >= range[0] && value <= range[1];
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// التحقق من مطابقة Regular Expression
    /// </summary>
    private bool IsRegexMatch(string? fieldValue, string? pattern)
    {
        if (string.IsNullOrEmpty(fieldValue) || string.IsNullOrEmpty(pattern))
            return false;

        try
        {
            return Regex.IsMatch(fieldValue, pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// التحقق من صحة قاعدة
    /// </summary>
    public bool ValidateRule(EvaluationRuleDto rule)
    {
        if (string.IsNullOrEmpty(rule.Field) || 
            string.IsNullOrEmpty(rule.Operator) || 
            string.IsNullOrEmpty(rule.Action))
        {
            return false;
        }

        if (!_supportedOperators.Contains(rule.Operator, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!_supportedFields.Contains(rule.Field, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!_operatorActions.ContainsKey(rule.Action))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// التحقق من صحة شرط
    /// </summary>
    public bool ValidateCondition(ConditionDto condition)
    {
        if (string.IsNullOrEmpty(condition.Field) || 
            string.IsNullOrEmpty(condition.Operator))
        {
            return false;
        }

        if (!_supportedOperators.Contains(condition.Operator, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!_supportedFields.Contains(condition.Field, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var validLogicalOperators = new[] { "AND", "OR" };
        if (!validLogicalOperators.Contains(condition.LogicalOperator.ToUpper()))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// الحصول على العمليات المدعومة
    /// </summary>
    public List<string> GetSupportedOperators()
    {
        return _supportedOperators.ToList();
    }

    /// <summary>
    /// الحصول على الحقول المدعومة
    /// </summary>
    public List<string> GetSupportedFields()
    {
        return _supportedFields.ToList();
    }
}