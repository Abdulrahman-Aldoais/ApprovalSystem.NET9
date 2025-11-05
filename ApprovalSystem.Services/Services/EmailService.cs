using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using ApprovalSystem.Core.Interfaces;
using ApprovalSystem.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApprovalSystem.Services.Services;

/// <summary>
/// خدمة البريد الإلكتروني
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpClient _smtpClient;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Initialize SMTP settings
        _smtpClient = new SmtpClient
        {
            Host = _configuration["Email:SmtpHost"] ?? "localhost",
            Port = int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
            EnableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true),
            Credentials = new NetworkCredential(
                _configuration["Email:Username"],
                _configuration["Email:Password"]
            ),
            Timeout = 30000 // 30 seconds
        };

        _fromAddress = _configuration["Email:FromAddress"] ?? "noreply@approvalsystem.com";
        _fromName = _configuration["Email:FromName"] ?? "نظام الموافقات";
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null, List<string>? attachments = null)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(to);

            if (!string.IsNullOrEmpty(textBody))
            {
                // Create alternate text body
                var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                message.AlternateViews.Add(textView);
            }

            // Add attachments
            if (attachments != null && attachments.Any())
            {
                foreach (var attachmentPath in attachments)
                {
                    if (System.IO.File.Exists(attachmentPath))
                    {
                        var attachment = new Attachment(attachmentPath);
                        message.Attachments.Add(attachment);
                    }
                }
            }

            await _smtpClient.SendMailAsync(message);

            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
            
            // Log email
            await LogEmailAsync(to, subject, "Sent", null, htmlBody.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To} with subject: {Subject}", to, subject);
            
            // Log failed email
            await LogEmailAsync(to, subject, "Failed", ex.Message, htmlBody.Length);

            return false;
        }
    }

    public async Task<bool> SendHtmlEmailAsync(string to, string subject, string htmlBody, string? fromName = null)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, fromName ?? _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await _smtpClient.SendMailAsync(message);

            _logger.LogInformation("HTML email sent successfully to {To} with subject: {Subject}", to, subject);
            
            await LogEmailAsync(to, subject, "Sent", null, htmlBody.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending HTML email to {To} with subject: {Subject}", to, subject);
            
            await LogEmailAsync(to, subject, "Failed", ex.Message, htmlBody.Length);

            return false;
        }
    }

    public async Task<bool> SendTextEmailAsync(string to, string subject, string textBody)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = subject,
                Body = textBody,
                IsBodyHtml = false
            };

            message.To.Add(to);

            await _smtpClient.SendMailAsync(message);

            _logger.LogInformation("Text email sent successfully to {To} with subject: {Subject}", to, subject);
            
            await LogEmailAsync(to, subject, "Sent", null, textBody.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text email to {To} with subject: {Subject}", to, subject);
            
            await LogEmailAsync(to, subject, "Failed", ex.Message, textBody.Length);

            return false;
        }
    }

    public async Task<bool> SendBulkEmailsAsync(List<EmailMessage> emails)
    {
        try
        {
            var results = new List<bool>();
            const int batchSize = 10;

            // Process emails in batches to avoid overwhelming the SMTP server
            for (int i = 0; i < emails.Count; i += batchSize)
            {
                var batch = emails.Skip(i).Take(batchSize).ToList();
                var batchTasks = batch.Select(SendSingleEmailAsync);
                
                var batchResults = await Task.WhenAll(batchTasks);
                results.AddRange(batchResults);

                // Small delay between batches
                if (i + batchSize < emails.Count)
                {
                    await Task.Delay(1000);
                }
            }

            var successCount = results.Count(r => r);
            _logger.LogInformation("Bulk email sending completed: {Success}/{Total} emails sent successfully", 
                successCount, emails.Count);

            return successCount == emails.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk emails");
            return false;
        }
    }

    public async Task<bool> SendTestEmailAsync(string to, string subject, string message)
    {
        var htmlBody = $@"
            <html>
                <head>
                    <meta charset='utf-8'>
                    <title>إشعار تجريبي</title>
                </head>
                <body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                        <h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>إشعار تجريبي</h2>
                        <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p><strong>رسالة:</strong></p>
                            <p>{message}</p>
                        </div>
                        <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                            <p>تم إرسال هذا الإشعار في: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
                            <p>نظام إدارة الموافقات</p>
                        </div>
                    </div>
                </body>
            </html>";

        return await SendHtmlEmailAsync(to, subject, htmlBody);
    }

    public async Task<bool> SendApprovalEmailAsync(Request request, Approval approval, string action)
    {
        try
        {
            var requester = await GetUserEmailAsync(request.RequesterId);
            if (string.IsNullOrEmpty(requester)) return false;

            var approver = await GetUserEmailAsync(approval.ApproverId);
            if (string.IsNullOrEmpty(approver)) approver = requester;

            var actionText = action switch
            {
                "Approved" => "تمت الموافقة على",
                "Rejected" => "تم رفض",
                "Escalated" => "تم تصعيد",
                _ => "تم تحديث حالة"
            };

            var subject = $"{actionText} طلبك: {request.Title}";
            
            var htmlBody = await GenerateApprovalEmailTemplate(request, approval, action, actionText);

            return await SendHtmlEmailAsync(requester, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending approval email for request {RequestId}", request.Id);
            return false;
        }
    }

    public async Task<bool> SendRejectionEmailAsync(Request request, Approval approval, string reason)
    {
        try
        {
            var requester = await GetUserEmailAsync(request.RequesterId);
            if (string.IsNullOrEmpty(requester)) return false;

            var subject = $"تم رفض طلبك: {request.Title}";
            
            var htmlBody = $@"
                <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>رفض طلب</title>
                    </head>
                    <body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #e74c3c; border-bottom: 2px solid #e74c3c; padding-bottom: 10px;'>تم رفض طلبك</h2>
                            
                            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h3>تفاصيل الطلب:</h3>
                                <p><strong>عنوان الطلب:</strong> {request.Title}</p>
                                <p><strong>الوصف:</strong> {request.Description}</p>
                                <p><strong>تاريخ الإنشاء:</strong> {request.CreatedAt:yyyy-MM-dd HH:mm}</p>
                            </div>

                            <div style='background: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-right: 4px solid #ffc107;'>
                                <h3 style='color: #856404; margin-top: 0;'>سبب الرفض:</h3>
                                <p>{reason}</p>
                            </div>

                            <div style='background: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0; border-right: 4px solid #17a2b8;'>
                                <h3 style='color: #0c5460; margin-top: 0;'>خطوات تالية:</h3>
                                <ul>
                                    <li>يمكنك مراجعة ملاحظات الموافق وتطبيق التصحيحات المطلوبة</li>
                                    <li>قم بإنشاء طلب جديد بعد إجراء التعديلات اللازمة</li>
                                    <li>استشر المشرف أو قسم الدعم إذا كنت بحاجة لمساعدة</li>
                                </ul>
                            </div>

                            <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                                <p>تم إرسال هذا الإشعار في: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
                                <p>نظام إدارة الموافقات</p>
                            </div>
                        </div>
                    </body>
                </html>";

            return await SendHtmlEmailAsync(requester, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending rejection email for request {RequestId}", request.Id);
            return false;
        }
    }

    public async Task<bool> SendEscalationEmailAsync(Request request, ApprovalEscalation escalation)
    {
        try
        {
            var escalatedTo = await GetUserEmailAsync(escalation.EscalatedToUserId);
            var escalatedBy = await GetUserEmailAsync(escalation.EscalatedByUserId);
            var requester = await GetUserEmailAsync(request.RequesterId);

            var subject = $"طلب تم تصعيده: {request.Title}";
            
            var htmlBody = $@"
                <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>تصعيد طلب</title>
                    </head>
                    <body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #f39c12; border-bottom: 2px solid #f39c12; padding-bottom: 10px;'>تم تصعيد طلب</h2>
                            
                            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h3>تفاصيل الطلب:</h3>
                                <p><strong>عنوان الطلب:</strong> {request.Title}</p>
                                <p><strong>الوصف:</strong> {request.Description}</p>
                                <p><strong>الأولوية:</strong> {request.Priority}</p>
                                <p><strong>تاريخ الإنشاء:</strong> {request.CreatedAt:yyyy-MM-dd HH:mm}</p>
                            </div>

                            <div style='background: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-right: 4px solid #ffc107;'>
                                <h3 style='color: #856404; margin-top: 0;'>تفاصيل التصعيد:</h3>
                                <p><strong>سبب التصعيد:</strong> {escalation.Reason}</p>
                                <p><strong>تم التصعيد بواسطة:</strong> {escalatedBy ?? "غير محدد"}</p>
                                <p><strong>وقت التصعيد:</strong> {escalation.TriggeredAt:yyyy-MM-dd HH:mm}</p>
                            </div>

                            <div style='background: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-right: 4px solid #28a745;'>
                                <h3 style='color: #155724; margin-top: 0;'>الإجراء المطلوب:</h3>
                                <ul>
                                    <li>مراجعة الطلب والتصعيد بأسرع وقت ممكن</li>
                                    <li>اتخاذ القرار المناسب بناءً على المعلومات المتاحة</li>
                                    <li>إضافة تعليق يوضح القرار والأسباب</li>
                                </ul>
                            </div>

                            <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                                <p>تم إرسال هذا الإشعار في: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
                                <p>نظام إدارة الموافقات</p>
                            </div>
                        </div>
                    </body>
                </html>";

            var result1 = await SendHtmlEmailAsync(escalatedTo ?? "", subject, htmlBody);
            var result2 = await SendHtmlEmailAsync(requester ?? "", subject, htmlBody);

            return result1 && result2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending escalation email for request {RequestId}", request.Id);
            return false;
        }
    }

    public async Task<bool> SendApprovalReminderEmailAsync(Approval approval)
    {
        try
        {
            var approver = await GetUserEmailAsync(approval.ApproverId);
            if (string.IsNullOrEmpty(approver)) return false;

            var subject = $"تذكير بموافقة معلقة: {approval.Request?.Title ?? "طلب"}";
            
            var htmlBody = $@"
                <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>تذكير بموافقة</title>
                    </head>
                    <body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #e74c3c; border-bottom: 2px solid #e74c3c; padding-bottom: 10px;'>تذكير بموافقة معلقة</h2>
                            
                            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p><strong>لديك موافقة معلقة تحتاج لمراجعة:</strong></p>
                                <p><strong>عنوان الطلب:</strong> {approval.Request?.Title}</p>
                                <p><strong>الوصف:</strong> {approval.Request?.Description}</p>
                                <p><strong>تاريخ الإنشاء:</strong> {approval.Request?.CreatedAt:yyyy-MM-dd HH:mm}</p>
                                <p><strong>الأولوية:</strong> {approval.Request?.Priority}</p>
                            </div>";

            if (approval.Request?.DueDate.HasValue == true)
            {
                var daysLeft = (approval.Request.DueDate.Value - DateTime.UtcNow).Days;
                htmlBody += $@"
                            <div style='background: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border-right: 4px solid #ffc107;'>
                                <h3 style='color: #856404; margin-top: 0;'>الموعد النهائي:</h3>
                                <p><strong>الوقت المتبقي:</strong> {daysLeft} يوم</p>
                                <p><strong>الموعد النهائي:</strong> {approval.Request.DueDate:yyyy-MM-dd HH:mm}</p>
                            </div>";
            }

            htmlBody += $@"
                            <div style='background: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-right: 4px solid #28a745;'>
                                <h3 style='color: #155724; margin-top: 0;'>الإجراء المطلوب:</h3>
                                <p>يرجى مراجعة الطلب واتخاذ الإجراء المناسب (موافقة أو رفض) في أقرب وقت ممكن.</p>
                            </div>

                            <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                                <p>تم إرسال هذا التذكير في: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
                                <p>نظام إدارة الموافقات</p>
                            </div>
                        </div>
                    </body>
                </html>";

            return await SendHtmlEmailAsync(approver, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending approval reminder email for approval {ApprovalId}", approval.Id);
            return false;
        }
    }

    public async Task<bool> SendSystemEmailAsync(string subject, string message, List<string> recipients)
    {
        try
        {
            var htmlBody = $@"
                <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>إشعار نظام</title>
                    </head>
                    <body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #6c757d; border-bottom: 2px solid #6c757d; padding-bottom: 10px;'>إشعار نظام</h2>
                            
                            <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p>{message}</p>
                            </div>

                            <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                                <p>تم إرسال هذا الإشعار في: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
                                <p>نظام إدارة الموافقات</p>
                            </div>
                        </div>
                    </body>
                </html>";

            var emailMessages = recipients.Select(recipient => new EmailMessage
            {
                To = recipient,
                Subject = subject,
                HtmlBody = htmlBody
            }).ToList();

            return await SendBulkEmailsAsync(emailMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system email");
            return false;
        }
    }

    public async Task<bool> SendBulkNotificationsAsync(List<Notification> notifications)
    {
        try
        {
            var emailMessages = new List<EmailMessage>();

            foreach (var notification in notifications)
            {
                var recipient = await GetUserEmailAsync(notification.UserId);
                if (string.IsNullOrEmpty(recipient)) continue;

                var htmlBody = $@"
                    <html>
                        <head>
                            <meta charset='utf-8'>
                            <title>{notification.Title}</title>
                        </head>
                        <body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                                <h2 style='color: #007bff; border-bottom: 2px solid #007bff; padding-bottom: 10px;'>{notification.Title}</h2>
                                
                                <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                    <p>{notification.Message}</p>
                                </div>

                                <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                                    <p>تم إرسال هذا الإشعار في: {notification.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}</p>
                                    <p>نظام إدارة الموافقات</p>
                                </div>
                            </div>
                        </body>
                    </html>";

                emailMessages.Add(new EmailMessage
                {
                    To = recipient,
                    Subject = notification.Title,
                    HtmlBody = htmlBody,
                    Priority = notification.Priority
                });
            }

            return await SendBulkEmailsAsync(emailMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications");
            return false;
        }
    }

    public async Task<bool> SendTestNotificationAsync(Notification notification)
    {
        try
        {
            var recipient = await GetUserEmailAsync(notification.UserId);
            if (string.IsNullOrEmpty(recipient)) return false;

            return await SendTestEmailAsync(recipient, notification.Title, notification.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return false;
        }
    }

    public async Task<bool> ValidateEmailSettingsAsync()
    {
        try
        {
            // Test SMTP connection
            using var testClient = new SmtpClient
            {
                Host = _configuration["Email:SmtpHost"] ?? "localhost",
                Port = int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
                EnableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true),
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]
                )
            };

            // Test with a simple message
            using var testMessage = new MailMessage
            {
                From = new MailAddress(_fromAddress),
                To = { _fromAddress },
                Subject = "Test Email",
                Body = "This is a test email to validate SMTP settings."
            };

            await testClient.SendMailAsync(testMessage);

            _logger.LogInformation("Email settings validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email settings validation failed");
            return false;
        }
    }

    public async Task<EmailServiceStatus> GetServiceStatusAsync()
    {
        try
        {
            var isHealthy = await ValidateEmailSettingsAsync();
            
            return new EmailServiceStatus
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                LastCheckTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                FailedEmailsCount = 0, // TODO: Get from email logs
                PendingEmailsCount = 0, // TODO: Get from email logs
                SentEmailsToday = 0 // TODO: Get from email logs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email service status");
            return new EmailServiceStatus
            {
                IsHealthy = false,
                Status = "Error",
                LastCheckTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<List<EmailLog>> GetEmailLogsAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 50)
    {
        try
        {
            // TODO: Implement actual email logging to database
            // For now, return empty list
            return new List<EmailLog>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email logs");
            return new List<EmailLog>();
        }
    }

    public async Task<bool> RetryFailedEmailAsync(Guid emailId)
    {
        try
        {
            // TODO: Implement retry logic for failed emails
            _logger.LogInformation("Retrying failed email {EmailId}", emailId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed email {EmailId}", emailId);
            return false;
        }
    }

    public async Task<bool> CancelPendingEmailAsync(Guid emailId)
    {
        try
        {
            // TODO: Implement cancellation logic for pending emails
            _logger.LogInformation("Cancelling pending email {EmailId}", emailId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling pending email {EmailId}", emailId);
            return false;
        }
    }

    private async Task<bool> SendSingleEmailAsync(EmailMessage email)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = email.Subject,
                Body = email.HtmlBody,
                IsBodyHtml = true
            };

            message.To.Add(email.To);

            if (!string.IsNullOrEmpty(email.Cc))
                message.CC.Add(email.Cc);

            if (!string.IsNullOrEmpty(email.Bcc))
                message.Bcc.Add(email.Bcc);

            if (!string.IsNullOrEmpty(email.TextBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(email.TextBody, null, "text/plain");
                message.AlternateViews.Add(textView);
            }

            await _smtpClient.SendMailAsync(message);

            _logger.LogDebug("Email sent successfully to {To} with subject: {Subject}", email.To, email.Subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To} with subject: {Subject}", email.To, email.Subject);
            return false;
        }
    }

    private async Task<string> GetUserEmailAsync(string userId)
    {
        try
        {
            // TODO: Get user email from database
            // For now, return a placeholder
            return "user@example.com";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email for user {UserId}", userId);
            return "";
        }
    }

    private async Task LogEmailAsync(string to, string subject, string status, string? errorMessage, int bodyLength)
    {
        try
        {
            // TODO: Implement email logging to database
            _logger.LogDebug("Email log - To: {To}, Subject: {Subject}, Status: {Status}, BodyLength: {BodyLength}, Error: {ErrorMessage}", 
                to, subject, status, bodyLength, errorMessage ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging email");
        }
    }

    private async Task<string> GenerateApprovalEmailTemplate(Request request, Approval approval, string action, string actionText)
    {
        return $@"
            <html>
                <head>
                    <meta charset='utf-8'>
                    <title>تحديث حالة الطلب</title>
                </head>
                <body style='font-family: Arial, sans-serif; direction: rtl; text-align: right;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                        <h2 style='color: #28a745; border-bottom: 2px solid #28a745; padding-bottom: 10px;'>{actionText} طلبك</h2>
                        
                        <div style='background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <h3>تفاصيل الطلب:</h3>
                            <p><strong>عنوان الطلب:</strong> {request.Title}</p>
                            <p><strong>الوصف:</strong> {request.Description}</p>
                            <p><strong>تاريخ الإنشاء:</strong> {request.CreatedAt:yyyy-MM-dd HH:mm}</p>
                            <p><strong>الأولوية:</strong> {request.Priority}</p>
                        </div>

                        <div style='background: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border-right: 4px solid #28a745;'>
                            <h3 style='color: #155724; margin-top: 0;'>تفاصيل الموافقة:</h3>
                            <p><strong>الإجراء:</strong> {action}</p>
                            <p><strong>تاريخ الموافقة:</strong> {approval.ApprovedAt:yyyy-MM-dd HH:mm}</p>
                            {(!string.IsNullOrEmpty(approval.Comments) ? $"<p><strong>التعليق:</strong> {approval.Comments}</p>" : "")}
                        </div>

                        <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666;'>
                            <p>تم إرسال هذا الإشعار في: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
                            <p>نظام إدارة الموافقات</p>
                        </div>
                    </div>
                </body>
            </html>";
    }
}