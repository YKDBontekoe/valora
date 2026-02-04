using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_Bedrooms",
                table: "Listings",
                columns: new[] { "City", "Bedrooms" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_LivingAreaM2",
                table: "Listings",
                columns: new[] { "City", "LivingAreaM2" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_Price",
                table: "Listings",
                columns: new[] { "City", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_LastFundaFetchUtc",
                table: "Listings",
                column: "LastFundaFetchUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_City_Bedrooms",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_City_LivingAreaM2",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_City_Price",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_LastFundaFetchUtc",
                table: "Listings");
        }
    }
}
