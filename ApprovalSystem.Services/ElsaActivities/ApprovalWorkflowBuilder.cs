using Elsa.Activities.Workflows;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApprovalSystem.Services.ElsaActivities;

namespace ApprovalSystem.Services.ElsaActivities;

/// <summary>
/// تسجيل Custom Elsa Activities
/// </summary>
public class ApprovalSystemActivitiesStartup : IStartup
{
    public void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
    {
        elsa
            // إضافة Custom Activities
            .AddActivity<StartApprovalWorkflowActivity>()
            .AddActivity<EvaluateApprovalRulesActivity>()
            .AddActivity<SendApprovalRequestActivity>()
            .AddActivity<ProcessApprovalDecisionActivity>()
            .AddActivity<SendNotificationActivity>()
            .AddActivity<CompleteWorkflowActivity>()
            
            // إضافة Built-in Activities المطلوبة
            .AddActivitiesFrom<StartApprovalWorkflowActivity>()
            .AddHttpActivities()
            .AddEmailActivities()
            .AddTimerActivities()
            .AddUserTaskActivities()
            .AddConsoleActivities()
            .AddWorkflowActivities();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // تسجيل الخدمات المطلوبة للـ Activities
        // الخدمات مُسجلة بالفعل في Program.cs
    }
}

/// <summary>
/// Extension Methods لتسهيل تسجيل الـ Activities
/// </summary>
public static class ApprovalSystemElsaExtensions
{
    /// <summary>
    /// إضافة نظام الموافقات لـ Elsa
    /// </summary>
    public static ElsaOptionsBuilder AddApprovalSystemActivities(this ElsaOptionsBuilder elsa)
    {
        return elsa
            .AddActivity<StartApprovalWorkflowActivity>()
            .AddActivity<EvaluateApprovalRulesActivity>()
            .AddActivity<SendApprovalRequestActivity>()
            .AddActivity<ProcessApprovalDecisionActivity>()
            .AddActivity<SendNotificationActivity>()
            .AddActivity<CompleteWorkflowActivity>();
    }

    /// <summary>
    /// إضافة Startup للنظام
    /// </summary>
    public static IServiceCollection AddApprovalSystemElsaStartup(this IServiceCollection services)
    {
        return services.AddStartup<ApprovalSystemActivitiesStartup>();
    }
}

/// <summary>
/// معرفات ثوابت للـ Activities
/// </summary>
public static class ApprovalActivityConstants
{
    public const string StartApprovalWorkflow = "StartApprovalWorkflow";
    public const string EvaluateApprovalRules = "EvaluateApprovalRules";
    public const string SendApprovalRequest = "SendApprovalRequest";
    public const string ProcessApprovalDecision = "ProcessApprovalDecision";
    public const string SendNotification = "SendNotification";
    public const string CompleteWorkflow = "CompleteWorkflow";

    // Outcomes
    public static class Outcomes
    {
        public const string RequiresApproval = "RequiresApproval";
        public const string AutoApproved = "AutoApproved";
        public const string Rejected = "Rejected";
        public const string Approved = "Approved";
        public const string Returned = "Returned";
        public const string Timeout = "Timeout";
        public const string Escalated = "Escalated";
        public const string Sent = "Sent";
        public const string Failed = "Failed";
        public const string PartiallyFailed = "PartiallyFailed";
        public const string Completed = "Completed";
        public const string Success = "Success";
        public const string Error = "Error";
        public const string Matched = "Matched";
        public const string NotMatched = "NotMatched";
    }

    // Variable Names
    public static class Variables
    {
        public const string WorkflowConfigurationId = "WorkflowConfigurationId";
        public const string RequestTypeId = "RequestTypeId";
        public const string TenantId = "TenantId";
        public const string RequestId = "RequestId";
        public const string RequiredAction = "RequiredAction";
        public const string RequestData = "RequestData";
        public const string EvaluationResult = "EvaluationResult";
        public const string ApprovalDecision = "ApprovalDecision";
        public const string WorkflowStartTime = "WorkflowStartTime";
        public const string WorkflowCompleted = "WorkflowCompleted";
        public const string CompletionTime = "CompletionTime";
        public const string FinalStatus = "FinalStatus";
        public const string WorkflowDuration = "WorkflowDuration";
        public const string CurrentApprovalLevel = "CurrentApprovalLevel";
        public const string NextApprovalLevel = "NextApprovalLevel";
        public const string ApproverId = "ApproverId";
        public const string NextApproverId = "NextApproverId";
        public const string ApprovalBookmark = "ApprovalBookmark";
        public const string ApprovalSentAt = "ApprovalSentAt";
        public const string TimeoutHours = "TimeoutHours";
        public const string NotificationResults = "NotificationResults";
        public const string SuccessfulSends = "SuccessfulSends";
        public const string FailedSends = "FailedSends";
        public const string ErrorMessage = "ErrorMessage";
        public const string ProcessedBy = "ProcessedBy";
    }
}

/// <summary>
/// Helper لإنشاء Workflow definitions برمجياً
/// </summary>
public class ApprovalWorkflowBuilder
{
    private readonly List<object> _activities = new();
    private readonly List<object> _connections = new();
    private int _activityIdCounter = 0;

    /// <summary>
    /// إضافة نشاط بدء الموافقة
    /// </summary>
    public ApprovalWorkflowBuilder AddStartApprovalActivity(int requestTypeId, int tenantId, int? requestId = null)
    {
        var activityId = $"activity_{++_activityIdCounter}";
        
        _activities.Add(new
        {
            activityId = activityId,
            type = ApprovalActivityConstants.StartApprovalWorkflow,
            displayName = "بدء الموافقة",
            properties = new
            {
                RequestTypeId = requestTypeId,
                TenantId = tenantId,
                RequestId = requestId,
                RequestData = "{{ Variables.RequestData }}"
            },
            outcomes = new[] 
            { 
                ApprovalActivityConstants.Outcomes.RequiresApproval,
                ApprovalActivityConstants.Outcomes.AutoApproved,
                ApprovalActivityConstants.Outcomes.Rejected,
                ApprovalActivityConstants.Outcomes.Error
            }
        });

        return this;
    }

    /// <summary>
    /// إضافة نشاط تقييم القوانين
    /// </summary>
    public ApprovalWorkflowBuilder AddEvaluateRulesActivity(string rulesJson)
    {
        var activityId = $"activity_{++_activityIdCounter}";
        
        _activities.Add(new
        {
            activityId = activityId,
            type = ApprovalActivityConstants.EvaluateApprovalRules,
            displayName = "تقييم القوانين",
            properties = new
            {
                EvaluationRules = rulesJson,
                RequestData = "{{ Variables.RequestData }}"
            },
            outcomes = new[] 
            { 
                ApprovalActivityConstants.Outcomes.Matched,
                ApprovalActivityConstants.Outcomes.NotMatched,
                ApprovalActivityConstants.Outcomes.Error
            }
        });

        return this;
    }

    /// <summary>
    /// إضافة نشاط إرسال طلب الموافقة
    /// </summary>
    public ApprovalWorkflowBuilder AddSendApprovalRequestActivity(string approverId, int timeoutHours = 24)
    {
        var activityId = $"activity_{++_activityIdCounter}";
        
        _activities.Add(new
        {
            activityId = activityId,
            type = ApprovalActivityConstants.SendApprovalRequest,
            displayName = "إرسال طلب الموافقة",
            properties = new
            {
                ApproverId = approverId,
                RequestId = "{{ Variables.RequestId }}",
                TimeoutHours = timeoutHours,
                CustomMessage = ""
            },
            outcomes = new[] 
            { 
                ApprovalActivityConstants.Outcomes.Approved,
                ApprovalActivityConstants.Outcomes.Rejected,
                ApprovalActivityConstants.Outcomes.Returned,
                ApprovalActivityConstants.Outcomes.Timeout,
                ApprovalActivityConstants.Outcomes.Error
            }
        });

        return this;
    }

    /// <summary>
    /// إضافة نشاط معالجة القرار
    /// </summary>
    public ApprovalWorkflowBuilder AddProcessDecisionActivity()
    {
        var activityId = $"activity_{++_activityIdCounter}";
        
        _activities.Add(new
        {
            activityId = activityId,
            type = ApprovalActivityConstants.ProcessApprovalDecision,
            displayName = "معالجة القرار",
            properties = new
            {
                RequestId = "{{ Variables.RequestId }}",
                Decision = "{{ Variables.ApprovalDecision.Decision }}",
                ApproverId = "{{ Variables.ApprovalDecision.ApproverId }}",
                Comments = "{{ Variables.ApprovalDecision.Comments }}",
                CurrentApprovalLevel = "{{ Variables.CurrentApprovalLevel }}"
            },
            outcomes = new[] 
            { 
                ApprovalActivityConstants.Outcomes.Success,
                ApprovalActivityConstants.Outcomes.Failed,
                ApprovalActivityConstants.Outcomes.Escalated,
                ApprovalActivityConstants.Outcomes.Error
            }
        });

        return this;
    }

    /// <summary>
    /// إضافة نشاط إرسال الإشعارات
    /// </summary>
    public ApprovalWorkflowBuilder AddSendNotificationActivity(
        string notificationType, 
        string[] recipients, 
        string[] channels,
        string subject,
        string message)
    {
        var activityId = $"activity_{++_activityIdCounter}";
        
        _activities.Add(new
        {
            activityId = activityId,
            type = ApprovalActivityConstants.SendNotification,
            displayName = "إرسال إشعار",
            properties = new
            {
                NotificationType = notificationType,
                Recipients = System.Text.Json.JsonSerializer.Serialize(recipients),
                Channels = System.Text.Json.JsonSerializer.Serialize(channels),
                Subject = subject,
                Message = message,
                AdditionalData = "{{ Variables.RequestData }}",
                Priority = "Normal"
            },
            outcomes = new[] 
            { 
                ApprovalActivityConstants.Outcomes.Sent,
                ApprovalActivityConstants.Outcomes.Failed,
                ApprovalActivityConstants.Outcomes.PartiallyFailed,
                ApprovalActivityConstants.Outcomes.Error
            }
        });

        return this;
    }

    /// <summary>
    /// إضافة نشاط إكمال الـ Workflow
    /// </summary>
    public ApprovalWorkflowBuilder AddCompleteWorkflowActivity(string finalStatus, bool sendNotification = true)
    {
        var activityId = $"activity_{++_activityIdCounter}";
        
        _activities.Add(new
        {
            activityId = activityId,
            type = ApprovalActivityConstants.CompleteWorkflow,
            displayName = "إكمال الـ Workflow",
            properties = new
            {
                RequestId = "{{ Variables.RequestId }}",
                FinalStatus = finalStatus,
                CompletionNotes = "",
                CompletionData = "{}",
                SendCompletionNotification = sendNotification
            },
            outcomes = new[] 
            { 
                ApprovalActivityConstants.Outcomes.Completed,
                ApprovalActivityConstants.Outcomes.Failed,
                ApprovalActivityConstants.Outcomes.Error
            }
        });

        return this;
    }

    /// <summary>
    /// ربط نشاطين
    /// </summary>
    public ApprovalWorkflowBuilder Connect(int fromActivityIndex, string outcome, int toActivityIndex)
    {
        if (fromActivityIndex < 0 || fromActivityIndex >= _activities.Count ||
            toActivityIndex < 0 || toActivityIndex >= _activities.Count)
        {
            throw new ArgumentException("Invalid activity index");
        }

        var fromActivity = _activities[fromActivityIndex] as dynamic;
        var toActivity = _activities[toActivityIndex] as dynamic;

        _connections.Add(new
        {
            sourceActivityId = fromActivity.activityId,
            targetActivityId = toActivity.activityId,
            outcome = outcome
        });

        return this;
    }

    /// <summary>
    /// بناء الـ Workflow Definition
    /// </summary>
    public object Build(string name, string description, int version = 1)
    {
        return new
        {
            name = name,
            displayName = name,
            description = description,
            version = version,
            isLatest = true,
            isPublished = true,
            activities = _activities,
            connections = _connections,
            variables = new
            {
                WorkflowStartTime = DateTime.UtcNow,
                CurrentApprovalLevel = 1
            }
        };
    }

    /// <summary>
    /// إنشاء Workflow أساسي للموافقة
    /// </summary>
    public static object CreateBasicApprovalWorkflow(
        string name,
        int requestTypeId,
        int tenantId,
        string approverId,
        string[] notificationRecipients)
    {
        var builder = new ApprovalWorkflowBuilder();

        return builder
            .AddStartApprovalActivity(requestTypeId, tenantId)
            .AddSendApprovalRequestActivity(approverId)
            .AddProcessDecisionActivity()
            .AddSendNotificationActivity(
                "RequestProcessed",
                notificationRecipients,
                new[] { "Email", "InApp" },
                "تم معالجة طلبك",
                "تم الانتهاء من معالجة طلبك. يرجى مراجعة النتيجة.")
            .AddCompleteWorkflowActivity("{{ Variables.FinalStatus }}")
            // ربط الأنشطة
            .Connect(0, ApprovalActivityConstants.Outcomes.RequiresApproval, 1)
            .Connect(1, ApprovalActivityConstants.Outcomes.Approved, 2)
            .Connect(1, ApprovalActivityConstants.Outcomes.Rejected, 2)
            .Connect(2, ApprovalActivityConstants.Outcomes.Success, 3)
            .Connect(3, ApprovalActivityConstants.Outcomes.Sent, 4)
            .Build(name, $"Workflow موافقة أساسي لـ {name}");
    }

    /// <summary>
    /// إنشاء Workflow متعدد المستويات
    /// </summary>
    public static object CreateMultiLevelApprovalWorkflow(
        string name,
        int requestTypeId,
        int tenantId,
        string[] approverIds,
        string[] notificationRecipients)
    {
        var builder = new ApprovalWorkflowBuilder();

        // بدء الـ workflow
        builder.AddStartApprovalActivity(requestTypeId, tenantId);

        // إضافة مستويات الموافقة
        for (int i = 0; i < approverIds.Length; i++)
        {
            builder.AddSendApprovalRequestActivity(approverIds[i]);
            builder.AddProcessDecisionActivity();
        }

        // إشعار وإكمال
        builder.AddSendNotificationActivity(
            "RequestProcessed",
            notificationRecipients,
            new[] { "Email", "InApp" },
            "تم معالجة طلبك",
            "تم الانتهاء من معالجة طلبك متعدد المستويات.");

        builder.AddCompleteWorkflowActivity("{{ Variables.FinalStatus }}");

        // ربط الأنشطة (مبسط - يحتاج تعقيد أكثر للمستويات المتعددة)
        for (int i = 0; i < approverIds.Length; i++)
        {
            int sendApprovalIndex = 1 + (i * 2);
            int processDecisionIndex = 2 + (i * 2);
            
            if (i == 0)
            {
                builder.Connect(0, ApprovalActivityConstants.Outcomes.RequiresApproval, sendApprovalIndex);
            }
            else
            {
                int prevProcessIndex = i * 2;
                builder.Connect(prevProcessIndex, ApprovalActivityConstants.Outcomes.Success, sendApprovalIndex);
            }

            builder.Connect(sendApprovalIndex, ApprovalActivityConstants.Outcomes.Approved, processDecisionIndex);
            builder.Connect(sendApprovalIndex, ApprovalActivityConstants.Outcomes.Rejected, processDecisionIndex);
        }

        // ربط النشاط الأخير بالإشعار والإكمال
        int lastProcessIndex = approverIds.Length * 2;
        int notificationIndex = lastProcessIndex + 1;
        int completeIndex = lastProcessIndex + 2;

        builder.Connect(lastProcessIndex, ApprovalActivityConstants.Outcomes.Success, notificationIndex);
        builder.Connect(notificationIndex, ApprovalActivityConstants.Outcomes.Sent, completeIndex);

        return builder.Build(name, $"Workflow موافقة متعدد المستويات لـ {name}");
    }
}