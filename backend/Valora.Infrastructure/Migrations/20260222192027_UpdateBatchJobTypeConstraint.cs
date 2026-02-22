using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBatchJobTypeConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs");

            migrationBuilder.CreateTable(
                name: "UserAiProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Preferences = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisallowedSuggestions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HouseholdProfile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsSessionOnlyMode = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAiProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAiProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs",
                sql: "[Type] IN ('CityIngestion', 'MapGeneration', 'AllCitiesIngestion')");

            migrationBuilder.CreateIndex(
                name: "IX_UserAiProfiles_UserId",
                table: "UserAiProfiles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAiProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BatchJob_Type",
                table: "BatchJobs",
                sql: "[Type] IN ('CityIngestion', 'MapGeneration')");
        }
    }
}
