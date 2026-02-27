using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: ONLINE = ON is not used here to ensure compatibility with SQL Server Standard/Express editions
            // and CI environments that do not support online index operations.
            // For production environments with Enterprise Edition, these indexes should be created
            // during a maintenance window or manually with WITH (ONLINE = ON) to prevent table locking.
            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_CreatedAt",
                table: "Workspaces",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_WorkspaceId_CreatedAt",
                table: "SavedListings",
                columns: new[] { "WorkspaceId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workspaces_CreatedAt",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_SavedListings_WorkspaceId_CreatedAt",
                table: "SavedListings");
        }
    }
}
