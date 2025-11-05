using ApprovalSystem.Services.ElsaActivities;
using System.Text.Json;

namespace ApprovalSystem.Services.ElsaActivities;

/// <summary>
/// قوالب Workflow جاهزة للاستعمال
/// </summary>
public static class WorkflowTemplates
{
    /// <summary>
    /// قالب الموافقة الأساسي
    /// مناسب للطلبات البسيطة التي تحتاج موافقة واحدة
    /// </summary>
    public static string BasicApprovalTemplate => JsonSerializer.Serialize(new
    {
        name = "BasicApprovalWorkflow",
        displayName = "سير عمل الموافقة الأساسي",
        description = "سير عمل أساسي للموافقة على الطلبات البسيطة",
        version = 1,
        isLatest = true,
        isPublished = true,
        variables = new
        {
            WorkflowStartTime = "{{ NowUtc }}",
            CurrentApprovalLevel = 1,
            MaxApprovalLevels = 1
        },
        activities = new[]
        {
            new
            {
                activityId = "start_approval",
                type = ApprovalActivityConstants.StartApprovalWorkflow,
                displayName = "بدء الموافقة",
                properties = new
                {
                    RequestTypeId = "{{ Variables.RequestTypeId }}",
                    TenantId = "{{ Variables.TenantId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    RequestData = "{{ Variables.RequestData }}"
                },
                outcomes = new[] 
                { 
                    ApprovalActivityConstants.Outcomes.RequiresApproval,
                    ApprovalActivityConstants.Outcomes.AutoApproved,
                    ApprovalActivityConstants.Outcomes.Rejected
                }
            },
            new
            {
                activityId = "send_approval_request",
                type = ApprovalActivityConstants.SendApprovalRequest,
                displayName = "إرسال طلب الموافقة",
                properties = new
                {
                    ApproverId = "{{ Variables.ApproverId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    TimeoutHours = 24,
                    CustomMessage = "يرجى مراجعة الطلب واتخاذ القرار المناسب"
                },
                outcomes = new[] 
                { 
                    ApprovalActivityConstants.Outcomes.Approved,
                    ApprovalActivityConstants.Outcomes.Rejected,
                    ApprovalActivityConstants.Outcomes.Returned,
                    ApprovalActivityConstants.Outcomes.Timeout
                }
            },
            new
            {
                activityId = "process_decision",
                type = ApprovalActivityConstants.ProcessApprovalDecision,
                displayName = "معالجة القرار",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    Decision = "{{ Variables.ApprovalDecision }}",
                    ApproverId = "{{ Variables.ApproverId }}",
                    Comments = "{{ Variables.ApproverComments }}",
                    CurrentApprovalLevel = 1
                },
                outcomes = new[] 
                { 
                    ApprovalActivityConstants.Outcomes.Success,
                    ApprovalActivityConstants.Outcomes.Failed
                }
            },
            new
            {
                activityId = "send_notification",
                type = ApprovalActivityConstants.SendNotification,
                displayName = "إرسال إشعار",
                properties = new
                {
                    NotificationType = "RequestProcessed",
                    Recipients = "[\"{{ Variables.RequesterId }}\"]",
                    Channels = "[\"Email\", \"InApp\"]",
                    Subject = "تم معالجة طلبك",
                    Message = "تم الانتهاء من معالجة طلبك. الحالة: {{ Variables.FinalStatus }}",
                    Priority = "Normal"
                },
                outcomes = new[] 
                { 
                    ApprovalActivityConstants.Outcomes.Sent,
                    ApprovalActivityConstants.Outcomes.Failed
                }
            },
            new
            {
                activityId = "complete_workflow",
                type = ApprovalActivityConstants.CompleteWorkflow,
                displayName = "إكمال سير العمل",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    FinalStatus = "{{ Variables.FinalStatus }}",
                    CompletionNotes = "تم إكمال سير العمل الأساسي بنجاح",
                    SendCompletionNotification = true
                },
                outcomes = new[] 
                { 
                    ApprovalActivityConstants.Outcomes.Completed
                }
            }
        },
        connections = new[]
        {
            new { sourceActivityId = "start_approval", targetActivityId = "send_approval_request", outcome = ApprovalActivityConstants.Outcomes.RequiresApproval },
            new { sourceActivityId = "start_approval", targetActivityId = "complete_workflow", outcome = ApprovalActivityConstants.Outcomes.AutoApproved },
            new { sourceActivityId = "start_approval", targetActivityId = "complete_workflow", outcome = ApprovalActivityConstants.Outcomes.Rejected },
            new { sourceActivityId = "send_approval_request", targetActivityId = "process_decision", outcome = ApprovalActivityConstants.Outcomes.Approved },
            new { sourceActivityId = "send_approval_request", targetActivityId = "process_decision", outcome = ApprovalActivityConstants.Outcomes.Rejected },
            new { sourceActivityId = "send_approval_request", targetActivityId = "process_decision", outcome = ApprovalActivityConstants.Outcomes.Returned },
            new { sourceActivityId = "process_decision", targetActivityId = "send_notification", outcome = ApprovalActivityConstants.Outcomes.Success },
            new { sourceActivityId = "send_notification", targetActivityId = "complete_workflow", outcome = ApprovalActivityConstants.Outcomes.Sent }
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// قالب الموافقة متعددة المستويات
    /// مناسب للطلبات المعقدة التي تحتاج موافقات متعددة
    /// </summary>
    public static string MultiLevelApprovalTemplate => JsonSerializer.Serialize(new
    {
        name = "MultiLevelApprovalWorkflow",
        displayName = "سير عمل الموافقة متعدد المستويات",
        description = "سير عمل للموافقة على الطلبات عبر مستويات متعددة",
        version = 1,
        isLatest = true,
        isPublished = true,
        variables = new
        {
            WorkflowStartTime = "{{ NowUtc }}",
            CurrentApprovalLevel = 1,
            MaxApprovalLevels = 3,
            ApprovalMatrix = "{{ Variables.ApprovalMatrix }}"
        },
        activities = new[]
        {
            new
            {
                activityId = "start_approval",
                type = ApprovalActivityConstants.StartApprovalWorkflow,
                displayName = "بدء الموافقة",
                properties = new
                {
                    RequestTypeId = "{{ Variables.RequestTypeId }}",
                    TenantId = "{{ Variables.TenantId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    RequestData = "{{ Variables.RequestData }}"
                }
            },
            new
            {
                activityId = "evaluate_rules",
                type = ApprovalActivityConstants.EvaluateApprovalRules,
                displayName = "تقييم القوانين",
                properties = new
                {
                    EvaluationRules = "{{ Variables.EvaluationRules }}",
                    RequestData = "{{ Variables.RequestData }}"
                }
            },
            new
            {
                activityId = "send_level1_approval",
                type = ApprovalActivityConstants.SendApprovalRequest,
                displayName = "موافقة المستوى الأول",
                properties = new
                {
                    ApproverId = "{{ Variables.Level1ApproverId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    TimeoutHours = 24,
                    CustomMessage = "طلب موافقة المستوى الأول - يرجى المراجعة"
                }
            },
            new
            {
                activityId = "process_level1_decision",
                type = ApprovalActivityConstants.ProcessApprovalDecision,
                displayName = "معالجة قرار المستوى الأول",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    Decision = "{{ Variables.ApprovalDecision }}",
                    ApproverId = "{{ Variables.Level1ApproverId }}",
                    Comments = "{{ Variables.ApproverComments }}",
                    CurrentApprovalLevel = 1
                }
            },
            new
            {
                activityId = "send_level2_approval",
                type = ApprovalActivityConstants.SendApprovalRequest,
                displayName = "موافقة المستوى الثاني",
                properties = new
                {
                    ApproverId = "{{ Variables.Level2ApproverId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    TimeoutHours = 48,
                    CustomMessage = "طلب موافقة المستوى الثاني - مراجعة نهائية"
                }
            },
            new
            {
                activityId = "process_level2_decision",
                type = ApprovalActivityConstants.ProcessApprovalDecision,
                displayName = "معالجة قرار المستوى الثاني",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    Decision = "{{ Variables.ApprovalDecision }}",
                    ApproverId = "{{ Variables.Level2ApproverId }}",
                    Comments = "{{ Variables.ApproverComments }}",
                    CurrentApprovalLevel = 2
                }
            },
            new
            {
                activityId = "send_final_notification",
                type = ApprovalActivityConstants.SendNotification,
                displayName = "إشعار نهائي",
                properties = new
                {
                    NotificationType = "RequestCompleted",
                    Recipients = "[\"{{ Variables.RequesterId }}\", \"{{ Variables.Level1ApproverId }}\", \"{{ Variables.Level2ApproverId }}\"]",
                    Channels = "[\"Email\", \"InApp\"]",
                    Subject = "اكتمال معالجة الطلب",
                    Message = "تم الانتهاء من معالجة الطلب عبر جميع مستويات الموافقة",
                    Priority = "Normal"
                }
            },
            new
            {
                activityId = "complete_workflow",
                type = ApprovalActivityConstants.CompleteWorkflow,
                displayName = "إكمال سير العمل",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    FinalStatus = "{{ Variables.FinalStatus }}",
                    CompletionNotes = "تم إكمال سير العمل متعدد المستويات",
                    SendCompletionNotification = true
                }
            }
        },
        connections = new[]
        {
            new { sourceActivityId = "start_approval", targetActivityId = "evaluate_rules", outcome = ApprovalActivityConstants.Outcomes.RequiresApproval },
            new { sourceActivityId = "start_approval", targetActivityId = "complete_workflow", outcome = ApprovalActivityConstants.Outcomes.AutoApproved },
            new { sourceActivityId = "evaluate_rules", targetActivityId = "send_level1_approval", outcome = ApprovalActivityConstants.Outcomes.Matched },
            new { sourceActivityId = "send_level1_approval", targetActivityId = "process_level1_decision", outcome = ApprovalActivityConstants.Outcomes.Approved },
            new { sourceActivityId = "send_level1_approval", targetActivityId = "process_level1_decision", outcome = ApprovalActivityConstants.Outcomes.Rejected },
            new { sourceActivityId = "process_level1_decision", targetActivityId = "send_level2_approval", outcome = ApprovalActivityConstants.Outcomes.Success },
            new { sourceActivityId = "send_level2_approval", targetActivityId = "process_level2_decision", outcome = ApprovalActivityConstants.Outcomes.Approved },
            new { sourceActivityId = "send_level2_approval", targetActivityId = "process_level2_decision", outcome = ApprovalActivityConstants.Outcomes.Rejected },
            new { sourceActivityId = "process_level2_decision", targetActivityId = "send_final_notification", outcome = ApprovalActivityConstants.Outcomes.Success },
            new { sourceActivityId = "send_final_notification", targetActivityId = "complete_workflow", outcome = ApprovalActivityConstants.Outcomes.Sent }
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// قالب الموافقة السريعة
    /// مناسب للطلبات العاجلة التي تحتاج معالجة سريعة
    /// </summary>
    public static string ExpressApprovalTemplate => JsonSerializer.Serialize(new
    {
        name = "ExpressApprovalWorkflow",
        displayName = "سير عمل الموافقة السريعة",
        description = "سير عمل للموافقة السريعة على الطلبات العاجلة",
        version = 1,
        isLatest = true,
        isPublished = true,
        variables = new
        {
            WorkflowStartTime = "{{ NowUtc }}",
            CurrentApprovalLevel = 1,
            TimeoutHours = 4, // مهلة أقصر للطلبات العاجلة
            NotificationFrequency = 1 // إشعارات كل ساعة
        },
        activities = new[]
        {
            new
            {
                activityId = "start_approval",
                type = ApprovalActivityConstants.StartApprovalWorkflow,
                displayName = "بدء الموافقة السريعة",
                properties = new
                {
                    RequestTypeId = "{{ Variables.RequestTypeId }}",
                    TenantId = "{{ Variables.TenantId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    RequestData = "{{ Variables.RequestData }}"
                }
            },
            new
            {
                activityId = "send_urgent_notification",
                type = ApprovalActivityConstants.SendNotification,
                displayName = "إشعار عاجل",
                properties = new
                {
                    NotificationType = "UrgentApprovalRequired",
                    Recipients = "[\"{{ Variables.ApproverId }}\"]",
                    Channels = "[\"Email\", \"SMS\", \"InApp\", \"RealTime\"]",
                    Subject = "🚨 طلب موافقة عاجل",
                    Message = "يوجد طلب عاجل يحتاج موافقتك خلال 4 ساعات",
                    Priority = "Critical"
                }
            },
            new
            {
                activityId = "send_express_approval",
                type = ApprovalActivityConstants.SendApprovalRequest,
                displayName = "طلب الموافقة السريعة",
                properties = new
                {
                    ApproverId = "{{ Variables.ApproverId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    TimeoutHours = 4,
                    CustomMessage = "⚡ طلب عاجل - يرجى الموافقة خلال 4 ساعات"
                }
            },
            new
            {
                activityId = "process_express_decision",
                type = ApprovalActivityConstants.ProcessApprovalDecision,
                displayName = "معالجة القرار السريع",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    Decision = "{{ Variables.ApprovalDecision }}",
                    ApproverId = "{{ Variables.ApproverId }}",
                    Comments = "{{ Variables.ApproverComments }}",
                    CurrentApprovalLevel = 1
                }
            },
            new
            {
                activityId = "send_completion_notification",
                type = ApprovalActivityConstants.SendNotification,
                displayName = "إشعار الإكمال",
                properties = new
                {
                    NotificationType = "ExpressRequestCompleted",
                    Recipients = "[\"{{ Variables.RequesterId }}\", \"{{ Variables.ApproverId }}\"]",
                    Channels = "[\"Email\", \"InApp\", \"RealTime\"]",
                    Subject = "✅ تم إكمال الطلب العاجل",
                    Message = "تم الانتهاء من معالجة طلبك العاجل",
                    Priority = "High"
                }
            },
            new
            {
                activityId = "complete_express_workflow",
                type = ApprovalActivityConstants.CompleteWorkflow,
                displayName = "إكمال سير العمل السريع",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    FinalStatus = "{{ Variables.FinalStatus }}",
                    CompletionNotes = "تم إكمال سير العمل السريع",
                    SendCompletionNotification = true
                }
            }
        },
        connections = new[]
        {
            new { sourceActivityId = "start_approval", targetActivityId = "send_urgent_notification", outcome = ApprovalActivityConstants.Outcomes.RequiresApproval },
            new { sourceActivityId = "start_approval", targetActivityId = "complete_express_workflow", outcome = ApprovalActivityConstants.Outcomes.AutoApproved },
            new { sourceActivityId = "send_urgent_notification", targetActivityId = "send_express_approval", outcome = ApprovalActivityConstants.Outcomes.Sent },
            new { sourceActivityId = "send_express_approval", targetActivityId = "process_express_decision", outcome = ApprovalActivityConstants.Outcomes.Approved },
            new { sourceActivityId = "send_express_approval", targetActivityId = "process_express_decision", outcome = ApprovalActivityConstants.Outcomes.Rejected },
            new { sourceActivityId = "send_express_approval", targetActivityId = "process_express_decision", outcome = ApprovalActivityConstants.Outcomes.Timeout },
            new { sourceActivityId = "process_express_decision", targetActivityId = "send_completion_notification", outcome = ApprovalActivityConstants.Outcomes.Success },
            new { sourceActivityId = "send_completion_notification", targetActivityId = "complete_express_workflow", outcome = ApprovalActivityConstants.Outcomes.Sent }
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// قالب الموافقة التلقائية
    /// للطلبات التي تلبي شروط معينة للموافقة التلقائية
    /// </summary>
    public static string AutoApprovalTemplate => JsonSerializer.Serialize(new
    {
        name = "AutoApprovalWorkflow",
        displayName = "سير عمل الموافقة التلقائية",
        description = "سير عمل للموافقة التلقائية على الطلبات التي تلبي الشروط",
        version = 1,
        isLatest = true,
        isPublished = true,
        variables = new
        {
            WorkflowStartTime = "{{ NowUtc }}",
            AutoApprovalRules = "{{ Variables.AutoApprovalRules }}"
        },
        activities = new[]
        {
            new
            {
                activityId = "start_auto_approval",
                type = ApprovalActivityConstants.StartApprovalWorkflow,
                displayName = "بدء الموافقة التلقائية",
                properties = new
                {
                    RequestTypeId = "{{ Variables.RequestTypeId }}",
                    TenantId = "{{ Variables.TenantId }}",
                    RequestId = "{{ Variables.RequestId }}",
                    RequestData = "{{ Variables.RequestData }}"
                }
            },
            new
            {
                activityId = "evaluate_auto_approval_rules",
                type = ApprovalActivityConstants.EvaluateApprovalRules,
                displayName = "تقييم قوانين الموافقة التلقائية",
                properties = new
                {
                    EvaluationRules = "{{ Variables.AutoApprovalRules }}",
                    RequestData = "{{ Variables.RequestData }}"
                }
            },
            new
            {
                activityId = "send_auto_approval_notification",
                type = ApprovalActivityConstants.SendNotification,
                displayName = "إشعار الموافقة التلقائية",
                properties = new
                {
                    NotificationType = "AutoApprovalGranted",
                    Recipients = "[\"{{ Variables.RequesterId }}\"]",
                    Channels = "[\"Email\", \"InApp\"]",
                    Subject = "✅ تم الموافقة على طلبك تلقائياً",
                    Message = "تم الموافقة على طلبك تلقائياً بناءً على الشروط المحددة",
                    Priority = "Normal"
                }
            },
            new
            {
                activityId = "complete_auto_workflow",
                type = ApprovalActivityConstants.CompleteWorkflow,
                displayName = "إكمال سير العمل التلقائي",
                properties = new
                {
                    RequestId = "{{ Variables.RequestId }}",
                    FinalStatus = "Approved",
                    CompletionNotes = "تم الموافقة تلقائياً حسب القوانين المحددة",
                    SendCompletionNotification = false
                }
            }
        },
        connections = new[]
        {
            new { sourceActivityId = "start_auto_approval", targetActivityId = "evaluate_auto_approval_rules", outcome = ApprovalActivityConstants.Outcomes.RequiresApproval },
            new { sourceActivityId = "start_auto_approval", targetActivityId = "send_auto_approval_notification", outcome = ApprovalActivityConstants.Outcomes.AutoApproved },
            new { sourceActivityId = "evaluate_auto_approval_rules", targetActivityId = "send_auto_approval_notification", outcome = ApprovalActivityConstants.Outcomes.Matched },
            new { sourceActivityId = "send_auto_approval_notification", targetActivityId = "complete_auto_workflow", outcome = ApprovalActivityConstants.Outcomes.Sent }
        }
    }, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// الحصول على جميع القوالب المتاحة
    /// </summary>
    public static Dictionary<string, string> GetAllTemplates()
    {
        return new Dictionary<string, string>
        {
            ["BasicApproval"] = BasicApprovalTemplate,
            ["MultiLevelApproval"] = MultiLevelApprovalTemplate,
            ["ExpressApproval"] = ExpressApprovalTemplate,
            ["AutoApproval"] = AutoApprovalTemplate
        };
    }

    /// <summary>
    /// الحصول على قالب بالاسم
    /// </summary>
    public static string? GetTemplate(string templateName)
    {
        var templates = GetAllTemplates();
        return templates.TryGetValue(templateName, out var template) ? template : null;
    }

    /// <summary>
    /// الحصول على قائمة أسماء القوالب مع الأوصاف
    /// </summary>
    public static Dictionary<string, string> GetTemplateDescriptions()
    {
        return new Dictionary<string, string>
        {
            ["BasicApproval"] = "سير عمل أساسي للموافقة على الطلبات البسيطة - موافقة واحدة",
            ["MultiLevelApproval"] = "سير عمل متعدد المستويات للطلبات المعقدة - موافقات متعددة",
            ["ExpressApproval"] = "سير عمل سريع للطلبات العاجلة - مهلة قصيرة وإشعارات متكررة",
            ["AutoApproval"] = "سير عمل تلقائي للطلبات التي تلبي شروط الموافقة التلقائية"
        };
    }
}