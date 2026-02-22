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
            // Execute index creation concurrently to avoid table locks in Postgres
            // Note: This requires the migration to be run outside of a transaction if the driver enforces it,
            // but for now we rely on the provider handling.

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_Bedrooms",
                table: "Listings",
                columns: new[] { "City", "Bedrooms" })
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_LivingAreaM2",
                table: "Listings",
                columns: new[] { "City", "LivingAreaM2" })
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_Price",
                table: "Listings",
                columns: new[] { "City", "Price" })
                .Annotation("Npgsql:CreatedConcurrently", true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_LastFundaFetchUtc",
                table: "Listings",
                column: "LastFundaFetchUtc")
                .Annotation("Npgsql:CreatedConcurrently", true);
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
