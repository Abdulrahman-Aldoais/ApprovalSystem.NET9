using ApprovalSystem.Core.Interfaces;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using System.Text.Json;

namespace ApprovalSystem.Services.ElsaActivities;

/// <summary>
/// نشاط معالجة قرار الموافقة (Elsa 3.x)
/// يقوم بمعالجة قرار الموافقة وتحديث حالة الطلب
/// </summary>
[Activity(
    Category = "Approval System",
    DisplayName = "معالجة قرار الموافقة",
    Description = "يعالج قرار الموافقة ويحدث حالة الطلب في قاعدة البيانات"
)]
public class ProcessApprovalDecisionActivity : Activity
{
    private readonly IApprovalService _approvalService;
    private readonly IRequestService _requestService;

    public ProcessApprovalDecisionActivity(
        IApprovalService approvalService,
        IRequestService requestService)
    {
        _approvalService = approvalService;
        _requestService = requestService;
    }

    /// <summary>
    /// معرف الطلب
    /// </summary>
    [ActivityInput(
        Hint = "معرف الطلب المراد معالجة قراره",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public int RequestId { get; set; }

    /// <summary>
    /// قرار الموافقة
    /// </summary>
    [ActivityInput(
        Hint = "قرار الموافقة (Approved, Rejected, Returned)",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string Decision { get; set; } = string.Empty;

    /// <summary>
    /// معرف المُوافق
    /// </summary>
    [ActivityInput(
        Hint = "معرف المستخدم الذي اتخذ القرار",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// تعليقات المُوافق
    /// </summary>
    [ActivityInput(
        Hint = "تعليقات المُوافق على القرار",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string? Comments { get; set; }

    /// <summary>
    /// مستوى الموافقة الحالي
    /// </summary>
    [ActivityInput(
        Hint = "مستوى الموافقة الحالي",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript }
    )]
    public int CurrentApprovalLevel { get; set; } = 1;

    /// <summary>
    /// هل يتطلب موافقات إضافية؟
    /// </summary>
    [ActivityOutput]
    public bool RequiresAdditionalApprovals { get; set; }

    /// <summary>
    /// المستوى التالي للموافقة
    /// </summary>
    [ActivityOutput]
    public int? NextApprovalLevel { get; set; }

    /// <summary>
    /// المُوافق التالي
    /// </summary>
    [ActivityOutput]
    public string? NextApproverId { get; set; }

    /// <summary>
    /// الحالة الجديدة للطلب
    /// </summary>
    [ActivityOutput]
    public string? NewRequestStatus { get; set; }

    /// <summary>
    /// رسالة النتيجة
    /// </summary>
    [ActivityOutput]
    public string? ResultMessage { get; set; }

    /// <summary>
    /// رسالة الخطأ
    /// </summary>
    [ActivityOutput]
    public string? ErrorMessage { get; set; }

    public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            // إنشاء قرار الموافقة
            var approvalDecision = new
            {
                RequestId = RequestId,
                ApproverId = ApproverId,
                Decision = Decision,
                Comments = Comments,
                ApprovalLevel = CurrentApprovalLevel,
                DecisionTime = DateTime.UtcNow
            };

            // معالجة القرار حسب نوعه
            switch (Decision.ToLower())
            {
                case "approved":
                    await ProcessApprovalDecision(context, approvalDecision);
                    break;

                case "rejected":
                    await ProcessRejectionDecision(context, approvalDecision);
                    break;

                case "returned":
                    await ProcessReturnDecision(context, approvalDecision);
                    break;

                default:
                    ErrorMessage = $"Invalid decision: {Decision}";
                    context.WorkflowExecutionContext.SetVariable("ErrorMessage", ErrorMessage);
                    return new Outcome("Error");

            }

            // حفظ البيانات في السياق (Elsa 3.x syntax)
            context.WorkflowExecutionContext.SetVariable("ApprovalDecision", approvalDecision);
            context.WorkflowExecutionContext.SetVariable("ProcessedAt", DateTime.UtcNow);
            context.WorkflowExecutionContext.SetVariable("NewRequestStatus", NewRequestStatus);
            context.WorkflowExecutionContext.SetVariable("RequiresAdditionalApprovals", RequiresAdditionalApprovals);

            return new Outcome("Success");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing approval decision: {ex.Message}";
            context.WorkflowExecutionContext.SetVariable("ErrorMessage", ErrorMessage);
            return new Outcome("Error");
        }
    }

    /// <summary>
    /// معالجة قرار الموافقة
    /// </summary>
    private async Task ProcessApprovalDecision(ActivityExecutionContext context, object approvalDecision)
    {
        // التحقق من وجود مستويات موافقة إضافية
        RequiresAdditionalApprovals = await CheckForAdditionalApprovals();

        if (RequiresAdditionalApprovals)
        {
            // الانتقال للمستوى التالي
            NextApprovalLevel = CurrentApprovalLevel + 1;
            NextApproverId = await GetNextApprover(NextApprovalLevel.Value);
            NewRequestStatus = "Pending Next Approval";
            ResultMessage = $"Approved at level {CurrentApprovalLevel}. Moving to level {NextApprovalLevel}";
        }
        else
        {
            // الموافقة النهائية
            NewRequestStatus = "Approved";
            ResultMessage = "Request fully approved";
        }
    }

    /// <summary>
    /// معالجة قرار الرفض
    /// </summary>
    private async Task ProcessRejectionDecision(ActivityExecutionContext context, object approvalDecision)
    {
        NewRequestStatus = "Rejected";
        RequiresAdditionalApprovals = false;
        ResultMessage = "Request rejected";
    }

    /// <summary>
    /// معالجة قرار الإرجاع
    /// </summary>
    private async Task ProcessReturnDecision(ActivityExecutionContext context, object approvalDecision)
    {
        NewRequestStatus = "Returned for Revision";
        RequiresAdditionalApprovals = false;
        ResultMessage = "Request returned for revision";
    }

    /// <summary>
    /// التحقق من وجود مستويات موافقة إضافية
    /// </summary>
    private async Task<bool> CheckForAdditionalApprovals()
    {
        // هذا سيتم ربطه مع ApprovalMatrix service لاحقاً
        // مؤقتاً نفترض أن هناك 3 مستويات كحد أقصى
        return CurrentApprovalLevel < 3;
    }

    /// <summary>
    /// الحصول على المُوافق التالي
    /// </summary>
    private async Task<string?> GetNextApprover(int level)
    {
        // هذا سيتم ربطه مع ApprovalMatrix service لاحقاً
        // مؤقتاً نرجع null
        return null;
    }
}

/// <summary>
/// نشاط إرسال الإشعارات (Elsa 3.x)
/// يرسل إشعارات عبر قنوات متعددة (Email, SMS, Real-time)
/// </summary>
[Activity(
    Category = "Approval System",
    DisplayName = "إرسال إشعار",
    Description = "يرسل إشعارات عبر القنوات المختلفة (Email, SMS, في التطبيق)"
)]
public class SendNotificationActivity : Activity
{
    private readonly INotificationService _notificationService;

    public SendNotificationActivity(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// نوع الإشعار
    /// </summary>
    [ActivityInput(
        Hint = "نوع الإشعار (RequestSubmitted, RequestApproved, etc.)",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript }
    )]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// المستقبلين
    /// </summary>
    [ActivityInput(
        Hint = "قائمة معرفات المستقبلين (JSON array)",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript }
    )]
    public string Recipients { get; set; } = "[]";

    /// <summary>
    /// القنوات المطلوبة
    /// </summary>
    [ActivityInput(
        Hint = "القنوات المطلوبة (Email, SMS, InApp, RealTime)",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript }
    )]
    public string Channels { get; set; } = "[\"Email\", \"InApp\"]";

    /// <summary>
    /// عنوان الرسالة
    /// </summary>
    [ActivityInput(
        Hint = "عنوان الرسالة",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// محتوى الرسالة
    /// </summary>
    [ActivityInput(
        Hint = "محتوى الرسالة",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// بيانات إضافية (JSON)
    /// </summary>
    [ActivityInput(
        Hint = "بيانات إضافية للإشعار (JSON)",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript }
    )]
    public string AdditionalData { get; set; } = "{}";

    /// <summary>
    /// أولوية الإشعار
    /// </summary>
    [ActivityInput(
        Hint = "أولوية الإشعار (Low, Normal, High, Critical)",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript }
    )]
    public string Priority { get; set; } = "Normal";

    /// <summary>
    /// عدد الإشعارات المُرسلة بنجاح
    /// </summary>
    [ActivityOutput]
    public int SuccessfulSends { get; set; }

    /// <summary>
    /// عدد الإشعارات الفاشلة
    /// </summary>
    [ActivityOutput]
    public int FailedSends { get; set; }

    /// <summary>
    /// تفاصيل النتائج
    /// </summary>
    [ActivityOutput]
    public List<string> SendResults { get; set; } = new();

    /// <summary>
    /// رسالة الخطأ
    /// </summary>
    [ActivityOutput]
    public string? ErrorMessage { get; set; }

    public override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            // تحويل البيانات من JSON
            var recipientsList = JsonSerializer.Deserialize<List<string>>(Recipients) ?? new List<string>();
            var channelsList = JsonSerializer.Deserialize<List<string>>(Channels) ?? new List<string>();
            var additionalDataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalData) ?? new Dictionary<string, object>();

            if (!recipientsList.Any())
            {
                ErrorMessage = "No recipients specified";
                context.WorkflowExecutionContext.SetVariable("ErrorMessage", ErrorMessage);
                return Outcome.FromResult("Error");
            }

            if (!channelsList.Any())
            {
                ErrorMessage = "No channels specified";
                context.WorkflowExecutionContext.SetVariable("ErrorMessage", ErrorMessage);
                return Outcome.FromResult("Error");
            }

            // إرسال الإشعارات
            var sendTasks = new List<Task<bool>>();
            var results = new List<string>();

            foreach (var recipient in recipientsList)
            {
                foreach (var channel in channelsList)
                {
                    var task = SendNotificationAsync(recipient, channel, additionalDataDict);
                    sendTasks.Add(task);
                }
            }

            // انتظار إكمال جميع المهام
            var taskResults = await Task.WhenAll(sendTasks);

            // حساب النتائج
            SuccessfulSends = taskResults.Count(r => r);
            FailedSends = taskResults.Count(r => !r);

            // تسجيل النتائج في السياق (Elsa 3.x syntax)
            context.WorkflowExecutionContext.SetVariable("NotificationResults", new
            {
                Type = NotificationType,
                Recipients = recipientsList,
                Channels = channelsList,
                SuccessfulSends = SuccessfulSends,
                FailedSends = FailedSends,
                SentAt = DateTime.UtcNow
            });

            // تحديد النتيجة
            if (FailedSends == 0)
            {
                return new Outcome("Sent");
            }
            else if (SuccessfulSends > 0)
            {
                return new Outcome("PartiallyFailed");
            }
            else
            {
                return new Outcome("Failed");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error sending notifications: {ex.Message}";
            context.WorkflowExecutionContext.SetVariable("ErrorMessage", ErrorMessage);
            return Outcome("Error");
        }
    }

    /// <summary>
    /// إرسال إشعار واحد
    /// </summary>
    private async Task<bool> SendNotificationAsync(string recipient, string channel, Dictionary<string, object> additionalData)
    {
        try
        {
            // هذا سيتم ربطه مع NotificationService الفعلي
            // مؤقتاً نحاكي الإرسال
            await Task.Delay(100); // محاكاة زمن الإرسال

            SendResults.Add($"{channel} notification sent to {recipient} successfully");
            return true;
        }
        catch (Exception ex)
        {
            SendResults.Add($"Failed to send {channel} notification to {recipient}: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// نشاط إكمال الـ Workflow (Elsa 3.x)
/// يقوم بإكمال الـ workflow وتحديث حالة الطلب النهائية
/// </summary>
[Activity(
    Category = "Approval System",
    DisplayName = "إكمال الـ Workflow",
    Description = "يكمل الـ workflow ويحدث الحالة النهائية للطلب"
)]
public class CompleteWorkflowActivity : Activity
{
    private readonly IRequestService _requestService;
    private readonly IWorkflowService _workflowService;

    public CompleteWorkflowActivity(
        IRequestService requestService,
        IWorkflowService workflowService)
    {
        _requestService = requestService;
        _workflowService = workflowService;
    }

    /// <summary>
    /// معرف الطلب
    /// </summary>
    [ActivityInput(
        Hint = "معرف الطلب المراد إكماله",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public int RequestId { get; set; }

    /// <summary>
    /// الحالة النهائية
    /// </summary>
    [ActivityInput(
        Hint = "الحالة النهائية للطلب (Approved, Rejected, Cancelled)",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string FinalStatus { get; set; } = string.Empty;

    /// <summary>
    /// ملاحظات الإكمال
    /// </summary>
    [ActivityInput(
        Hint = "ملاحظات حول إكمال الـ workflow",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
    )]
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// بيانات إضافية للإكمال
    /// </summary>
    [ActivityInput(
        Hint = "بيانات إضافية للإكمال (JSON)",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript }
    )]
    public string CompletionData { get; set; } = "{}";

    /// <summary>
    /// هل إرسال إشعار إكمال؟
    /// </summary>
    [ActivityInput(
        Hint = "هل إرسال إشعار عند الإكمال؟",
        UIHint = ActivityInputUIHints.Checkbox,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript }
    )]
    public bool SendCompletionNotification { get; set; } = true;

    /// <summary>
    /// وقت الإكمال
    /// </summary>
    [ActivityOutput]
    public DateTime CompletionTime { get; set; }

    /// <summary>
    /// مدة الـ Workflow الإجمالية
    /// </summary>
    [ActivityOutput]
    public TimeSpan? WorkflowDuration { get; set; }

    /// <summary>
    /// إحصائيات الـ Workflow
    /// </summary>
    [ActivityOutput]
    public Dictionary<string, object> WorkflowStatistics { get; set; } = new();

    /// <summary>
    /// رسالة النجاح
    /// </summary>
    [ActivityOutput]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// رسالة الخطأ
    /// </summary>
    [ActivityOutput]
    public string? ErrorMessage { get; set; }

    public override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        try
        {
            CompletionTime = DateTime.UtcNow;

            // الحصول على بيانات الـ workflow من السياق (Elsa 3.x syntax)
            var workflowStartTime = context.WorkflowExecutionContext.GetVariable<DateTime?>("WorkflowStartTime");
            if (workflowStartTime.HasValue)
            {
                WorkflowDuration = CompletionTime - workflowStartTime.Value;
            }

            // تحديث حالة الطلب
            // هذا سيتم ربطه مع RequestService الفعلي
            var updateResult = await UpdateRequestStatus();

            if (!updateResult)
            {
                ErrorMessage = "Failed to update request status";
                context.WorkflowExecutionContext.SetVariable("ErrorMessage", ErrorMessage);
                return Outcome.FromResult("Failed");
            }

            // إنشاء إحصائيات الـ Workflow
            await GenerateWorkflowStatistics(context);

            // حفظ بيانات الإكمال في السياق (Elsa 3.x syntax)
            context.WorkflowExecutionContext.SetVariable("WorkflowCompleted", true);
            context.WorkflowExecutionContext.SetVariable("CompletionTime", CompletionTime);
            context.WorkflowExecutionContext.SetVariable("FinalStatus", FinalStatus);
            context.WorkflowExecutionContext.SetVariable("WorkflowDuration", WorkflowDuration);
            context.WorkflowExecutionContext.SetVariable("CompletionData", JsonSerializer.Deserialize<object>(CompletionData));

            SuccessMessage = $"Workflow completed successfully with status: {FinalStatus}";

            return Outcome.FromResult("Completed");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error completing workflow: {ex.Message}";
            context.WorkflowExecutionContext.SetVariable("ErrorMessage", ErrorMessage);
            return Outcome.FromResult("Error");
        }
    }

    /// <summary>
    /// تحديث حالة الطلب
    /// </summary>
    private async Task<bool> UpdateRequestStatus()
    {
        try
        {
            // مؤقتاً نرجع true
            // سيتم ربطه مع RequestService الفعلي لاحقاً
            await Task.Delay(100);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// إنشاء إحصائيات الـ Workflow
    /// </summary>
    private async Task GenerateWorkflowStatistics(ActivityExecutionContext context)
    {
        try
        {
            WorkflowStatistics = new Dictionary<string, object>
            {
                ["RequestId"] = RequestId,
                ["FinalStatus"] = FinalStatus,
                ["CompletionTime"] = CompletionTime,
                ["Duration"] = WorkflowDuration?.TotalHours ?? 0,
                ["WorkflowId"] = context.WorkflowInstance.Id,
                ["TenantId"] = context.WorkflowExecutionContext.GetVariable<int>("TenantId"),
                ["ProcessedBy"] = context.WorkflowExecutionContext.GetVariable<string>("ProcessedBy") ?? "System",
                ["ApprovalLevels"] = context.WorkflowExecutionContext.GetVariable<int>("CurrentApprovalLevel"),
                ["NotificationsSent"] = context.WorkflowExecutionContext.GetVariable<int>("SuccessfulSends"),
                ["CompletionNotes"] = CompletionNotes ?? ""
            };

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // تسجيل الخطأ لكن عدم إيقاف الـ workflow
            WorkflowStatistics["StatisticsError"] = ex.Message;
        }
    }
}
