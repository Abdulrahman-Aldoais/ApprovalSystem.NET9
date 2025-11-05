using ApprovalSystem.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ApprovalSystem.Infrastructure.Data;

/// <summary>
/// DbContext الرئيسي مع دعم Multi-tenancy
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<ApprovalMatrix> ApprovalMatrices => Set<ApprovalMatrix>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<RequestAudit> RequestAudits => Set<RequestAudit>();
    public DbSet<WorkflowTracking> WorkflowTrackings => Set<WorkflowTracking>();
    public DbSet<ApprovalEscalation> ApprovalEscalations => Set<ApprovalEscalation>();
    public DbSet<WorkflowConfiguration> WorkflowConfigurations => Set<WorkflowConfiguration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Tenant relationships with Row Level Security
        ConfigureTenantRelationships(builder);

        // Configure Identity User
        ConfigureIdentity(builder);

        // Configure Relationships
        ConfigureRelationships(builder);

        // Configure Indexes
        ConfigureIndexes(builder);

        // Configure JSON columns
        ConfigureJsonColumns(builder);

        // Seed initial data
        SeedData(builder);
    }

    private void ConfigureTenantRelationships(ModelBuilder builder)
    {
        // Tenant
        builder.Entity<Tenant>()
            .HasIndex(t => t.Identifier)
            .IsUnique();

        builder.Entity<Tenant>()
            .HasMany(t => t.Users)
            .WithOne(u => u.Tenant)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tenant>()
            .HasMany(t => t.Modules)
            .WithOne(m => m.Tenant)
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Tenant>()
            .HasMany(t => t.Requests)
            .WithOne(r => r.Tenant)
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Tenant>()
            .HasMany(t => t.ApprovalMatrices)
            .WithOne(am => am.Tenant)
            .HasForeignKey(am => am.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Tenant>()
            .HasMany(t => t.Notifications)
            .WithOne(n => n.Tenant)
            .HasForeignKey(n => n.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkflowConfiguration relationships
        builder.Entity<Tenant>()
            .HasMany(t => t.WorkflowConfigurations)
            .WithOne(wc => wc.Tenant)
            .HasForeignKey(wc => wc.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkflowConfiguration - RequestType relationship
        builder.Entity<WorkflowConfiguration>()
            .HasOne(wc => wc.RequestType)
            .WithMany()
            .HasForeignKey(wc => wc.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void ConfigureIdentity(ModelBuilder builder)
    {
        // User configuration
        builder.Entity<User>()
            .HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique();

        builder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Entity<User>()
            .Property(u => u.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Configure Identity tables
        builder.Entity<User>().ToTable("AspNetUsers");
    }

    private void ConfigureRelationships(ModelBuilder builder)
    {
        // Request relationships
        builder.Entity<Request>()
            .HasOne(r => r.Requester)
            .WithMany(u => u.SubmittedRequests)
            .HasForeignKey(r => r.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Request>()
            .HasOne(r => r.RequestType)
            .WithMany(rt => rt.Requests)
            .HasForeignKey(r => r.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Request>()
            .HasOne(r => r.ApprovalMatrix)
            .WithMany(am => am.Requests)
            .HasForeignKey(r => r.ApprovalMatrixId)
            .OnDelete(DeleteBehavior.SetNull);

        // Approval relationships
        builder.Entity<Approval>()
            .HasOne(a => a.Request)
            .WithMany(r => r.Approvals)
            .HasForeignKey(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Approval>()
            .HasOne(a => a.Approver)
            .WithMany(u => u.Approvals)
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification relationships
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Module relationships
        builder.Entity<Module>()
            .HasOne(m => m.Tenant)
            .WithMany(t => t.Modules)
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Module>()
            .HasMany(m => m.RequestTypes)
            .WithOne(rt => rt.Module)
            .HasForeignKey(rt => rt.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Approval Matrix relationships
        builder.Entity<ApprovalMatrix>()
            .HasOne(am => am.Tenant)
            .WithMany(t => t.ApprovalMatrices)
            .HasForeignKey(am => am.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApprovalMatrix>()
            .HasMany(am => am.RequestTypes)
            .WithOne(rt => rt.ApprovalMatrix)
            .HasForeignKey(rt => rt.ApprovalMatrixId)
            .OnDelete(DeleteBehavior.SetNull);

        // Request Type relationships
        builder.Entity<RequestType>()
            .HasOne(rt => rt.Tenant)
            .WithMany(t => t.Requests)
            .HasForeignKey(rt => rt.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attachment relationships
        builder.Entity<Attachment>()
            .HasOne(a => a.Request)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Attachment>()
            .HasOne(a => a.UploadedBy)
            .WithMany()
            .HasForeignKey(a => a.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Request Audit relationships
        builder.Entity<RequestAudit>()
            .HasOne(ra => ra.Tenant)
            .WithMany(t => t.RequestAudits)
            .HasForeignKey(ra => ra.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RequestAudit>()
            .HasOne(ra => ra.Request)
            .WithMany(r => r.RequestAudits)
            .HasForeignKey(ra => ra.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RequestAudit>()
            .HasOne(ra => ra.Actor)
            .WithMany()
            .HasForeignKey(ra => ra.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Workflow Tracking relationships
        builder.Entity<WorkflowTracking>()
            .HasOne(wt => wt.Tenant)
            .WithMany(t => t.WorkflowTrackings)
            .HasForeignKey(wt => wt.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkflowTracking>()
            .HasOne(wt => wt.Request)
            .WithMany(r => r.WorkflowTrackings)
            .HasForeignKey(wt => wt.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Approval Escalation relationships
        builder.Entity<ApprovalEscalation>()
            .HasOne(ae => ae.Tenant)
            .WithMany(t => t.ApprovalEscalations)
            .HasForeignKey(ae => ae.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApprovalEscalation>()
            .HasOne(ae => ae.Request)
            .WithMany()
            .HasForeignKey(ae => ae.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApprovalEscalation>()
            .HasOne(ae => ae.Approval)
            .WithMany()
            .HasForeignKey(ae => ae.ApprovalId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureIndexes(ModelBuilder builder)
    {
        // Request indexes
        builder.Entity<Request>()
            .HasIndex(r => new { r.TenantId, r.Status })
            .HasDatabaseName("IX_Requests_TenantId_Status");

        builder.Entity<Request>()
            .HasIndex(r => new { r.TenantId, r.RequesterId })
            .HasDatabaseName("IX_Requests_TenantId_RequesterId");

        builder.Entity<Request>()
            .HasIndex(r => new { r.TenantId, r.RequestTypeId })
            .HasDatabaseName("IX_Requests_TenantId_RequestTypeId");

        builder.Entity<Request>()
            .HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Requests_CreatedAt");

        // Approval indexes
        builder.Entity<Approval>()
            .HasIndex(a => new { a.RequestId, a.Stage })
            .HasDatabaseName("IX_Approvals_RequestId_Stage");

        builder.Entity<Approval>()
            .HasIndex(a => new { a.ApproverId, a.Status })
            .HasDatabaseName("IX_Approvals_ApproverId_Status");

        // Notification indexes
        builder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");

        builder.Entity<Notification>()
            .HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");

        // Request Audit indexes
        builder.Entity<RequestAudit>()
            .HasIndex(ra => new { ra.RequestId, ra.CreatedAt })
            .HasDatabaseName("IX_RequestAudit_RequestId_CreatedAt");

        builder.Entity<RequestAudit>()
            .HasIndex(ra => new { ra.TenantId, ra.ActionType })
            .HasDatabaseName("IX_RequestAudit_TenantId_ActionType");
    }

    private void ConfigureJsonColumns(ModelBuilder builder)
    {
        // Request JSON columns
        builder.Entity<Request>()
            .Property(r => r.Data)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);

        // RequestType JSON columns
        builder.Entity<RequestType>()
            .Property(rt => rt.AmountThresholds)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);

        builder.Entity<RequestType>()
            .Property(rt => rt.RequiredFields)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);

        // ApprovalMatrix JSON columns
        builder.Entity<ApprovalMatrix>()
            .Property(am => am.Rules)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);

        builder.Entity<ApprovalMatrix>()
            .Property(am => am.Conditions)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);

        // Notification JSON columns
        builder.Entity<Notification>()
            .Property(n => n.Data)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);

        // WorkflowTracking JSON columns
        builder.Entity<WorkflowTracking>()
            .Property(wt => wt.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);

        // RequestAudit JSON columns
        builder.Entity<RequestAudit>()
            .Property(ra => ra.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => v,
                v => string.IsNullOrEmpty(v) ? null : v);
    }

    private void SeedData(ModelBuilder builder)
    {
        // Create default tenant
        builder.Entity<Tenant>().HasData(
            new Tenant
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                Name = "الشركة الافتراضية",
                Identifier = "default-tenant",
                IsActive = true,
                ContactEmail = "admin@default.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Create default module
        builder.Entity<Module>().HasData(
            new Module
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                Name = "الموافقات العامة",
                Description = "وحدة الموافقات الأساسية",
                TenantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                IsActive = true,
                SortOrder = 1,
                Color = "blue",
                CreatedAt = DateTime.UtcNow
            }
        );

        // Create default approval matrix
        builder.Entity<ApprovalMatrix>().HasData(
            new ApprovalMatrix
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                Name = "مصفوفة الموافقة الافتراضية",
                Description = "مصفوفة الموافقة الأساسية للمؤسسة",
                TenantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Create default request type
        builder.Entity<RequestType>().HasData(
            new RequestType
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
                Name = "طلب موافقة عام",
                Description = "طلب موافقة عامة",
                TenantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                ModuleId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                ApprovalMatrixId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<Request>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
