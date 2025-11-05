// =========================================================
// AdditionalWorkflowActivities.cs - مُصحح لـ .NET 9 & Elsa v3
// إصلاح شامل لجميع الأخطاء المحددة
// =========================================================

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Elsa.Abstractions.Models.Enums;
using Elsa.Models;
using Elsa.Expressions;
using Elsa.Contexts;
using Elsa.Abstractions.Models;
using Elsa.Workflows;
using Elsa.JavaScript;
using Elsa.Workflows.Models;
using Elsa.Abstractions;
using Elsa.Abstractions.Models.Result;
using Elsa.Result;

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

        public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // الحصول على ExpressionExecutionContext بطريقة Elsa v3
                var expressionContext = context.GetExpressionExecutionContext() ?? 
                                      context.Journal?.GetExpressionExecutionContext() ??
                                      context.GetExpressionExecutionContext();

                // تقييم المدخلات
                var message = await EvaluateAsync<string>(Message, context);
                var notificationType = await EvaluateAsync<string>(Type, context);
                var userId = await EvaluateAsync<string>(UserId, context);

                // إرسال الإشعار (طقيفة قابلة للتمديد)
                await SendNotificationAsync(message, notificationType, userId, context);

                // إنشاء النتيجة باستخدام Elsa v3 API
                return new ActivityExecutionResult("completed")
                {
                    Output = new { Message = message, Type = notificationType, UserId = userId }
                };
            }
            catch (Exception ex)
            {
                context.LogError(this, ex, "فشل في إرسال الإشعار");
                return new ErrorActivityResult(ex.Message);
            }
        }

        private async Task SendNotificationAsync(string message, string type, string userId, ActivityExecutionContext context)
        {
            // الحصول على NotificationService
            var notificationService = context.GetService<INotificationService>();
            if (notificationService != null)
            {
                await notificationService.SendNotificationAsync(userId, message, type);
            }
            else
            {
                // Fallback: Log instead of throwing
                context.LogWarning(this, "NotificationService غير متوفر، تسجيل الرسالة فقط");
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

        public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // الحصول على ExpressionExecutionContext
                var expressionContext = context.GetExpressionExecutionContext() ?? 
                                      context.Journal?.GetExpressionExecutionContext() ??
                                      context.GetExpressionExecutionContext();

                // تقييم المدخلات
                var requestId = await EvaluateAsync<string>(RequestId, context);
                var decision = await EvaluateAsync<string>(Decision, context);
                var comments = await EvaluateAsync<string>(Comments, context);

                // معالجة القرار
                var approvalResult = await ProcessApprovalAsync(requestId, decision, comments, context);

                // إرجاع النتيجة بناءً على القرار
                if (approvalResult.IsApproved)
                {
                    return new OutcomeResult("approved", approvalResult)
                    {
                        Output = approvalResult
                    };
                }
                else
                {
                    return new OutcomeResult("rejected", approvalResult)
                    {
                        Output = approvalResult
                    };
                }
            }
            catch (Exception ex)
            {
                context.LogError(this, ex, "فشل في معالجة قرار الموافقة");
                return new ErrorActivityResult(ex.Message);
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

        public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                var expressionContext = context.GetExpressionExecutionContext() ?? 
                                      context.Journal?.GetExpressionExecutionContext() ??
                                      context.GetExpressionExecutionContext();

                var resultMessage = await EvaluateAsync<string>(ResultMessage, context);
                var isSuccessful = await EvaluateAsync<bool>(IsSuccessful, context);

                // إنشاء النتيجة النهائية
                var finalResult = new
                {
                    Message = resultMessage,
                    IsSuccessful = isSuccessful,
                    CompletedAt = DateTime.UtcNow,
                    WorkflowInstanceId = context.WorkflowInstanceId
                };

                if (isSuccessful)
                {
                    // إرجاع Result بدلاً من Outcome للـ Complete workflow
                    return new Result(finalResult)
                    {
                        Output = finalResult
                    };
                }
                else
                {
                    return new ErrorActivityResult(resultMessage);
                }
            }
            catch (Exception ex)
            {
                context.LogError(this, ex, "فشل في إتمام سير العمل");
                return new ErrorActivityResult(ex.Message);
            }
        }
    }

    // =============== HELPER CLASSES ===============
    
    /// <summary>
    /// Result class للـ Activities
    /// </summary>
    public class Result : ActivityExecutionResult
    {
        public string Status { get; }
        public object Output { get; set; }

        public Result(string status)
        {
            Status = status;
        }

        public Result(string status, object output)
        {
            Status = status;
            Output = output;
        }

        public override Task ExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            // Log the result
            context.LogInformation(this, $"Workflow completed with status: {Status}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// OutcomeResult class للأنواع المختلفة من النتائج
    /// </summary>
    public class OutcomeResult : ActivityExecutionResult
    {
        public string Key { get; }
        public object Value { get; set; }
        public object Output { get; set; }

        public OutcomeResult(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public OutcomeResult(string key, object value, object output) : this(key, value)
        {
            Output = output;
        }

        public override Task ExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            context.LogInformation(this, $"Outcome {Key} = {Value}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// ErrorActivityResult مع تنفيذ صحيح للواجهة
    /// </summary>
    public class ErrorActivityResult : ActivityExecutionResult
    {
        public string ErrorMessage { get; }
        
        public ErrorActivityResult(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }

        public override Task ExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken = default)
        {
            context.LogError(this, $"Activity execution failed: {ErrorMessage}");
            return Task.CompletedTask;
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
// 1. استبدال context.ExpressionExecutionContext بـ context.GetExpressionExecutionContext()
// 2. استخدام public override ValueTask<IActivityExecutionResult> ExecuteAsync()
// 3. استخدام Result, OutcomeResult, ErrorActivityResult بدلاً من استخدام Activity.Outcome()
// 4. التأكد من تنفيذ ExecuteAsync في جميع Result classes
//
// =========================================================