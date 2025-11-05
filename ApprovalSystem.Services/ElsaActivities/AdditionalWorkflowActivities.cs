// =========================================================
// AdditionalWorkflowActivities.cs - مُصحح للـ Elsa v3 الصحيح
// إصلاح API أخطاء Elsa Framework v3
// =========================================================

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Elsa;
using Elsa.ActivityResults;
using Elsa.Abstractions.Models;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using System.Threading;

namespace ApprovalSystem.Services.ElsaActivities
{
    // =============== FIXED ACTIVITY CLASSES ===============
    
    /// <summary>
    /// Activity لإرسال الإشعارات
    /// </summary>
    public class SendNotificationActivity : Activity
    {
        [Input]
        public Input<string> Message { get; set; } = default!;
        
        [Input]
        public Input<string> Type { get; set; } = default!;
        
        [Input]
        public Input<string> UserId { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // الحصول على المدخلات بطريقة صحيحة
                var message = await context.EvaluateAsync(Message, context.CancellationToken);
                var notificationType = await context.EvaluateAsync(Type, context.CancellationToken);
                var userId = await context.EvaluateAsync(UserId, context.CancellationToken);

                // إرسال الإشعار
                await SendNotificationAsync(message, notificationType, userId, context);

                return Outcome("Sent");
            }
            catch (Exception ex)
            {
                var logger = context.GetRequiredService<ILogger<SendNotificationActivity>>();
                logger.LogError(ex, "فشل في إرسال الإشعار");
                return Fault(ex);
            }
        }

        private async Task SendNotificationAsync(string message, string type, string userId, ActivityExecutionContext context)
        {
            try
            {
                // استخدام Logger بدلاً من context.LogError
                var logger = context.GetRequiredService<ILogger<SendNotificationActivity>>();
                
                // الحصول على NotificationService
                var notificationService = context.GetService<INotificationService>();
                if (notificationService != null)
                {
                    await notificationService.SendNotificationAsync(userId, message, type);
                }
                else
                {
                    logger.LogWarning("NotificationService غير متوفر، تسجيل الرسالة فقط");
                }
            }
            catch (Exception ex)
            {
                var logger = context.GetRequiredService<ILogger<SendNotificationActivity>>();
                logger.LogError(ex, "فشل في إرسال الإشعار");
                throw;
            }
        }
    }

    /// <summary>
    /// Activity لمعالجة قرار الموافقة
    /// </summary>
    public class ProcessApprovalDecisionActivity : Activity
    {
        [Input]
        public Input<string> RequestId { get; set; } = default!;
        
        [Input]
        public Input<string> Decision { get; set; } = default!;
        
        [Input]
        public Input<string> Comments { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // الحصول على المدخلات
                var requestId = await context.EvaluateAsync(RequestId, context.CancellationToken);
                var decision = await context.EvaluateAsync(Decision, context.CancellationToken);
                var comments = await context.EvaluateAsync(Comments, context.CancellationToken);

                // معالجة القرار
                var approvalResult = await ProcessApprovalAsync(requestId, decision, comments, context);

                // إرجاع النتيجة بناءً على القرار
                if (approvalResult.IsApproved)
                {
                    return Outcome("approved");
                }
                else
                {
                    return Outcome("rejected");
                }
            }
            catch (Exception ex)
            {
                var logger = context.GetRequiredService<ILogger<ProcessApprovalDecisionActivity>>();
                logger.LogError(ex, "فشل في معالجة قرار الموافقة");
                return Fault(ex);
            }
        }

        private async Task<ApprovalResult> ProcessApprovalAsync(string requestId, string decision, string comments, ActivityExecutionContext context)
        {
            var approvalService = context.GetService<IApprovalService>();
            if (approvalService != null)
            {
                return await approvalService.ProcessApprovalAsync(requestId, decision, comments);
            }
            
            // Fallback: محاكاة المعالجة
            return new ApprovalResult
            {
                IsApproved = decision.Equals("approved", StringComparison.OrdinalIgnoreCase),
                RequestId = requestId,
                Decision = decision,
                Comments = comments,
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Activity لإتمام سير العمل
    /// </summary>
    public class CompleteWorkflowActivity : Activity
    {
        [Input]
        public Input<string> ResultMessage { get; set; } = default!;
        
        [Input]
        public Input<bool> IsSuccessful { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                var resultMessage = await context.EvaluateAsync(ResultMessage, context.CancellationToken);
                var isSuccessful = await context.EvaluateAsync(IsSuccessful, context.CancellationToken);

                // إنشاء النتيجة النهائية
                var finalResult = new
                {
                    Message = resultMessage,
                    IsSuccessful = isSuccessful,
                    CompletedAt = DateTime.UtcNow,
                    WorkflowInstanceId = context.WorkflowExecutionContext.WorkflowInstanceId
                };

                if (isSuccessful)
                {
                    return new CompletedResult
                    {
                        Output = finalResult
                    };
                }
                else
                {
                    return Fault(resultMessage);
                }
            }
            catch (Exception ex)
            {
                var logger = context.GetRequiredService<ILogger<CompleteWorkflowActivity>>();
                logger.LogError(ex, "فشل في إتمام سير العمل");
                return Fault(ex);
            }
        }
    }

    // =============== MODELS ===============
    
    public class ApprovalResult
    {
        public bool IsApproved { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string ProcessedBy { get; set; } = string.Empty;
    }

    // =============== SERVICE INTERFACES ===============
    
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string message, string type);
    }

    public interface IApprovalService
    {
        Task<ApprovalResult> ProcessApprovalAsync(string requestId, string decision, string comments);
    }
}

// =========================================================
// تعليمات التطبيق:
// =========================================================
//
// التغييرات المطبقة:
// 1. استخدام protected override بدلاً من public override
// 2. استخدام OnExecuteAsync بدلاً من ExecuteAsync
// 3. استخدام context.EvaluateAsync بدلاً من EvaluateAsync
// 4. استخدام Logger من context.GetRequiredService<ILogger<>>()
// 5. استخدام Outcome(), Fault(), CompletedResult APIs
// 6. استخدام context.WorkflowExecutionContext.WorkflowInstanceId
// 7. إزالة Properties غير الموجودة في Elsa v3
//
// =========================================================