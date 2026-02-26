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
            // Use ONLINE = ON to prevent table locking during index creation in SQL Server
            // This allows the application to remain responsive during migration
            migrationBuilder.Sql("CREATE INDEX [IX_Workspaces_CreatedAt] ON [Workspaces] ([CreatedAt]) WITH (ONLINE = ON)");

            migrationBuilder.Sql("CREATE INDEX [IX_SavedListings_WorkspaceId_CreatedAt] ON [SavedListings] ([WorkspaceId], [CreatedAt]) WITH (ONLINE = ON)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX [IX_Workspaces_CreatedAt] ON [Workspaces]");

            migrationBuilder.Sql("DROP INDEX [IX_SavedListings_WorkspaceId_CreatedAt] ON [SavedListings]");
        }
    }
}
