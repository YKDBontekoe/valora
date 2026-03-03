using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAiProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAiProfiles')
                BEGIN
                    CREATE TABLE [UserAiProfiles] (
                        [Id] uniqueidentifier NOT NULL,
                        [UserId] nvarchar(450) NOT NULL,
                        [Preferences] nvarchar(4000) NOT NULL,
                        [DisallowedSuggestions] nvarchar(4000) NOT NULL,
                        [HouseholdProfile] nvarchar(4000) NOT NULL,
                        [IsEnabled] bit NOT NULL,
                        [IsSessionOnlyMode] bit NOT NULL,
                        [Version] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_UserAiProfiles] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserAiProfiles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX [IX_UserAiProfiles_UserId] ON [UserAiProfiles] ([UserId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAiProfiles')
                BEGIN
                    DROP TABLE [UserAiProfiles];
                END
            ");
        }
    }
}
