using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_City",
                table: "Listings",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ListedDate",
                table: "Listings",
                column: "ListedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_PostalCode",
                table: "Listings",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Price",
                table: "Listings",
                column: "Price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_City",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ListedDate",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_PostalCode",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Price",
                table: "Listings");
        }
    }
}
