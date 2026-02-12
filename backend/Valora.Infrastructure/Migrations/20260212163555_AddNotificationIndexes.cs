using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

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
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

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
