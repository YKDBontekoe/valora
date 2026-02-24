using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class EnsureBatchJobTypeConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old constraint if it exists (or the one we suspect is wrong)
            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs");

            // Add the correct constraint including 'AllCitiesIngestion'
            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs",
                sql: "[Type] IN ('CityIngestion', 'MapGeneration', 'AllCitiesIngestion')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // In case of rollback, we ensure the constraint is still valid for the application code
            // We do not want to revert to a broken state where 'AllCitiesIngestion' is disallowed.

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs",
                sql: "[Type] IN ('CityIngestion', 'MapGeneration', 'AllCitiesIngestion')");
        }
    }
}
