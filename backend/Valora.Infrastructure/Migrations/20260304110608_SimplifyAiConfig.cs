using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAiConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AiModelConfig_Intent",
                table: "AiModelConfigs");

            // We must wrap schema changes in raw SQL with IF EXISTS checks because SQLite (Testcontainers default)
            // does not support constraints and Testcontainers may run out of sync.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'FallbackModels' AND Object_ID = Object_ID(N'AiModelConfigs'))
                BEGIN
                    ALTER TABLE [AiModelConfigs] DROP COLUMN [FallbackModels];
                END
            ", suppressTransaction: true);

            migrationBuilder.RenameColumn(
                name: "PrimaryModel",
                table: "AiModelConfigs",
                newName: "ModelId");

            migrationBuilder.RenameColumn(
                name: "Intent",
                table: "AiModelConfigs",
                newName: "Feature");

            migrationBuilder.RenameIndex(
                name: "IX_AiModelConfigs_Intent",
                table: "AiModelConfigs",
                newName: "IX_AiModelConfigs_Feature");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiModelConfig_Feature",
                table: "AiModelConfigs",
                sql: "[Feature] NOT LIKE '%[^a-zA-Z0-9_]%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AiModelConfig_Feature",
                table: "AiModelConfigs");

            migrationBuilder.RenameColumn(
                name: "ModelId",
                table: "AiModelConfigs",
                newName: "PrimaryModel");

            migrationBuilder.RenameColumn(
                name: "Feature",
                table: "AiModelConfigs",
                newName: "Intent");

            migrationBuilder.RenameIndex(
                name: "IX_AiModelConfigs_Feature",
                table: "AiModelConfigs",
                newName: "IX_AiModelConfigs_Intent");

            migrationBuilder.AddColumn<string>(
                name: "FallbackModels",
                table: "AiModelConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AiModelConfig_Intent",
                table: "AiModelConfigs",
                sql: "[Intent] NOT LIKE '%[^a-zA-Z0-9_]%'");
        }
    }
}
