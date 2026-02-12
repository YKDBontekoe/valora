using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_Bedrooms",
                table: "Listings",
                sql: "\"Bedrooms\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_LivingAreaM2",
                table: "Listings",
                sql: "\"LivingAreaM2\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_Price",
                table: "Listings",
                sql: "\"Price\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_Bedrooms",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_LivingAreaM2",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_Price",
                table: "Listings");
        }
    }
}
