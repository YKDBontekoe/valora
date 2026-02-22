using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_Address",
                table: "Listings",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_PropertyType",
                table: "Listings",
                column: "PropertyType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_Address",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_PropertyType",
                table: "Listings");
        }
    }
}
