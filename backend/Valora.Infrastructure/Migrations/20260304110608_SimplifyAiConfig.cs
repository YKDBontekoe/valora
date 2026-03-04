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
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.DropCheckConstraint(
                    name: "CK_AiModelConfig_Intent",
                    table: "AiModelConfigs");

                // Safely drop column if it exists in SQL Server
                migrationBuilder.Sql(@"
                    IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'FallbackModels' AND Object_ID = Object_ID(N'AiModelConfigs'))
                    BEGIN
                        ALTER TABLE [AiModelConfigs] DROP COLUMN [FallbackModels];
                    END
                ", suppressTransaction: true);
            }
            else
            {
                // Fallback for SQLite / Testing
                migrationBuilder.DropColumn(
                    name: "FallbackModels",
                    table: "AiModelConfigs");
            }

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

            // Add LLM Parameters columns
            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "AiModelConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "AiModelConfigs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "AiModelConfigs",
                type: "float",
                nullable: true);

            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.AddCheckConstraint(
                    name: "CK_AiModelConfig_Feature",
                    table: "AiModelConfigs",
                    sql: "[Feature] NOT LIKE '%[^a-zA-Z0-9_]%'");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.DropCheckConstraint(
                    name: "CK_AiModelConfig_Feature",
                    table: "AiModelConfigs");
            }

            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "AiModelConfigs");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "AiModelConfigs");

            migrationBuilder.DropColumn(
                name: "Temperature",
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

            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.AddCheckConstraint(
                    name: "CK_AiModelConfig_Intent",
                    table: "AiModelConfigs",
                    sql: "[Intent] NOT LIKE '%[^a-zA-Z0-9_]%'");
            }
        }
    }
}
