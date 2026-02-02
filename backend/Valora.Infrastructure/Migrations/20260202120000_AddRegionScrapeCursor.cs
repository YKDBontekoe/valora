using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionScrapeCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegionScrapeCursors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    NextBackfillPage = table.Column<int>(type: "integer", nullable: false),
                    LastRecentScrapeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastBackfillScrapeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionScrapeCursors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegionScrapeCursors_Region",
                table: "RegionScrapeCursors",
                column: "Region",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegionScrapeCursors");
        }
    }
}
