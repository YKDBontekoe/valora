using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceHistoryConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove invalid data before adding the constraint to avoid migration failure
            migrationBuilder.Sql("DELETE FROM \"PriceHistories\" WHERE \"Price\" <= 0;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriceHistory_Price",
                table: "PriceHistories",
                sql: "\"Price\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PriceHistory_Price",
                table: "PriceHistories");
        }
    }
}
