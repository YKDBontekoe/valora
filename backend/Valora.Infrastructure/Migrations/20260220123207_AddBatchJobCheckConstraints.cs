using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchJobCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clean up invalid data before applying constraints
            migrationBuilder.Sql("DELETE FROM \"BatchJobs\" WHERE \"Status\" NOT IN ('Pending', 'Processing', 'Completed', 'Failed');");
            migrationBuilder.Sql("DELETE FROM \"BatchJobs\" WHERE \"Type\" NOT IN ('CityIngestion');");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchJob_Status",
                table: "BatchJobs",
                sql: "\"Status\" IN ('Pending', 'Processing', 'Completed', 'Failed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs",
                sql: "\"Type\" IN ('CityIngestion')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchJob_Status",
                table: "BatchJobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs");
        }
    }
}
