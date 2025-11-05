using ApprovalSystem.Models.DTOs;

namespace ApprovalSystem.Core.Interfaces;

/// <summary>
/// واجهة خدمة الـ workflows
/// </summary>
public interface IWorkflowService
{
    Task StartWorkflowAsync(string workflowType, object input = null, string tenantId = null);
    Task<object> GetWorkflowStatusAsync(string workflowInstanceId);
    Task CompleteWorkflowAsync(string workflowInstanceId, object result = null);
    Task CancelWorkflowAsync(string workflowInstanceId, string reason = null);
}

/// <summary>
/// واجهة خدمة الموافقات
/// </summary>
public interface IApprovalService
{
    Task<ApprovalDecisionDto> CreateApprovalAsync(ApprovalRequestDto request);
    Task<ApprovalDecisionDto> ProcessApprovalAsync(Guid approvalId, string decision, string comments = null);
    Task<ApprovalDecisionDto?> GetApprovalAsync(Guid approvalId);
    Task<List<ApprovalDecisionDto>> GetPendingApprovalsAsync(string userId);
    Task<bool> EscalateApprovalAsync(Guid approvalId, string reason, string escalatedTo);
}

/// <summary>
/// واجهة خدمة الطلبات
/// </summary>
public interface IRequestService
{
    Task<RequestDto> CreateRequestAsync(CreateRequestDto request);
    Task<RequestDto?> GetRequestAsync(Guid requestId);
    Task<List<RequestDto>> GetRequestsAsync(string userId, RequestStatus? status = null);
    Task<RequestDto> UpdateRequestStatusAsync(Guid requestId, RequestStatus status, string comments = null);
    Task<bool> CancelRequestAsync(Guid requestId, string reason);
}

/// <summary>
/// واجهة خدمة الإشعارات
/// </summary>
public interface INotificationService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, object data = null);
    Task<bool> SendSmsAsync(string to, string message, object data = null);
    Task<bool> SendInAppAsync(string userId, string message, object data = null);
    Task<bool> SendPushAsync(string userId, string title, string body, object data = null);
    Task<bool> SendNotificationAsync(string type, List<string> recipients, string message, object data = null);
}
