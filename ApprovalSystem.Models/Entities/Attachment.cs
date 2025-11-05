using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApprovalSystem.Models.Entities;

/// <summary>
/// كيان المرفقات
/// </summary>
public class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FileUrl { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [Required]
    [ForeignKey("Request")]
    public Guid RequestId { get; set; }

    [Required]
    [ForeignKey("UploadedBy")]
    public string UploadedById { get; set; } = string.Empty;

    // Navigation Properties
    public virtual Request Request { get; set; } = null!;
    public virtual User UploadedBy { get; set; } = null!;

    // Helper Properties
    public string FileSizeFormatted => FileSize switch
    {
        < 1024 => $"{FileSize} بايت",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} كيلوبايت",
        < 1024 * 1024 * 1024 => $"{FileSize / (1024.0 * 1024.0):F1} ميجابايت",
        _ => $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} جيجابايت"
    };

    public bool IsImage => ContentType?.StartsWith("image/") == true;
    public bool IsPdf => ContentType == "application/pdf";
    public bool IsDocument => ContentType?.Contains("document") == true || ContentType == "application/vnd.ms-word" || ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public bool IsAllowedType()
    {
        var allowedTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "image/jpeg",
            "image/png",
            "image/gif",
            "text/plain"
        };
        
        return allowedTypes.Contains(ContentType);
    }
}
