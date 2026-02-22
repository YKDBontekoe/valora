using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeospatialIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_Latitude",
                table: "Listings",
                column: "Latitude");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Longitude",
                table: "Listings",
                column: "Longitude");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_Latitude",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Longitude",
                table: "Listings");
        }
    }
}
