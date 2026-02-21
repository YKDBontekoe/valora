using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRedundantListingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_City",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Latitude",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Longitude",
                table: "Listings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_City",
                table: "Listings",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Latitude",
                table: "Listings",
                column: "Latitude");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Longitude",
                table: "Listings",
                column: "Longitude");
        }
    }
}
