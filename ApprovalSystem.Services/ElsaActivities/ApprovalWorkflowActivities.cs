using ApprovalSystem.Models.DTOs;
using ApprovalSystem.Services.Configuration;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using System.Text.Json;

namespace ApprovalSystem.Services.ElsaActivities;

/// <summary>
/// نشاط بدء تشغيل Workflow الموافقة (Elsa 3.x)
/// يقوم بتقييم الشروط وتحديد إعدادات الـ Workflow المناسبة
/// </summary>
[Activity(
    Category = "Approval System",
    DisplayName = "بدء تشغيل Workflow الموافقة",
    Description = "يبدأ تشغيل workflow الموافقة ويقيم الشروط المطلوبة"
)]
public class StartApprovalWorkflowActivity : Activity
{
    private readonly IConfigurationService _configurationService;
    private readonly IRuleEngine _ruleEngine;

    public StartApprovalWorkflowActivity(
        IConfigurationService configurationService,
        IRuleEngine ruleEngine)
    {
        _configurationService = configurationService;
        _ruleEngine = ruleEngine;
    }

    /// <summary>
    /// معرف نوع الطلب
    /// </summary>
    [ActivityInput(
        Hint = "معرف نوع الطلب المراد معالجته",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public int RequestTypeId { get; set; }

    /// <summary>
    /// بيانات الطلب (JSON)
    /// </summary>
    [ActivityInput(
        Hint = "بيانات الطلب بصيغة JSON",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string RequestData { get; set; } = "{}";

    /// <summary>
    /// معرف المستأجر
    /// </summary>
    [ActivityInput(
        Hint = "معرف المستأجر (Tenant ID)",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public int TenantId { get; set; }

    /// <summary>
    /// معرف الطلب (اختياري)
    /// </summary>
    [ActivityInput(
        Hint = "معرف الطلب إذا كان موجوداً",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public int? RequestId { get; set; }

    /// <summary>
    /// إعداد الـ Workflow المحدد
    /// </summary>
    [ActivityOutput]
    public WorkflowConfigurationDto? WorkflowConfiguration { get; set; }

    /// <summary>
    /// نتيجة تقييم القوانين
    /// </summary>
    [ActivityOutput]
    public RuleEvaluationResultDto? RuleEvaluationResult { get; set; }

    /// <summary>
    /// الإجراء المطلوب
    /// </summary>
    [ActivityOutput]
    public string? RequiredAction { get; set; }

    /// <summary>
    /// رسالة الخطأ (إن وجدت)
    /// </summary>
    [ActivityOutput]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// البيانات المُحولة للمعالجة
    /// </summary>
    [ActivityOutput]
    public Dictionary<string, object>? ProcessedData { get; set; }

    public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            // تحويل بيانات الطلب من JSON
            var requestDataDict = ParseRequestData(RequestData);
            ProcessedData = requestDataDict;

            // الحصول على الإعداد المناسب للطلب
            var configuration = await _configurationService.GetActiveConfigurationForRequestAsync(
                RequestTypeId, requestDataDict, TenantId);

            if (configuration == null)
            {
                ErrorMessage = $"No active workflow configuration found for request type {RequestTypeId}";
                context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
                context.ExpressionExecutionContext.Set("WorkflowConfiguration", WorkflowConfiguration);
                context.ExpressionExecutionContext.Set("RuleEvaluationResult", RuleEvaluationResult);
                context.ExpressionExecutionContext.Set("RequiredAction", RequiredAction);
                context.ExpressionExecutionContext.Set("ProcessedData", ProcessedData);
                return Outcome("Error");
            }

            WorkflowConfiguration = configuration;

            // التحقق من شروط البداية
            var canStart = await _configurationService.CheckStartConditionsAsync(
                configuration.Id, requestDataDict);

            if (!canStart)
            {
                ErrorMessage = "Request does not meet start conditions for this workflow";
                context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
                return Outcome("Rejected");
            }

            // تقييم قوانين الـ Workflow
            var evaluationResult = await _configurationService.EvaluateWorkflowRulesAsync(
                configuration.Id, requestDataDict);

            RuleEvaluationResult = evaluationResult;

            if (!evaluationResult.IsValid)
            {
                ErrorMessage = $"Rule evaluation failed: {string.Join(", ", evaluationResult.Errors)}";
                context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
                return Outcome("Error");
            }

            // تحديد الإجراء المطلوب
            RequiredAction = DetermineRequiredAction(evaluationResult, configuration);

            // إضافة بيانات إضافية للـ workflow (Elsa 3.x syntax)
            context.ExpressionExecutionContext.Set("WorkflowConfigurationId", configuration.Id);
            context.ExpressionExecutionContext.Set("RequestTypeId", RequestTypeId);
            context.ExpressionExecutionContext.Set("TenantId", TenantId);
            context.ExpressionExecutionContext.Set("RequestId", RequestId);
            context.ExpressionExecutionContext.Set("RequiredAction", RequiredAction);
            context.ExpressionExecutionContext.Set("RequestData", requestDataDict);
            context.ExpressionExecutionContext.Set("EvaluationResult", evaluationResult);

            // إرجاع النتيجة المناسبة
            return RequiredAction switch
            {
                "AutoApprove" => Outcome("AutoApproved"),
                "RequireApproval" or "HighPriorityApproval" or "EscalateApproval" => Outcome("RequiresApproval"),
                "RejectRequest" => Outcome("Rejected"),
                _ => Outcome("RequiresApproval") // Default to requiring approval
            };
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error starting approval workflow: {ex.Message}";
            context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
            return Outcome("Error");
        }
    }

    /// <summary>
    /// تحويل بيانات الطلب من JSON إلى Dictionary
    /// </summary>
    private Dictionary<string, object> ParseRequestData(string requestData)
    {
        try
        {
            if (string.IsNullOrEmpty(requestData) || requestData == "{}")
            {
                return new Dictionary<string, object>();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(requestData, options);
            return result ?? new Dictionary<string, object>();
        }
        catch (JsonException)
        {
            // إذا فشل التحويل، إرجاع dictionary فارغ
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// تحديد الإجراء المطلوب بناءً على نتيجة التقييم والإعدادات
    /// </summary>
    private string DetermineRequiredAction(RuleEvaluationResultDto evaluationResult, WorkflowConfigurationDto configuration)
    {
        // إذا كان هناك إجراء محدد من القوانين
        if (!string.IsNullOrEmpty(evaluationResult.ResultAction))
        {
            return evaluationResult.ResultAction;
        }

        // إذا كان الإعداد يتطلب موافقة يدوية دائماً
        if (configuration.RequiresManualApproval)
        {
            return "RequireApproval";
        }

        // إذا لم تكن هناك قوانين مطابقة والإعداد لا يتطلب موافقة يدوية
        return "AutoApprove";
    }
}

/// <summary>
/// نشاط تقييم قوانين الموافقة (Elsa 3.x)
/// يقوم بتقييم قوانين محددة ضد بيانات الطلب
/// </summary>
[Activity(
    Category = "Approval System",
    DisplayName = "تقييم قوانين الموافقة",
    Description = "يقيم قوانين الموافقة المحددة ضد بيانات الطلب"
)]
public class EvaluateApprovalRulesActivity : Activity
{
    private readonly IRuleEngine _ruleEngine;

    public EvaluateApprovalRulesActivity(IRuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    /// <summary>
    /// قوانين التقييم (JSON)
    /// </summary>
    [ActivityInput(
        Hint = "قوانين التقييم بصيغة JSON",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string EvaluationRules { get; set; } = "[]";

    /// <summary>
    /// بيانات الطلب للتقييم
    /// </summary>
    [ActivityInput(
        Hint = "بيانات الطلب للتقييم ضدها",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string RequestData { get; set; } = "{}";

    /// <summary>
    /// نتيجة التقييم
    /// </summary>
    [ActivityOutput]
    public RuleEvaluationResultDto? EvaluationResult { get; set; }

    /// <summary>
    /// هل تم العثور على قوانين مطابقة؟
    /// </summary>
    [ActivityOutput]
    public bool HasMatches { get; set; }

    /// <summary>
    /// الإجراء المطلوب
    /// </summary>
    [ActivityOutput]
    public string? RequiredAction { get; set; }

    /// <summary>
    /// رسالة الخطأ
    /// </summary>
    [ActivityOutput]
    public string? ErrorMessage { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            // تحويل القوانين من JSON
            var rules = JsonSerializer.Deserialize<List<EvaluationRuleDto>>(EvaluationRules);
            if (rules == null)
            {
                ErrorMessage = "Invalid evaluation rules format";
                context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
                return Outcome("Error");
            }

            // تحويل بيانات الطلب من JSON
            var requestDataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(RequestData);
            if (requestDataDict == null)
            {
                ErrorMessage = "Invalid request data format";
                context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
                return Outcome("Error");
            }

            // تقييم القوانين
            var result = await _ruleEngine.EvaluateRulesAsync(rules, requestDataDict);

            EvaluationResult = result;
            HasMatches = result.MatchedRules.Any();
            RequiredAction = result.ResultAction;

            // إضافة النتائج للـ workflow context (Elsa 3.x syntax)
            context.ExpressionExecutionContext.Set("EvaluationResult", result);
            context.ExpressionExecutionContext.Set("HasMatches", HasMatches);
            context.ExpressionExecutionContext.Set("RequiredAction", RequiredAction);

            if (!result.IsValid)
            {
                ErrorMessage = string.Join(", ", result.Errors);
                context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
                return Outcome("Error");
            }

            return HasMatches ? Outcome("Matched") : Outcome("NotMatched");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error evaluating approval rules: {ex.Message}";
            context.ExpressionExecutionContext.Set("ErrorMessage", ErrorMessage);
            return Outcome("Error");
        }
    }
}

/// <summary>
/// نشاط إرسال طلب الموافقة مع Bookmark (Elsa 3.x)
/// يرسل طلب موافقة للمُوافق المحدد وينتظر القرار
/// </summary>
[Activity(
    Category = "Approval System",
    DisplayName = "إرسال طلب الموافقة",
    Description = "يرسل طلب موافقة للمُوافق وينتظر القرار"
)]
public class SendApprovalRequestActivity : Activity
{
    /// <summary>
    /// معرف المُوافق
    /// </summary>
    [ActivityInput(
        Hint = "معرف المستخدم المُوافق",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// معرف الطلب
    /// </summary>
    [ActivityInput(
        Hint = "معرف الطلب المراد الموافقة عليه",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public int RequestId { get; set; }

    /// <summary>
    /// مهلة انتظار القرار (بالساعات)
    /// </summary>
    [ActivityInput(
        Hint = "مهلة انتظار القرار بالساعات (default: 24)",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript }
    )]
    public int TimeoutHours { get; set; } = 24;

    /// <summary>
    /// رسالة مخصصة للمُوافق
    /// </summary>
    [ActivityInput(
        Hint = "رسالة مخصصة للمُوافق (اختياري)",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string? CustomMessage { get; set; }

    /// <summary>
    /// القرار المُستلم
    /// </summary>
    [ActivityOutput]
    public string? Decision { get; set; }

    /// <summary>
    /// تعليقات المُوافق
    /// </summary>
    [ActivityOutput]
    public string? ApproverComments { get; set; }

    /// <summary>
    /// وقت اتخاذ القرار
    /// </summary>
    [ActivityOutput]
    public DateTime? DecisionTime { get; set; }

    /// <summary>
    /// هل انتهت المهلة الزمنية؟
    /// </summary>
    [ActivityOutput]
    public bool IsTimedOut { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            // إنشاء bookmark للانتظار
            var bookmarkName = $"ApprovalRequest_{RequestId}_{ApproverId}_{Guid.NewGuid()}";

            // حفظ بيانات السياق (Elsa 3.x syntax)
            context.ExpressionExecutionContext.Set("ApprovalBookmark", bookmarkName);
            context.ExpressionExecutionContext.Set("ApproverId", ApproverId);
            context.ExpressionExecutionContext.Set("RequestId", RequestId);
            context.ExpressionExecutionContext.Set("ApprovalSentAt", DateTime.UtcNow);
            context.ExpressionExecutionContext.Set("TimeoutHours", TimeoutHours);

            // إرسال الإشعار للمُوافق (يتم تنفيذه في service منفصل)
            // هذا سيتم ربطه مع NotificationService لاحقاً

            // إنشاء bookmark للانتظار
            var bookmarkPayload = new
            {
                RequestId = RequestId,
                ApproverId = ApproverId,
                SentAt = DateTime.UtcNow,
                CustomMessage = CustomMessage
            };

            // إرجاع Suspend مع bookmark
            return Suspend(bookmarkName);
        }
        catch (Exception ex)
        {
            context.ExpressionExecutionContext.Set("ErrorMessage", $"Error sending approval request: {ex.Message}");
            return Outcome("Error");
        }
    }

    protected override async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        try
        {
            // الحصول على البيانات من input (Elsa 3.x syntax)
            var input = context.ExpressionExecutionContext.Get<ApprovalDecisionInput>("ApprovalDecisionInput");

            if (input == null)
            {
                return Outcome("Error");
            }

            Decision = input.Decision;
            ApproverComments = input.Comments;
            DecisionTime = input.DecisionTime;

            // تحديث متغيرات السياق (Elsa 3.x syntax)
            context.ExpressionExecutionContext.Set("ApprovalDecision", Decision);
            context.ExpressionExecutionContext.Set("ApproverComments", ApproverComments);
            context.ExpressionExecutionContext.Set("DecisionTime", DecisionTime);

            // إرجاع النتيجة المناسبة
            return Decision?.ToLower() switch
            {
                "approved" => Outcome("Approved"),
                "rejected" => Outcome("Rejected"),
                "returned" => Outcome("Returned"),
                _ => Outcome("Error")
            };
        }
        catch (Exception ex)
        {
            context.ExpressionExecutionContext.Set("ErrorMessage", $"Error processing approval decision: {ex.Message}");
            return Outcome("Error");
        }
    }
}

/// <summary>
/// بيانات قرار الموافقة
/// </summary>
public class ApprovalDecisionInput
{
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecisionTime { get; set; } = DateTime.UtcNow;
    public string ApproverId { get; set; } = string.Empty;
    public int RequestId { get; set; }
}
