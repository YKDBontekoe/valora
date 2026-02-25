using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-migration check: ensure no data exceeds 4000 chars before truncation
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM UserAiProfiles WHERE LEN(Preferences) > 4000)
                BEGIN
                    THROW 51000, 'Data migration failed: UserAiProfiles.Preferences contains values exceeding 4000 characters. Please trim data before applying migration.', 1;
                END
                IF EXISTS (SELECT 1 FROM UserAiProfiles WHERE LEN(HouseholdProfile) > 4000)
                BEGIN
                    THROW 51000, 'Data migration failed: UserAiProfiles.HouseholdProfile contains values exceeding 4000 characters. Please trim data before applying migration.', 1;
                END
                IF EXISTS (SELECT 1 FROM UserAiProfiles WHERE LEN(DisallowedSuggestions) > 4000)
                BEGIN
                    THROW 51000, 'Data migration failed: UserAiProfiles.DisallowedSuggestions contains values exceeding 4000 characters. Please trim data before applying migration.', 1;
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Preferences",
                table: "UserAiProfiles",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "HouseholdProfile",
                table: "UserAiProfiles",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DisallowedSuggestions",
                table: "UserAiProfiles",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Preferences",
                table: "UserAiProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "HouseholdProfile",
                table: "UserAiProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "DisallowedSuggestions",
                table: "UserAiProfiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);
        }
    }
}
