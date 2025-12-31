using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LineNotify.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    HostFilter = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ServiceFilter = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    ReceiveTimeStart = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    ReceiveTimeEnd = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MuteTimeStart = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    MuteTimeEnd = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    AllowDuplicateAlerts = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DuplicateAlertIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Icon = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    IsSystemDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTypes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    LineUserId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    PictureUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LineAccessToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    LineAccessTokenExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GroupMessageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    MessageTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMessageTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMessageTypes_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMessageTypes_MessageTypes_MessageTypeId",
                        column: x => x.MessageTypeId,
                        principalTable: "MessageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LinkedUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_Users_LinkedUserId",
                        column: x => x.LinkedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MessageTypeId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_MessageTypes_MessageTypeId",
                        column: x => x.MessageTypeId,
                        principalTable: "MessageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    CreatedByAdminId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    KeyHash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    KeyPrefix = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Permissions = table.Column<string>(type: "json", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Admins_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AdminId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    OldValues = table.Column<string>(type: "longtext", nullable: true),
                    NewValues = table.Column<string>(type: "longtext", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AddedByAdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Admins_AddedByAdminId",
                        column: x => x.AddedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LoginLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    AdminId = table.Column<int>(type: "int", nullable: true),
                    LoginType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FailureReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    LoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginLogs_Admins_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Admins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LoginLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    MessageTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    SourceHost = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    SourceService = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    SourceIp = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "json", nullable: true),
                    Priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TargetGroups = table.Column<string>(type: "json", nullable: true),
                    ApiKeyId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Messages_MessageTypes_MessageTypeId",
                        column: x => x.MessageTypeId,
                        principalTable: "MessageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LineMessageId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SkipReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageDeliveries_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageDeliveries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "Id", "CreatedAt", "DisplayName", "IsActive", "IsSuperAdmin", "LastLoginAt", "LinkedUserId", "MustChangePassword", "PasswordHash", "UpdatedAt", "Username" },
                values: new object[] { 1, new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(4400), "系統管理員", true, true, null, null, true, "$2a$11$K3LiTXHXrCXl7J5GQs0B6eFg2y1YuP1W5m2X3Qk4R5v6N7M8O9P0Q", new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(4400), "ADMIN" });

            migrationBuilder.InsertData(
                table: "MessageTypes",
                columns: new[] { "Id", "Code", "Color", "CreatedAt", "Description", "Icon", "IsActive", "IsSystemDefault", "Name", "Priority", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CRITICAL", "#FF0000", new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(310), "嚴重告警，需要立即處理", "🔴", true, true, "嚴重", 1, new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(310) },
                    { 2, "WARNING", "#FFA500", new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1500), "警告告警，需要關注", "🟠", true, true, "警告", 2, new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1500) },
                    { 3, "UNKNOWN", "#808080", new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1510), "未知狀態", "⚪", true, true, "未知", 3, new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1510) },
                    { 4, "OK", "#00FF00", new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1510), "恢復正常", "🟢", true, true, "正常", 4, new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1510) },
                    { 5, "INFO", "#0000FF", new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1510), "一般資訊通知", "🔵", true, true, "資訊", 5, new DateTime(2025, 12, 31, 12, 59, 53, 572, DateTimeKind.Utc).AddTicks(1510) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_LinkedUserId",
                table: "Admins",
                column: "LinkedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Username",
                table: "Admins",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatedByAdminId",
                table: "ApiKeys",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyPrefix",
                table: "ApiKeys",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AdminId",
                table: "AuditLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_AddedByAdminId",
                table: "GroupMembers",
                column: "AddedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_GroupId_UserId",
                table: "GroupMembers",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMessageTypes_GroupId_MessageTypeId",
                table: "GroupMessageTypes",
                columns: new[] { "GroupId", "MessageTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMessageTypes_MessageTypeId",
                table: "GroupMessageTypes",
                column: "MessageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Code",
                table: "Groups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_AdminId",
                table: "LoginLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_LoginAt",
                table: "LoginLogs",
                column: "LoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_UserId_LoginAt",
                table: "LoginLogs",
                columns: new[] { "UserId", "LoginAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeliveries_MessageId_UserId",
                table: "MessageDeliveries",
                columns: new[] { "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeliveries_NextRetryAt",
                table: "MessageDeliveries",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeliveries_Status",
                table: "MessageDeliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeliveries_UserId",
                table: "MessageDeliveries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ApiKeyId",
                table: "Messages",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedAt",
                table: "Messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageTypeId",
                table: "Messages",
                column: "MessageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SourceHost_SourceService_MessageTypeId",
                table: "Messages",
                columns: new[] { "SourceHost", "SourceService", "MessageTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageTypes_Code",
                table: "MessageTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_GroupId",
                table: "Subscriptions",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_MessageTypeId",
                table: "Subscriptions",
                column: "MessageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_MessageTypeId_GroupId",
                table: "Subscriptions",
                columns: new[] { "UserId", "MessageTypeId", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LineUserId",
                table: "Users",
                column: "LineUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "GroupMessageTypes");

            migrationBuilder.DropTable(
                name: "LoginLogs");

            migrationBuilder.DropTable(
                name: "MessageDeliveries");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "MessageTypes");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
