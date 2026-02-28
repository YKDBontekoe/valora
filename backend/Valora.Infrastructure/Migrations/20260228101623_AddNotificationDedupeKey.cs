using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDedupeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_WorkspaceId",
                table: "ActivityLogs");

            migrationBuilder.AddColumn<string>(
                name: "DedupeKey",
                table: "Notifications",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_WorkspaceId_CreatedAt",
                table: "SavedListings",
                columns: new[] { "WorkspaceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_DedupeKey",
                table: "Notifications",
                columns: new[] { "UserId", "DedupeKey" },
                unique: true,
                filter: "[DedupeKey] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiModelConfig_Intent",
                table: "AiModelConfigs",
                sql: "[Intent] NOT LIKE '%[^a-zA-Z0-9_]%'");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_WorkspaceId_CreatedAt",
                table: "ActivityLogs",
                columns: new[] { "WorkspaceId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedListings_WorkspaceId_CreatedAt",
                table: "SavedListings");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_DedupeKey",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AiModelConfig_Intent",
                table: "AiModelConfigs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_WorkspaceId_CreatedAt",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "DedupeKey",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_WorkspaceId",
                table: "ActivityLogs",
                column: "WorkspaceId");
        }
    }
}
