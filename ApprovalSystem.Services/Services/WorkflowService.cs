using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using ApprovalSystem.Core.Interfaces;
using ApprovalSystem.Infrastructure.Data;
using ApprovalSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApprovalSystem.Services.Services;

/// <summary>
/// خدمة مسارات العمل (Workflow Engine)
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowService> _logger;
    private readonly Timer? _cleanupTimer;

    public WorkflowService(ApplicationDbContext context, ILogger<WorkflowService> logger)
    {
        _context = context;
        _logger = logger;

        // Start cleanup timer (runs every hour)
        _cleanupTimer = new Timer(async _ => await CleanupCompletedWorkflowsInternalAsync(), null, 
            TimeSpan.Zero, TimeSpan.FromHours(1));
    }

    public async Task<string?> CreateWorkflowAsync(string workflowName, Guid tenantId, Dictionary<string, object> config)
    {
        try
        {
            var instanceId = Guid.NewGuid().ToString();

            // Create workflow metadata record
            var workflowMetadata = new WorkflowMetadata
            {
                Id = Guid.NewGuid(),
                RequestId = Guid.Parse(config.GetValueOrDefault("requestId", Guid.Empty).ToString()),
                WorkflowName = workflowName,
                TenantId = tenantId,
                Data = System.Text.Json.JsonSerializer.Serialize(config),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = config.GetValueOrDefault("createdBy", "system").ToString()
            };

            _context.WorkflowMetadata.Add(workflowMetadata);

            // Create workflow tracking record
            var workflowTracking = new WorkflowTracking
            {
                Id = Guid.NewGuid(),
                RequestId = Guid.Parse(config.GetValueOrDefault("requestId", Guid.Empty).ToString()),
                WorkflowInstanceId = instanceId,
                WorkflowName = workflowName,
                Status = "started",
                TenantId = tenantId,
                Data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    config = config,
                    startedAt = DateTime.UtcNow
                }),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = config.GetValueOrDefault("createdBy", "system").ToString()
            };

            _context.WorkflowTrackings.Add(workflowTracking);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created workflow {WorkflowName} with instance ID {InstanceId} for request {RequestId}", 
                workflowName, instanceId, config.GetValueOrDefault("requestId", ""));

            return instanceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow {WorkflowName} for tenant {TenantId}", workflowName, tenantId);
            return null;
        }
    }

    public async Task<string?> StartWorkflowAsync(Guid requestId, string workflowName, Guid tenantId)
    {
        try
        {
            var instanceId = Guid.NewGuid().ToString();

            // Get request details
            var request = await _context.Requests
                .Include(r => r.RequestType)
                .ThenInclude(rt => rt.ApprovalMatrix)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.TenantId == tenantId);

            if (request == null)
            {
                _logger.LogWarning("Request {RequestId} not found for workflow {WorkflowName}", requestId, workflowName);
                return null;
            }

            // Create workflow instance
            var workflowMetadata = new WorkflowMetadata
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,
                WorkflowName = workflowName,
                TenantId = tenantId,
                Data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    requestType = request.RequestType?.Name,
                    priority = request.Priority,
                    amount = request.Amount,
                    requesterId = request.RequesterId
                }),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.RequesterId
            };

            _context.WorkflowMetadata.Add(workflowMetadata);

            // Create initial workflow tracking
            var workflowTracking = new WorkflowTracking
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,
                WorkflowInstanceId = instanceId,
                WorkflowName = workflowName,
                Status = "running",
                TenantId = tenantId,
                Data = System.Text.Json.JsonSerializer.Serialize(new
                {
                    startedAt = DateTime.UtcNow,
                    status = "initialized"
                }),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.RequesterId
            };

            _context.WorkflowTrackings.Add(workflowTracking);

            await _context.SaveChangesAsync();

            // Execute workflow logic based on type
            await ExecuteWorkflowLogicAsync(request, instanceId, tenantId);

            _logger.LogInformation("Started workflow {WorkflowName} with instance ID {InstanceId} for request {RequestId}", 
                workflowName, instanceId, requestId);

            return instanceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow {WorkflowName} for request {RequestId}", workflowName, requestId);
            return null;
        }
    }

    public async Task<bool> StopWorkflowAsync(string workflowInstanceId)
    {
        try
        {
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            if (workflowTracking == null)
            {
                _logger.LogWarning("Workflow {InstanceId} not found", workflowInstanceId);
                return false;
            }

            workflowTracking.Status = "cancelled";
            workflowTracking.Data = System.Text.Json.JsonSerializer.Serialize(System.Text.Json.JsonSerializer.Deserialize<dynamic>(workflowTracking.Data ?? "{}") 
                with { cancelledAt = DateTime.UtcNow });
            workflowTracking.UpdatedAt = DateTime.UtcNow;
            workflowTracking.UpdatedBy = "user";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Stopped workflow {InstanceId}", workflowInstanceId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping workflow {InstanceId}", workflowInstanceId);
            return false;
        }
    }

    public async Task<bool> UpdateWorkflowStatusAsync(string workflowInstanceId, string status, Dictionary<string, object>? data = null)
    {
        try
        {
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            if (workflowTracking == null)
            {
                _logger.LogWarning("Workflow {InstanceId} not found", workflowInstanceId);
                return false;
            }

            var statusData = new Dictionary<string, object>
            {
                { "status", status },
                { "updatedAt", DateTime.UtcNow },
                { "data", data ?? new Dictionary<string, object>() }
            };

            workflowTracking.Status = status;
            workflowTracking.Data = System.Text.Json.JsonSerializer.Serialize(statusData);
            workflowTracking.UpdatedAt = DateTime.UtcNow;

            if (status == "completed" || status == "failed")
            {
                workflowTracking.UpdatedBy = "system";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated workflow {InstanceId} status to {Status}", workflowInstanceId, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {InstanceId} status", workflowInstanceId);
            return false;
        }
    }

    public async Task<WorkflowInstance?> GetWorkflowAsync(string workflowInstanceId)
    {
        try
        {
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            if (workflowTracking == null)
            {
                return null;
            }

            var workflowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(workflowTracking.Data ?? "{}") 
                ?? new Dictionary<string, object>();

            return new WorkflowInstance
            {
                InstanceId = workflowInstanceId,
                WorkflowName = workflowTracking.WorkflowName,
                Status = workflowTracking.Status,
                RequestId = workflowTracking.RequestId,
                TenantId = workflowTracking.TenantId,
                StartedAt = workflowTracking.CreatedAt,
                CompletedAt = workflowTracking.UpdatedAt,
                LastActivityAt = workflowTracking.UpdatedAt,
                Data = workflowData,
                Variables = new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {InstanceId}", workflowInstanceId);
            return null;
        }
    }

    public async Task<List<WorkflowStep>> GetWorkflowStepsAsync(string workflowInstanceId)
    {
        try
        {
            // Get workflow tracking to extract step information
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            if (workflowTracking == null)
            {
                return new List<WorkflowStep>();
            }

            var workflowData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(workflowTracking.Data ?? "{}") 
                ?? new Dictionary<string, object>();

            var steps = new List<WorkflowStep>();

            // Parse steps from workflow data if available
            if (workflowData.TryGetValue("steps", out var stepsData) && stepsData is not null)
            {
                var stepArray = System.Text.Json.JsonSerializer.Deserialize<object[]>(stepsData.ToString() ?? "[]") ?? Array.Empty<object>();
                
                foreach (var stepObj in stepArray)
                {
                    var stepDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(stepObj.ToString() ?? "{}") 
                        ?? new Dictionary<string, object>();

                    steps.Add(new WorkflowStep
                    {
                        StepId = stepDict.GetValueOrDefault("stepId", Guid.NewGuid().ToString()).ToString() ?? "",
                        StepName = stepDict.GetValueOrDefault("stepName", "").ToString() ?? "",
                        StepType = stepDict.GetValueOrDefault("stepType", "task").ToString() ?? "",
                        Status = stepDict.GetValueOrDefault("status", "pending").ToString() ?? "",
                        StartedAt = stepDict.TryGetValue("startedAt", out var startedAt) && DateTime.TryParse(startedAt?.ToString(), out var startTime) 
                            ? startTime : null,
                        CompletedAt = stepDict.TryGetValue("completedAt", out var completedAt) && DateTime.TryParse(completedAt?.ToString(), out var endTime) 
                            ? endTime : null,
                        ScheduledAt = stepDict.TryGetValue("scheduledAt", out var scheduledAt) && DateTime.TryParse(scheduledAt?.ToString(), out var scheduleTime) 
                            ? scheduleTime : null,
                        Parameters = stepDict.TryGetValue("parameters", out var parameters) 
                            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parameters?.ToString() ?? "{}") ?? new Dictionary<string, object>()
                            : new Dictionary<string, object>(),
                        Results = stepDict.TryGetValue("results", out var results) 
                            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(results?.ToString() ?? "{}") ?? new Dictionary<string, object>()
                            : new Dictionary<string, object>(),
                        ErrorMessage = stepDict.GetValueOrDefault("errorMessage", "").ToString(),
                        RetryCount = stepDict.TryGetValue("retryCount", out var retry) && int.TryParse(retry?.ToString(), out var retryCount) 
                            ? retryCount : 0,
                        NextStepId = stepDict.GetValueOrDefault("nextStepId", "").ToString()
                    });
                }
            }
            else
            {
                // Create default workflow steps based on approval process
                steps = CreateDefaultWorkflowSteps(workflowTracking);
            }

            return steps.OrderBy(s => s.StepId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow steps for {InstanceId}", workflowInstanceId);
            return new List<WorkflowStep>();
        }
    }

    public async Task<bool> ExecuteWorkflowStepAsync(string workflowInstanceId, string stepName, Dictionary<string, object>? parameters = null)
    {
        try
        {
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            if (workflowTracking == null)
            {
                _logger.LogWarning("Workflow {InstanceId} not found", workflowInstanceId);
                return false;
            }

            _logger.LogInformation("Executing step {StepName} in workflow {InstanceId}", stepName, workflowInstanceId);

            // Execute step based on type
            var success = stepName.ToLower() switch
            {
                "approval" => await ExecuteApprovalStepAsync(workflowTracking.RequestId, parameters),
                "notification" => await ExecuteNotificationStepAsync(workflowTracking.RequestId, parameters),
                "escalation" => await ExecuteEscalationStepAsync(workflowTracking.RequestId, parameters),
                "validation" => await ExecuteValidationStepAsync(workflowTracking.RequestId, parameters),
                "automation" => await ExecuteAutomationStepAsync(workflowTracking.RequestId, parameters),
                _ => await ExecuteGenericStepAsync(stepName, parameters)
            };

            if (success)
            {
                await UpdateWorkflowStatusAsync(workflowInstanceId, "running", new Dictionary<string, object>
                {
                    { "completedStep", stepName },
                    { "stepParameters", parameters ?? new Dictionary<string, object>() }
                });
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step {StepName} in workflow {InstanceId}", stepName, workflowInstanceId);
            return false;
        }
    }

    public async Task<List<string>> GetAvailableWorkflowsAsync(Guid tenantId)
    {
        try
        {
            // Return predefined workflows
            var workflows = new List<string>
            {
                "BasicApproval",
                "MultiLevelApproval",
                "FinancialApproval",
                "ProcurementApproval",
                "ITServiceApproval",
                "EscalationWorkflow"
            };

            _logger.LogInformation("Retrieved {Count} available workflows for tenant {TenantId}", workflows.Count, tenantId);

            return workflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available workflows for tenant {TenantId}", tenantId);
            return new List<string>();
        }
    }

    public async Task<string?> GetWorkflowStatusAsync(string workflowInstanceId)
    {
        try
        {
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            return workflowTracking?.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow status for {InstanceId}", workflowInstanceId);
            return null;
        }
    }

    public async Task<bool> RestartWorkflowAsync(string workflowInstanceId)
    {
        try
        {
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            if (workflowTracking == null)
            {
                _logger.LogWarning("Workflow {InstanceId} not found for restart", workflowInstanceId);
                return false;
            }

            workflowTracking.Status = "running";
            workflowTracking.Data = System.Text.Json.JsonSerializer.Serialize(new
            {
                restartedAt = DateTime.UtcNow,
                status = "initialized"
            });
            workflowTracking.UpdatedAt = DateTime.UtcNow;
            workflowTracking.UpdatedBy = "system";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Restarted workflow {InstanceId}", workflowInstanceId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting workflow {InstanceId}", workflowInstanceId);
            return false;
        }
    }

    public async Task<bool> SendWorkflowDataAsync(string workflowInstanceId, string dataType, object data)
    {
        try
        {
            var workflowTracking = await _context.WorkflowTrackings
                .FirstOrDefaultAsync(w => w.WorkflowInstanceId == workflowInstanceId);

            if (workflowTracking == null)
            {
                _logger.LogWarning("Workflow {InstanceId} not found for data update", workflowInstanceId);
                return false;
            }

            var currentData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(workflowTracking.Data ?? "{}") 
                ?? new Dictionary<string, object>();

            currentData[dataType] = data;
            currentData["lastDataUpdate"] = DateTime.UtcNow;

            workflowTracking.Data = System.Text.Json.JsonSerializer.Serialize(currentData);
            workflowTracking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Sent {DataType} data to workflow {InstanceId}", dataType, workflowInstanceId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {DataType} data to workflow {InstanceId}", dataType, workflowInstanceId);
            return false;
        }
    }

    public async Task<WorkflowStats> GetWorkflowStatsAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.WorkflowTrackings.Where(w => w.TenantId == tenantId);

            if (startDate.HasValue)
                query = query.Where(w => w.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(w => w.CreatedAt <= endDate.Value);

            var workflows = await query.ToListAsync();

            var stats = new WorkflowStats
            {
                TotalWorkflows = workflows.Count,
                ActiveWorkflows = workflows.Count(w => w.Status == "running" || w.Status == "started"),
                CompletedWorkflows = workflows.Count(w => w.Status == "completed"),
                FailedWorkflows = workflows.Count(w => w.Status == "failed"),
                WorkflowsByStatus = workflows.GroupBy(w => w.Status).ToDictionary(g => g.Key, g => g.Count()),
                FailedWorkflowsToday = workflows.Count(w => w.Status == "failed" && w.CreatedAt.Date == DateTime.UtcNow.Date)
            };

            // Calculate average execution time
            var completedWorkflows = workflows.Where(w => w.Status == "completed" && w.UpdatedAt.HasValue).ToList();
            if (completedWorkflows.Any())
            {
                stats.AverageExecutionTime = completedWorkflows
                    .Average(w => (w.UpdatedAt!.Value - w.CreatedAt).TotalHours);
            }

            // Count approval workflows specifically
            stats.WorkflowsByType = workflows.Count(w => w.WorkflowName.Contains("approval", StringComparison.OrdinalIgnoreCase));

            _logger.LogInformation("Retrieved workflow stats for tenant {TenantId}: {TotalWorkflows} total, {CompletedWorkflows} completed", 
                tenantId, stats.TotalWorkflows, stats.CompletedWorkflows);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<int> CleanupCompletedWorkflowsAsync(Guid tenantId, int daysToKeep = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            
            var workflowsToCleanup = await _context.WorkflowTrackings
                .Where(w => w.TenantId == tenantId && 
                           w.Status == "completed" && 
                           w.UpdatedAt < cutoffDate)
                .ToListAsync();

            _context.WorkflowTrackings.RemoveRange(workflowsToCleanup);

            // Also cleanup metadata
            var metadataToCleanup = await _context.WorkflowMetadata
                .Where(wm => wm.TenantId == tenantId && 
                            wm.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.WorkflowMetadata.RemoveRange(metadataToCleanup);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} completed workflows for tenant {TenantId}", 
                workflowsToCleanup.Count + metadataToCleanup.Count, tenantId);

            return workflowsToCleanup.Count + metadataToCleanup.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up completed workflows for tenant {TenantId}", tenantId);
            return 0;
        }
    }

    public async Task<List<WorkflowInstance>> GetDelayedWorkflowsAsync(Guid tenantId, int maxDelayHours = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-maxDelayHours);
            
            var delayedWorkflows = await _context.WorkflowTrackings
                .Include(w => w.Request)
                .Where(w => w.TenantId == tenantId && 
                           w.Status == "running" && 
                           w.UpdatedAt < cutoffTime)
                .ToListAsync();

            var result = delayedWorkflows.Select(w => new WorkflowInstance
            {
                InstanceId = w.WorkflowInstanceId,
                WorkflowName = w.WorkflowName,
                Status = w.Status,
                RequestId = w.RequestId,
                TenantId = w.TenantId,
                StartedAt = w.CreatedAt,
                LastActivityAt = w.UpdatedAt,
                Data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(w.Data ?? "{}") 
                    ?? new Dictionary<string, object>()
            }).ToList();

            _logger.LogInformation("Found {Count} delayed workflows for tenant {TenantId}", result.Count, tenantId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delayed workflows for tenant {TenantId}", tenantId);
            return new List<WorkflowInstance>();
        }
    }

    private async Task ExecuteWorkflowLogicAsync(Request request, string instanceId, Guid tenantId)
    {
        try
        {
            // Get approval matrix if available
            var approvalMatrix = await _context.ApprovalMatrices
                .Include(am => am.RequestTypes)
                .FirstOrDefaultAsync(am => am.RequestTypes.Any(rt => rt.Id == request.RequestTypeId) && am.TenantId == tenantId);

            if (approvalMatrix != null)
            {
                // Create approval steps
                var approvals = await _context.Approvals
                    .Include(a => a.Approver)
                    .Where(a => a.RequestId == request.Id)
                    .ToListAsync();

                foreach (var approval in approvals.OrderBy(a => a.Stage))
                {
                    await ExecuteWorkflowStepAsync(instanceId, "approval", new Dictionary<string, object>
                    {
                        { "approvalId", approval.Id },
                        { "approverId", approval.ApproverId },
                        { "stage", approval.Stage }
                    });
                }

                // Mark workflow as started
                await UpdateWorkflowStatusAsync(instanceId, "running", new Dictionary<string, object>
                {
                    { "approvalsCreated", approvals.Count },
                    { "matrixId", approvalMatrix.Id }
                });
            }
            else
            {
                // No approval matrix, just mark as completed
                await UpdateWorkflowStatusAsync(instanceId, "completed", new Dictionary<string, object>
                {
                    { "reason", "no_approval_matrix" }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow logic for request {RequestId}", request.Id);
            await UpdateWorkflowStatusAsync(instanceId, "failed", new Dictionary<string, object>
            {
                { "error", ex.Message }
            });
        }
    }

    private async Task<bool> ExecuteApprovalStepAsync(Guid requestId, Dictionary<string, object>? parameters)
    {
        try
        {
            // This would trigger actual approval logic
            _logger.LogInformation("Executing approval step for request {RequestId}", requestId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing approval step for request {RequestId}", requestId);
            return false;
        }
    }

    private async Task<bool> ExecuteNotificationStepAsync(Guid requestId, Dictionary<string, object>? parameters)
    {
        try
        {
            // This would trigger notification logic
            _logger.LogInformation("Executing notification step for request {RequestId}", requestId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing notification step for request {RequestId}", requestId);
            return false;
        }
    }

    private async Task<bool> ExecuteEscalationStepAsync(Guid requestId, Dictionary<string, object>? parameters)
    {
        try
        {
            // This would trigger escalation logic
            _logger.LogInformation("Executing escalation step for request {RequestId}", requestId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing escalation step for request {RequestId}", requestId);
            return false;
        }
    }

    private async Task<bool> ExecuteValidationStepAsync(Guid requestId, Dictionary<string, object>? parameters)
    {
        try
        {
            // This would validate request data
            _logger.LogInformation("Executing validation step for request {RequestId}", requestId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing validation step for request {RequestId}", requestId);
            return false;
        }
    }

    private async Task<bool> ExecuteAutomationStepAsync(Guid requestId, Dictionary<string, object>? parameters)
    {
        try
        {
            // This would trigger automated processes
            _logger.LogInformation("Executing automation step for request {RequestId}", requestId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing automation step for request {RequestId}", requestId);
            return false;
        }
    }

    private async Task<bool> ExecuteGenericStepAsync(string stepName, Dictionary<string, object>? parameters)
    {
        try
        {
            // Generic step execution
            _logger.LogInformation("Executing generic step: {StepName}", stepName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing generic step: {StepName}", stepName);
            return false;
        }
    }

    private List<WorkflowStep> CreateDefaultWorkflowSteps(WorkflowTracking workflowTracking)
    {
        return new List<WorkflowStep>
        {
            new WorkflowStep
            {
                StepId = "1",
                StepName = "Request Validation",
                StepType = "task",
                Status = workflowTracking.Status == "completed" ? "completed" : "pending",
                CompletedAt = workflowTracking.Status == "completed" ? workflowTracking.UpdatedAt : null
            },
            new WorkflowStep
            {
                StepId = "2",
                StepName = "Approval Process",
                StepType = "task",
                Status = workflowTracking.Status == "completed" ? "completed" : "pending",
                CompletedAt = workflowTracking.Status == "completed" ? workflowTracking.UpdatedAt : null
            },
            new WorkflowStep
            {
                StepId = "3",
                StepName = "Notifications",
                StepType = "task",
                Status = workflowTracking.Status == "completed" ? "completed" : "pending",
                CompletedAt = workflowTracking.Status == "completed" ? workflowTracking.UpdatedAt : null
            }
        };
    }

    private async Task CleanupCompletedWorkflowsInternalAsync()
    {
        try
        {
            var tenants = await _context.Tenants.Select(t => t.Id).ToListAsync();
            
            foreach (var tenantId in tenants)
            {
                await CleanupCompletedWorkflowsAsync(tenantId, 30);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automated workflow cleanup");
        }
    }
}