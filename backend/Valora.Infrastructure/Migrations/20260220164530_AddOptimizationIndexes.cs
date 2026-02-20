using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_RecordedAt",
                table: "PriceHistories",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_LastFundaFetchUtc_Price",
                table: "Listings",
                columns: new[] { "City", "LastFundaFetchUtc", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Latitude_Longitude",
                table: "Listings",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobs_CreatedAt",
                table: "BatchJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobs_Status",
                table: "BatchJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceHistories_RecordedAt",
                table: "PriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_Listings_City_LastFundaFetchUtc_Price",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Latitude_Longitude",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_BatchJobs_CreatedAt",
                table: "BatchJobs");

            migrationBuilder.DropIndex(
                name: "IX_BatchJobs_Status",
                table: "BatchJobs");
        }
    }
}
