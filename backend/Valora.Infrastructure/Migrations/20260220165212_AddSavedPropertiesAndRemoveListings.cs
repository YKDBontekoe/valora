using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedPropertiesAndRemoveListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceHistories");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.CreateTable(
                name: "SavedProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    CachedScore = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedProperties", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedProperties");

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BalconyM2 = table.Column<int>(type: "integer", nullable: true),
                    Bathrooms = table.Column<int>(type: "integer", nullable: true),
                    Bedrooms = table.Column<int>(type: "integer", nullable: true),
                    BrochureUrl = table.Column<string>(type: "text", nullable: true),
                    BrokerAssociationCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BrokerLogoUrl = table.Column<string>(type: "text", nullable: true),
                    BrokerOfficeId = table.Column<int>(type: "integer", nullable: true),
                    BrokerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CVBoilerBrand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CVBoilerYear = table.Column<int>(type: "integer", nullable: true),
                    CadastralDesignation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConstructionPeriod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContextAmenitiesScore = table.Column<double>(type: "double precision", nullable: true),
                    ContextCompositeScore = table.Column<double>(type: "double precision", nullable: true),
                    ContextEnvironmentScore = table.Column<double>(type: "double precision", nullable: true),
                    ContextReport = table.Column<string>(type: "jsonb", nullable: true),
                    ContextSafetyScore = table.Column<double>(type: "double precision", nullable: true),
                    ContextSocialScore = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EnergyLabel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ExternalStorageM2 = table.Column<int>(type: "integer", nullable: true),
                    Features = table.Column<string>(type: "jsonb", nullable: false),
                    FiberAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    FloorPlanUrls = table.Column<string>(type: "jsonb", nullable: false),
                    FundaId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GardenM2 = table.Column<int>(type: "integer", nullable: true),
                    GardenOrientation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HasGarage = table.Column<bool>(type: "boolean", nullable: false),
                    HeatingType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageUrls = table.Column<string>(type: "jsonb", nullable: false),
                    InsulationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSoldOrRented = table.Column<bool>(type: "boolean", nullable: false),
                    Labels = table.Column<string>(type: "jsonb", nullable: false),
                    LastFundaFetchUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    ListedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LivingAreaM2 = table.Column<int>(type: "integer", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    NeighborhoodAvgPriceM2 = table.Column<decimal>(type: "numeric", nullable: true),
                    NeighborhoodPopulation = table.Column<int>(type: "integer", nullable: true),
                    NumberOfFloors = table.Column<int>(type: "integer", nullable: true),
                    OpenHouseDates = table.Column<string>(type: "jsonb", nullable: false),
                    OwnershipType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ParkingType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PlotAreaM2 = table.Column<int>(type: "integer", nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PropertyType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RoofType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SaveCount = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VVEContribution = table.Column<decimal>(type: "numeric", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: true),
                    VirtualTourUrl = table.Column<string>(type: "text", nullable: true),
                    VolumeM3 = table.Column<int>(type: "integer", nullable: true),
                    YearBuilt = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.CheckConstraint("CK_Listing_Bedrooms", "\"Bedrooms\" >= 0");
                    table.CheckConstraint("CK_Listing_ContextAmenitiesScore", "\"ContextAmenitiesScore\" >= 0 AND \"ContextAmenitiesScore\" <= 100");
                    table.CheckConstraint("CK_Listing_ContextCompositeScore", "\"ContextCompositeScore\" >= 0 AND \"ContextCompositeScore\" <= 100");
                    table.CheckConstraint("CK_Listing_ContextEnvironmentScore", "\"ContextEnvironmentScore\" >= 0 AND \"ContextEnvironmentScore\" <= 100");
                    table.CheckConstraint("CK_Listing_ContextSafetyScore", "\"ContextSafetyScore\" >= 0 AND \"ContextSafetyScore\" <= 100");
                    table.CheckConstraint("CK_Listing_ContextSocialScore", "\"ContextSocialScore\" >= 0 AND \"ContextSocialScore\" <= 100");
                    table.CheckConstraint("CK_Listing_LivingAreaM2", "\"LivingAreaM2\" > 0");
                    table.CheckConstraint("CK_Listing_Price", "\"Price\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "PriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistories", x => x.Id);
                    table.CheckConstraint("CK_PriceHistory_Price", "\"Price\" > 0");
                    table.ForeignKey(
                        name: "FK_PriceHistories_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Address",
                table: "Listings",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Bedrooms",
                table: "Listings",
                column: "Bedrooms");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City",
                table: "Listings",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_Bedrooms",
                table: "Listings",
                columns: new[] { "City", "Bedrooms" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_LivingAreaM2",
                table: "Listings",
                columns: new[] { "City", "LivingAreaM2" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_City_Price",
                table: "Listings",
                columns: new[] { "City", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ContextCompositeScore",
                table: "Listings",
                column: "ContextCompositeScore");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ContextSafetyScore",
                table: "Listings",
                column: "ContextSafetyScore");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_FundaId",
                table: "Listings",
                column: "FundaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_IsSoldOrRented",
                table: "Listings",
                column: "IsSoldOrRented");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_LastFundaFetchUtc",
                table: "Listings",
                column: "LastFundaFetchUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Latitude",
                table: "Listings",
                column: "Latitude");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_ListedDate",
                table: "Listings",
                column: "ListedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_LivingAreaM2",
                table: "Listings",
                column: "LivingAreaM2");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Longitude",
                table: "Listings",
                column: "Longitude");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_PostalCode",
                table: "Listings",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Price",
                table: "Listings",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_PropertyType",
                table: "Listings",
                column: "PropertyType");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Status",
                table: "Listings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_ListingId",
                table: "PriceHistories",
                column: "ListingId");
        }
    }
}
