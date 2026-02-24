using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeActivityLogWorkspaceIdNullable_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Workspaces_WorkspaceId",
                table: "ActivityLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Workspaces_WorkspaceId",
                table: "ActivityLogs",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Workspaces_WorkspaceId",
                table: "ActivityLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Workspaces_WorkspaceId",
                table: "ActivityLogs",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
