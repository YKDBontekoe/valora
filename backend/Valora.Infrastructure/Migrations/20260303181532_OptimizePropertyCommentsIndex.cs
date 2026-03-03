using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizePropertyCommentsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyComments_SavedPropertyId",
                table: "PropertyComments");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyComments_SavedPropertyId_CreatedAt",
                table: "PropertyComments",
                columns: new[] { "SavedPropertyId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyComments_SavedPropertyId_CreatedAt",
                table: "PropertyComments");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyComments_SavedPropertyId",
                table: "PropertyComments",
                column: "SavedPropertyId");
        }
    }
}
