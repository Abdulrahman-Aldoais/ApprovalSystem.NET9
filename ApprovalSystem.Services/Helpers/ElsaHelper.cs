using Elsa.ActivityResults;
using Elsa.Services.Models;
using System.Text.Json;

namespace ApprovalSystem.Services.Helpers;

/// <summary>
/// مساعد Elsa 3.x - يوفر توابع مساعدة للتعامل مع Elsa Activities
/// </summary>
public static class ElsaHelper
{
    /// <summary>
    /// حفظ البيانات في سياق الـ workflow (Elsa 3.x syntax)
    /// </summary>
    public static void SetVariable<T>(this ActivityExecutionContext context, string name, T value)
    {
        context.WorkflowExecutionContext.SetVariable(name, value);
    }

    /// <summary>
    /// الحصول على البيانات من سياق الـ workflow (Elsa 3.x syntax)
    /// </summary>
    public static T? GetVariable<T>(this ActivityExecutionContext context, string name)
    {
        return context.WorkflowExecutionContext.GetVariable<T>(name);
    }

    /// <summary>
    /// حفظ البيانات المعقدة كـ JSON في سياق الـ workflow
    /// </summary>
    public static void SetJsonVariable(this ActivityExecutionContext context, string name, object value)
    {
        var json = JsonSerializer.Serialize(value);
        context.WorkflowExecutionContext.SetVariable(name, json);
    }

    /// <summary>
    /// الحصول على البيانات من JSON في سياق الـ workflow
    /// </summary>
    public static T? GetJsonVariable<T>(this ActivityExecutionContext context, string name) where T : class
    {
        var json = context.WorkflowExecutionContext.GetVariable<string>(name);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// تسجيل معلومات في الـ workflow
    /// </summary>
    public static void LogInfo(this ActivityExecutionContext context, string message)
    {
        context.WorkflowExecutionContext.SetVariable($"Log_{DateTime.UtcNow:yyyyMMdd_HHmmss}", message);
    }

    /// <summary>
    /// تسجيل خطأ في الـ workflow
    /// </summary>
    public static void LogError(this ActivityExecutionContext context, string error)
    {
        context.WorkflowExecutionContext.SetVariable($"Error_{DateTime.UtcNow:yyyyMMdd_HHmmss}", error);
    }

    /// <summary>
    /// إنشاء نتيجة فاشلة
    /// </summary>
    public static IActivityExecutionResult FailedResult(string errorMessage, string outputName = "ErrorMessage")
    {
        return new ErrorActivityResult(errorMessage, outputName);
    }

    /// <summary>
    /// التحقق من وجود البيانات في السياق
    /// </summary>
    public static bool HasVariable(this ActivityExecutionContext context, string name)
    {
        return context.WorkflowExecutionContext.GetVariable<object>(name) != null;
    }

    /// <summary>
    /// حذف متغير من السياق
    /// </summary>
    public static void RemoveVariable(this ActivityExecutionContext context, string name)
    {
        // في Elsa 3.x، نحتاج لحفظ null لحذف المتغير
        context.WorkflowExecutionContext.SetVariable(name, null);
    }
}

/// <summary>
/// نتيجة نشاط الفشل مع البيانات
/// </summary>
public class ErrorActivityResult : IActivityExecutionResult
{
    public string ErrorMessage { get; }
    public string OutputName { get; }

    public ErrorActivityResult(string errorMessage, string outputName = "ErrorMessage")
    {
        ErrorMessage = errorMessage;
        OutputName = outputName;
    }

    public void Execute(ActivityExecutionContext context, CancellationToken cancellationToken)
    {
        context.WorkflowExecutionContext.SetVariable(OutputName, ErrorMessage);
    }


}

/// <summary>
/// مساعد لتحويل JSON إلى Dictionary
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// تحويل JSON string إلى Dictionary<string, object>
    /// </summary>
    public static Dictionary<string, object> ParseJsonToDictionary(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "{}")
            return new Dictionary<string, object>();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
            return result ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// تحويل JSON string إلى List<string>
    /// </summary>
    public static List<string> ParseJsonToStringList(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
            return new List<string>();

        try
        {
            var result = JsonSerializer.Deserialize<List<string>>(json);
            return result ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// تحويل object إلى JSON string
    /// </summary>
    public static string ToJson(object obj)
    {
        if (obj == null)
            return "{}";

        try
        {
            return JsonSerializer.Serialize(obj);
        }
        catch
        {
            return "{}";
        }
    }
}

/// <summary>
/// مساعد للتعامل مع إشعارات Elsa
/// </summary>
public static class NotificationHelper
{
    /// <summary>
    /// إنشاء payload للإشعار
    /// </summary>
    public static object CreateNotificationPayload(string type, string message, object data = null)
    {
        return new
        {
            Type = type,
            Message = message,
            Data = data,
            Timestamp = DateTime.UtcNow,
            Id = Guid.NewGuid().ToString()
        };
    }

    /// <summary>
    /// إنشاء payload للموافقة
    /// </summary>
    public static object CreateApprovalPayload(string approverId, string requestId, string customMessage = null)
    {
        return new
        {
            ApproverId = approverId,
            RequestId = requestId,
            CustomMessage = customMessage,
            CreatedAt = DateTime.UtcNow,
            ExpirationAt = DateTime.UtcNow.AddDays(1) // ينتهي خلال يوم
        };
    }
}
