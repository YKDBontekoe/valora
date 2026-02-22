using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_ContextAmenitiesScore",
                table: "Listings",
                sql: "\"ContextAmenitiesScore\" >= 0 AND \"ContextAmenitiesScore\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_ContextCompositeScore",
                table: "Listings",
                sql: "\"ContextCompositeScore\" >= 0 AND \"ContextCompositeScore\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_ContextEnvironmentScore",
                table: "Listings",
                sql: "\"ContextEnvironmentScore\" >= 0 AND \"ContextEnvironmentScore\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_ContextSafetyScore",
                table: "Listings",
                sql: "\"ContextSafetyScore\" >= 0 AND \"ContextSafetyScore\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listing_ContextSocialScore",
                table: "Listings",
                sql: "\"ContextSocialScore\" >= 0 AND \"ContextSocialScore\" <= 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_ContextAmenitiesScore",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_ContextCompositeScore",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_ContextEnvironmentScore",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_ContextSafetyScore",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listing_ContextSocialScore",
                table: "Listings");
        }
    }
}
