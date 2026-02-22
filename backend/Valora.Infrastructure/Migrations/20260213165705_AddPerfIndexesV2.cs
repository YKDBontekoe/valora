using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfIndexesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_ContextCompositeScore",
                table: "Listings",
                columns: new[] { "City", "ContextCompositeScore" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_ContextSafetyScore",
                table: "Listings",
                columns: new[] { "City", "ContextSafetyScore" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_LastFundaFetchUtc",
                table: "Listings",
                columns: new[] { "City", "LastFundaFetchUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Listings_City_ContextCompositeScore",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_City_ContextSafetyScore",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_City_LastFundaFetchUtc",
                table: "Listings");
        }
    }
}
