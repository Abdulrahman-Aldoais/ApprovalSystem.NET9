using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace ApprovalSystem.Services.ElsaActivities
{
    /// <summary>
    /// SendNotificationActivity - يرسل إشعار للمستخدم
    /// </summary>
    [Activity(
        Category = "Custom",
        DisplayName = "Send Notification",
        Description = "يرسل إشعار للمستخدم",
        Icon = "fa-bell",
        Outcomes = new[] { OutcomeNames.Done, "Failed" }
    )]
    public class SendNotificationActivity : Activity
    {
        /// <summary>
        /// نص الإشعار - يمكن أن يكون تعبير (Expression)
        /// </summary>
        [Input(
            Name = "Message",
            DisplayName = "رسالة الإشعار",
            Description = "نص الرسالة المراد إرسالها",
            DefaultSyntax = SyntaxNames.JavaScript,
            DefaultValue = "'Hello from workflow!'"
        )]
        public Input<string> Message { get; set; } = default!;

        /// <summary>
        /// معرف المستخدم المراد إرسال الإشعار إليه
        /// </summary>
        [Input(
            Name = "UserId",
            DisplayName = "معرف المستخدم",
            Description = "معرف المستخدم الذي سيستقبل الإشعار",
            DefaultSyntax = SyntaxNames.Literal,
            DefaultValue = ""
        )]
        public Input<string> UserId { get; set; } = default!;

        /// <summary>
        /// نوع الإشعار
        /// </summary>
        [Input(
            Name = "NotificationType",
            DisplayName = "نوع الإشعار",
            Description = "نوع الإشعار المراد إرساله",
            DefaultSyntax = SyntaxNames.Literal,
            DefaultValue = "info"
        )]
        public Input<string> NotificationType { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // الحصول على القيم المُقيّمة باستخدام Elsa v3 API الصحيح
                var message = Message.Get(context);
                var userId = UserId.Get(context);
                var notificationType = NotificationType.Get(context);

                // الحصول على الخدمات المطلوبة
                var hubContext = context.GetRequiredService<IHubContext<ApprovalHub>>();
                var logger = context.GetRequiredService<ILogger<SendNotificationActivity>>();

                // إعداد بيانات الإشعار
                var notification = new
                {
                    Message = message,
                    Type = notificationType,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId
                };

                // إرسال الإشعار
                if (!string.IsNullOrEmpty(userId))
                {
                    await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
                    logger.LogInformation("تم إرسال إشعار للمستخدم {UserId}: {Message}", userId, message);
                }
                else
                {
                    await hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
                    logger.LogInformation("تم إرسال إشعار عام: {Message}", message);
                }

                // إرجاع النتيجة باستخدام Elsa v3 API
                return Outcome("Done");
            }
            catch (Exception ex)
            {
                var logger = context.GetRequiredService<ILogger<SendNotificationActivity>>();
                logger.LogError(ex, "فشل في إرسال الإشعار: {Message}", ex.Message);
                return Fault(ex.Message);
            }
        }
    }

    /// <summary>
    /// ProcessApprovalDecisionActivity - يعالج قرار الموافقة
    /// </summary>
    [Activity(
        Category = "Custom",
        DisplayName = "Process Approval Decision",
        Description = "يعالج قرار الموافقة على الطلب",
        Icon = "fa-check-circle",
        Outcomes = new[] { OutcomeNames.Done, "Rejected", "Failed" }
    )]
    public class ProcessApprovalDecisionActivity : Activity
    {
        /// <summary>
        /// معرف الطلب
        /// </summary>
        [Input(
            Name = "RequestId",
            DisplayName = "معرف الطلب",
            Description = "معرف الطلب المراد معالجة قراره",
            DefaultSyntax = SyntaxNames.Literal,
            DefaultValue = ""
        )]
        public Input<string> RequestId { get; set; } = default!;

        /// <summary>
        /// قرار الموافقة (approved/rejected/pending)
        /// </summary>
        [Input(
            Name = "Decision",
            DisplayName = "قرار الموافقة",
            Description = "قرار الموافقة على الطلب",
            DefaultSyntax = SyntaxNames.Literal,
            DefaultValue = "pending"
        )]
        public Input<string> Decision { get; set; } = default!;

        /// <summary>
        /// ملاحظات المراجع
        /// </summary>
        [Input(
            Name = "ReviewerNotes",
            DisplayName = "ملاحظات المراجع",
            Description = "ملاحظات المراجع على الطلب",
            DefaultSyntax = SyntaxNames.JavaScript,
            DefaultValue = "''"
        )]
        public Input<string> ReviewerNotes { get; set; } = default!;

        /// <summary>
        /// معرف المراجع
        /// </summary>
        [Input(
            Name = "ReviewerId",
            DisplayName = "معرف المراجع",
            Description = "معرف المراجع الذي اتخذ القرار",
            DefaultSyntax = SyntaxNames.Literal,
            DefaultValue = ""
        )]
        public Input<string> ReviewerId { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // الحصول على القيم المُقيّمة
                var requestId = RequestId.Get(context);
                var decision = Decision.Get(context);
                var reviewerNotes = ReviewerNotes.Get(context);
                var reviewerId = ReviewerId.Get(context);

                // الحصول على الخدمات
                var logger = context.GetRequiredService<ILogger<ProcessApprovalDecisionActivity>>();
                var hubContext = context.GetRequiredService<IHubContext<ApprovalHub>>();

                // الحصول على workflow instance ID
                var workflowInstanceId = context.WorkflowExecutionContext.WorkflowInstanceId;

                logger.LogInformation(
                    "معالجة قرار موافقة - الطلب: {RequestId}, القرار: {Decision}, المراجع: {ReviewerId}, InstanceId: {WorkflowInstanceId}",
                    requestId, decision, reviewerId, workflowInstanceId);

                // تحديث قاعدة البيانات (يُفترض أن تكون البيانات متاحة)
                var approvalUpdate = new
                {
                    RequestId = requestId,
                    Decision = decision.ToLowerInvariant(),
                    ReviewerNotes = reviewerNotes,
                    ReviewerId = reviewerId,
                    WorkflowInstanceId = workflowInstanceId,
                    UpdatedAt = DateTime.UtcNow
                };

                // إرسال إشعار للمستخدم
                await hubContext.Clients.All.SendAsync("ApprovalStatusUpdated", approvalUpdate);

                // إرجاع النتيجة بناءً على القرار
                var outcome = decision.ToLowerInvariant() switch
                {
                    "approved" => OutcomeNames.Done,
                    "rejected" => "Rejected",
                    _ => OutcomeNames.Done
                };

                logger.LogInformation("تم معالجة قرار الموافقة بنجاح: {Outcome}", outcome);
                return Outcome(outcome);
            }
            catch (Exception ex)
            {
                var logger = context.GetRequiredService<ILogger<ProcessApprovalDecisionActivity>>();
                logger.LogError(ex, "فشل في معالجة قرار الموافقة: {Message}", ex.Message);
                return Fault(ex.Message);
            }
        }
    }

    /// <summary>
    /// CompleteWorkflowActivity - يكمل سير العمل
    /// </summary>
    [Activity(
        Category = "Custom",
        DisplayName = "Complete Workflow",
        Description = "يكمل سير العمل ويرسل رسالة نهائية",
        Icon = "fa-flag-checkered",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class CompleteWorkflowActivity : Activity
    {
        /// <summary>
        /// رسالة الإنجاز
        /// </summary>
        [Input(
            Name = "CompletionMessage",
            DisplayName = "رسالة الإنجاز",
            Description = "رسالة تُرسل عند إكمال سير العمل",
            DefaultSyntax = SyntaxNames.JavaScript,
            DefaultValue = "'تم إنجاز سير العمل بنجاح'"
        )]
        public Input<string> CompletionMessage { get; set; } = default!;

        /// <summary>
        /// نوع الإنجاز
        /// </summary>
        [Input(
            Name = "CompletionType",
            DisplayName = "نوع الإنجاز",
            Description = "نوع إنجاز سير العمل",
            DefaultSyntax = SyntaxNames.Literal,
            DefaultValue = "success"
        )]
        public Input<string> CompletionType { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // الحصول على القيم المُقيّمة
                var completionMessage = CompletionMessage.Get(context);
                var completionType = CompletionType.Get(context);

                // الحصول على الخدمات
                var logger = context.GetRequiredService<ILogger<CompleteWorkflowActivity>>();
                var hubContext = context.GetRequiredService<IHubContext<ApprovalHub>>();

                // الحصول على معرف سير العمل
                var workflowInstanceId = context.WorkflowExecutionContext.WorkflowInstanceId;

                var completionData = new
                {
                    Message = completionMessage,
                    Type = completionType,
                    WorkflowInstanceId = workflowInstanceId,
                    CompletedAt = DateTime.UtcNow
                };

                // إرسال رسالة الإكمال
                await hubContext.Clients.All.SendAsync("WorkflowCompleted", completionData);

                logger.LogInformation(
                    "تم إنجاز سير العمل بنجاح - InstanceId: {WorkflowInstanceId}, الرسالة: {Message}",
                    workflowInstanceId, completionMessage);

                // إرجاع نتيجة الإنجاز
                return Outcome("Done");
            }
            catch (Exception ex)
            {
                var logger = context.GetRequiredService<ILogger<CompleteWorkflowActivity>>();
                logger.LogError(ex, "فشل في إكمال سير العمل: {Message}", ex.Message);
                return Fault(ex.Message);
            }
        }
    }
}