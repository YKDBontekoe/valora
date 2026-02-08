using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_PriceHistory_Price_NonNegative",
                table: "PriceHistories",
                sql: "\"Price\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_Bedrooms_NonNegative",
                table: "Listings",
                sql: "\"Bedrooms\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_LivingAreaM2_NonNegative",
                table: "Listings",
                sql: "\"LivingAreaM2\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_Price_NonNegative",
                table: "Listings",
                sql: "\"Price\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PriceHistory_Price_NonNegative",
                table: "PriceHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_Bedrooms_NonNegative",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_LivingAreaM2_NonNegative",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_Price_NonNegative",
                table: "Listings");
        }
    }
}
