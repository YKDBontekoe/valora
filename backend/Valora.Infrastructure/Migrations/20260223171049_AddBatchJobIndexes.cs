using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchJobIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "BatchJobs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobs_Status_CreatedAt",
                table: "BatchJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobs_Target",
                table: "BatchJobs",
                column: "Target");

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobs_Type",
                table: "BatchJobs",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BatchJobs_Status_CreatedAt",
                table: "BatchJobs");

            migrationBuilder.DropIndex(
                name: "IX_BatchJobs_Target",
                table: "BatchJobs");

            migrationBuilder.DropIndex(
                name: "IX_BatchJobs_Type",
                table: "BatchJobs");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "BatchJobs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
