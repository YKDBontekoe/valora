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
            migrationBuilder.Sql("UPDATE \"Listings\" SET \"Bedrooms\" = 0 WHERE \"Bedrooms\" < 0;");
            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_Bedrooms",
                table: "Listings",
                sql: "\"Bedrooms\" >= 0");

            migrationBuilder.Sql("UPDATE \"Listings\" SET \"LivingAreaM2\" = 1 WHERE \"LivingAreaM2\" <= 0;");
            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_LivingAreaM2",
                table: "Listings",
                sql: "\"LivingAreaM2\" > 0");

            migrationBuilder.Sql("UPDATE \"Listings\" SET \"Price\" = 1 WHERE \"Price\" <= 0;");
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
