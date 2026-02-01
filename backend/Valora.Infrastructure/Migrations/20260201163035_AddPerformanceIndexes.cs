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
            migrationBuilder.CreateIndex(
                name: "IX_Listings_Bedrooms",
                table: "Listings",
                column: "Bedrooms");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_LivingAreaM2",
                table: "Listings",
                column: "LivingAreaM2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_Bedrooms",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_LivingAreaM2",
                table: "Listings");
        }
    }
}
