using System.ComponentModel.DataAnnotations;

namespace ApprovalSystem.ViewModels;

/// <summary>
/// ViewModels للمصادقة وإدارة المستخدمين
/// </summary>

/// <summary>
/// نموذج تسجيل الدخول
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "تذكرني")]
    public bool RememberMe { get; set; }
}

/// <summary>
/// نموذج تسجيل المستخدم الجديد
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [Display(Name = "الاسم الكامل")]
    [MaxLength(255, ErrorMessage = "الاسم يجب ألا يزيد عن 255 حرف")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 8 أحرف وتحتوي على حرف كبير وصغير ورقم", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور")]
    [Compare("Password", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقتين")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "القسم")]
    [MaxLength(100, ErrorMessage = "القسم يجب ألا يزيد عن 100 حرف")]
    public string? Department { get; set; }

    [Display(Name = "رقم الهاتف")]
    [MaxLength(20, ErrorMessage = "رقم الهاتف يجب ألا يزيد عن 20 حرف")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "معرف المؤسسة مطلوب")]
    [Display(Name = "معرف المؤسسة")]
    public Guid TenantId { get; set; }
}

/// <summary>
/// نموذج تحديث الملف الشخصي
/// </summary>
public class UpdateProfileViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [Display(Name = "الاسم الكامل")]
    [MaxLength(255, ErrorMessage = "الاسم يجب ألا يزيد عن 255 حرف")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "القسم")]
    [MaxLength(100, ErrorMessage = "القسم يجب ألا يزيد عن 100 حرف")]
    public string? Department { get; set; }

    [Display(Name = "رقم الهاتف")]
    [MaxLength(20, ErrorMessage = "رقم الهاتف يجب ألا يزيد عن 20 حرف")]
    public string? Phone { get; set; }

    [Display(Name = "رابط الصورة الشخصية")]
    [MaxLength(500, ErrorMessage = "رابط الصورة يجب ألا يزيد عن 500 حرف")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "اللغة المفضلة")]
    [MaxLength(20, ErrorMessage = "اللغة يجب ألا تزيد عن 20 حرف")]
    public string? PreferredLanguage { get; set; } = "ar";

    [Display(Name = "المنطقة الزمنية")]
    [MaxLength(50, ErrorMessage = "المنطقة الزمنية يجب ألا تزيد عن 50 حرف")]
    public string? TimeZone { get; set; } = "Arabia Standard Time";

    [Display(Name = "تفعيل إشعارات البريد الإلكتروني")]
    public bool EmailNotificationsEnabled { get; set; } = true;

    [Display(Name = "تفعيل الإشعارات الفورية")]
    public bool PushNotificationsEnabled { get; set; } = true;

    [Display(Name = "تفعيل إشعارات الرسائل النصية")]
    public bool SmsNotificationsEnabled { get; set; } = false;
}

/// <summary>
/// نموذج تغيير كلمة المرور
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الحالية")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 8 أحرف وتحتوي على حرف كبير وصغير ورقم", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الجديدة")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور الجديدة")]
    [Compare("NewPassword", ErrorMessage = "كلمة المرور الجديدة وتأكيد كلمة المرور غير متطابقتين")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>
/// نموذج عرض بيانات المستخدم
/// </summary>
public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? TimeZone { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool SmsNotificationsEnabled { get; set; }
}

/// <summary>
/// نموذج استجابة المصادقة
/// </summary>
public class AuthResponseViewModel
{
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserViewModel? User { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public List<string>? Roles { get; set; }
}

/// <summary>
/// نموذج إعادة تعيين كلمة المرور
/// </summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// نموذج إعادة تعيين كلمة المرور مع الرمز
/// </summary>
public class ResetPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون على الأقل 8 أحرف", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور الجديدة")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور الجديدة")]
    [Compare("Password", ErrorMessage = "كلمة المرور الجديدة وتأكيد كلمة المرور غير متطابقتين")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// نموذج بيانات المؤسسة (للتسجيل المتعدد المؤسسات)
/// </summary>
public class TenantViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// نموذج إعدادات المؤسسة
/// </summary>
public class TenantSettingsViewModel
{
    [Required(ErrorMessage = "اسم المؤسسة مطلوب")]
    [Display(Name = "اسم المؤسسة")]
    [MaxLength(255, ErrorMessage = "اسم المؤسسة يجب ألا يزيد عن 255 حرف")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "معرف المؤسسة مطلوب")]
    [Display(Name = "معرف المؤسسة")]
    [MaxLength(50, ErrorMessage = "معرف المؤسسة يجب ألا يزيد عن 50 حرف")]
    public string Identifier { get; set; } = string.Empty;

    [Display(Name = "رابط الشعار")]
    [MaxLength(500, ErrorMessage = "رابط الشعار يجب ألا يزيد عن 500 حرف")]
    public string? LogoUrl { get; set; }

    [Display(Name = "اللون الأساسي")]
    [MaxLength(20, ErrorMessage = "اللون الأساسي يجب ألا يزيد عن 20 حرف")]
    public string? PrimaryColor { get; set; }

    [Display(Name = "اللون الثانوي")]
    [MaxLength(20, ErrorMessage = "اللون الثانوي يجب ألا يزيد عن 20 حرف")]
    public string? SecondaryColor { get; set; }

    [Display(Name = "بريد التواصل")]
    [EmailAddress(ErrorMessage = "بريد التواصل غير صحيح")]
    [MaxLength(255, ErrorMessage = "بريد التواصل يجب ألا يزيد عن 255 حرف")]
    public string? ContactEmail { get; set; }

    [Display(Name = "هاتف التواصل")]
    [MaxLength(20, ErrorMessage = "هاتف التواصل يجب ألا يزيد عن 20 حرف")]
    public string? ContactPhone { get; set; }
}
