using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApprovalSystem.Infrastructure.Migrations
{
    /// <summary>
    /// إضافة جدول WorkflowConfigurations لنظام الإعدادات الديناميكية
    /// </summary>
    public partial class AddWorkflowConfigurationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestTypeId = table.Column<int>(type: "int", nullable: false),
                    WorkflowDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluationRules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationSettings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationSettings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartConditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletionConditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RequiresManualApproval = table.Column<bool>(type: "bit", nullable: false),
                    SupportsParallelApproval = table.Column<bool>(type: "bit", nullable: false),
                    MaxExecutionTimeHours = table.Column<int>(type: "int", nullable: true),
                    MaxRetryCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowConfigurations_RequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalTable: "RequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowConfigurations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // إنشاء الفهارس لتحسين الأداء
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_TenantId",
                table: "WorkflowConfigurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_RequestTypeId",
                table: "WorkflowConfigurations",
                column: "RequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_TenantId_RequestTypeId",
                table: "WorkflowConfigurations",
                columns: new[] { "TenantId", "RequestTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_TenantId_IsActive",
                table: "WorkflowConfigurations",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_TenantId_Status",
                table: "WorkflowConfigurations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_TenantId_Priority",
                table: "WorkflowConfigurations",
                columns: new[] { "TenantId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_WorkflowName",
                table: "WorkflowConfigurations",
                column: "WorkflowName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_CreatedAt",
                table: "WorkflowConfigurations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_UpdatedAt",
                table: "WorkflowConfigurations",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConfigurations_IsDeleted",
                table: "WorkflowConfigurations",
                column: "IsDeleted");

            // إضافة بيانات أولية لإعداد workflow أساسي
            migrationBuilder.InsertData(
                table: "WorkflowConfigurations",
                columns: new[] { 
                    "TenantId", "WorkflowName", "Description", "RequestTypeId", 
                    "WorkflowDefinition", "EvaluationRules", "EscalationSettings", 
                    "NotificationSettings", "StartConditions", "CompletionConditions", 
                    "DefaultData", "Priority", "IsActive", "RequiresManualApproval", 
                    "SupportsParallelApproval", "MaxRetryCount", "Status", "Version", 
                    "CreatedAt", "UpdatedAt", "CreatedBy", "IsDeleted"
                },
                values: new object[] { 
                    1, // TenantId - يحتاج تعديل للربط مع tenant موجود
                    "إعداد الموافقة الافتراضي", 
                    "إعداد أساسي للموافقة على الطلبات العامة",
                    1, // RequestTypeId - يحتاج تعديل للربط مع request type موجود
                    @"{
                        ""name"": ""BasicApprovalWorkflow"",
                        ""displayName"": ""سير عمل الموافقة الأساسي"",
                        ""description"": ""سير عمل أساسي للموافقة على الطلبات"",
                        ""version"": 1,
                        ""isLatest"": true,
                        ""isPublished"": true,
                        ""activities"": [
                            {
                                ""activityId"": ""start_approval"",
                                ""type"": ""StartApprovalWorkflow"",
                                ""displayName"": ""بدء الموافقة""
                            }
                        ]
                    }",
                    @"[
                        {
                            ""field"": ""Amount"",
                            ""operator"": ""GreaterThan"",
                            ""value"": 1000,
                            ""action"": ""RequireApproval"",
                            ""description"": ""طلبات أكثر من 1000 تحتاج موافقة"",
                            ""priority"": 2,
                            ""isActive"": true
                        },
                        {
                            ""field"": ""Amount"",
                            ""operator"": ""LessThanOrEqual"",
                            ""value"": 1000,
                            ""action"": ""AutoApprove"",
                            ""description"": ""طلبات 1000 أو أقل موافقة تلقائية"",
                            ""priority"": 1,
                            ""isActive"": true
                        }
                    ]",
                    @"{
                        ""enableEscalation"": true,
                        ""escalationTimeHours"": 24,
                        ""escalationLevels"": [
                            {
                                ""level"": 1,
                                ""timeoutHours"": 24,
                                ""escalationUsers"": [""manager@company.com""],
                                ""escalationRoles"": [""Manager""],
                                ""action"": ""EscalateToNext""
                            }
                        ],
                        ""notifyOnEscalation"": true,
                        ""escalationMessage"": ""تم تصعيد الطلب بسبب انتهاء الوقت المحدد""
                    }",
                    @"{
                        ""emailNotifications"": true,
                        ""smsNotifications"": false,
                        ""inAppNotifications"": true,
                        ""realTimeNotifications"": true,
                        ""templates"": {
                            ""requestSubmitted"": ""تم استلام طلبك وسيتم مراجعته قريباً"",
                            ""requestApproved"": ""تم الموافقة على طلبك"",
                            ""requestRejected"": ""تم رفض طلبك"",
                            ""requestEscalated"": ""تم تصعيد طلبك للمستوى التالي"",
                            ""requestCompleted"": ""تم الانتهاء من معالجة طلبك""
                        },
                        ""notificationEvents"": [
                            ""RequestSubmitted"",
                            ""RequestApproved"",
                            ""RequestRejected"",
                            ""RequestCompleted""
                        ]
                    }",
                    @"[]", // StartConditions - فارغ يعني يطبق على جميع الطلبات
                    @"[
                        {
                            ""field"": ""ApprovalStatus"",
                            ""operator"": ""Equals"",
                            ""value"": ""Approved"",
                            ""logicalOperator"": ""OR"",
                            ""groupId"": 1
                        },
                        {
                            ""field"": ""ApprovalStatus"",
                            ""operator"": ""Equals"",
                            ""value"": ""Rejected"",
                            ""logicalOperator"": ""OR"",
                            ""groupId"": 1
                        }
                    ]",
                    @"{
                        ""defaultPriority"": ""Normal"",
                        ""autoAssignApprover"": true,
                        ""requireComments"": false,
                        ""allowDelegation"": true
                    }",
                    2, // Priority - Normal
                    true, // IsActive
                    true, // RequiresManualApproval
                    false, // SupportsParallelApproval
                    3, // MaxRetryCount
                    "Published", // Status
                    "1.0", // Version
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    "system@approvalsystem.com", // CreatedBy
                    false // IsDeleted
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowConfigurations");
        }
    }
}