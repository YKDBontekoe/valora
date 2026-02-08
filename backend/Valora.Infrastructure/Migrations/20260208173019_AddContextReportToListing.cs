using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContextReportToListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ContextAmenitiesScore",
                table: "Listings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ContextCompositeScore",
                table: "Listings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ContextEnvironmentScore",
                table: "Listings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextReport",
                table: "Listings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ContextSafetyScore",
                table: "Listings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ContextSocialScore",
                table: "Listings",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ContextCompositeScore",
                table: "Listings",
                column: "ContextCompositeScore");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ContextSafetyScore",
                table: "Listings",
                column: "ContextSafetyScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_ContextCompositeScore",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_ContextSafetyScore",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ContextAmenitiesScore",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ContextCompositeScore",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ContextEnvironmentScore",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ContextReport",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ContextSafetyScore",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ContextSocialScore",
                table: "Listings");
        }
    }
}
