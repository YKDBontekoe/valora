using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfIndexesV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_LastFundaFetchUtc_Price",
                table: "Listings",
                columns: new[] { "City", "LastFundaFetchUtc", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Latitude_Longitude",
                table: "Listings",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_City_LastFundaFetchUtc_Price",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Latitude_Longitude",
                table: "Listings");
        }
    }
}
