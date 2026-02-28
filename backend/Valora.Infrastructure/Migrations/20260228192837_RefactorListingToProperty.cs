using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorListingToProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingComments");

            migrationBuilder.DropTable(
                name: "PriceHistories");

            migrationBuilder.DropTable(
                name: "SavedListings");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BagId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LivingAreaM2 = table.Column<int>(type: "int", nullable: true),
                    YearBuilt = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    SaveCount = table.Column<int>(type: "int", nullable: true),
                    ContextCompositeScore = table.Column<double>(type: "float", nullable: true),
                    ContextSafetyScore = table.Column<double>(type: "float", nullable: true),
                    ContextSocialScore = table.Column<double>(type: "float", nullable: true),
                    ContextAmenitiesScore = table.Column<double>(type: "float", nullable: true),
                    ContextEnvironmentScore = table.Column<double>(type: "float", nullable: true),
                    ContextReport = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.CheckConstraint("CK_Property_ContextAmenitiesScore", "[ContextAmenitiesScore] >= 0 AND [ContextAmenitiesScore] <= 100");
                    table.CheckConstraint("CK_Property_ContextCompositeScore", "[ContextCompositeScore] >= 0 AND [ContextCompositeScore] <= 100");
                    table.CheckConstraint("CK_Property_ContextEnvironmentScore", "[ContextEnvironmentScore] >= 0 AND [ContextEnvironmentScore] <= 100");
                    table.CheckConstraint("CK_Property_ContextSafetyScore", "[ContextSafetyScore] >= 0 AND [ContextSafetyScore] <= 100");
                    table.CheckConstraint("CK_Property_ContextSocialScore", "[ContextSocialScore] >= 0 AND [ContextSocialScore] <= 100");
                });

            migrationBuilder.CreateTable(
                name: "SavedProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedProperties_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedProperties_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedProperties_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedPropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reactions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyComments_PropertyComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "PropertyComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyComments_SavedProperties_SavedPropertyId",
                        column: x => x.SavedPropertyId,
                        principalTable: "SavedProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_BagId",
                table: "Properties",
                column: "BagId",
                unique: true,
                filter: "[BagId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_City",
                table: "Properties",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ContextCompositeScore",
                table: "Properties",
                column: "ContextCompositeScore");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ContextSafetyScore",
                table: "Properties",
                column: "ContextSafetyScore");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Latitude",
                table: "Properties",
                column: "Latitude");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Latitude_Longitude",
                table: "Properties",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_Longitude",
                table: "Properties",
                column: "Longitude");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_PostalCode",
                table: "Properties",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyComments_ParentCommentId",
                table: "PropertyComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyComments_SavedPropertyId",
                table: "PropertyComments",
                column: "SavedPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyComments_UserId",
                table: "PropertyComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedProperties_AddedByUserId",
                table: "SavedProperties",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedProperties_PropertyId",
                table: "SavedProperties",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedProperties_WorkspaceId_PropertyId",
                table: "SavedProperties",
                columns: new[] { "WorkspaceId", "PropertyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyComments");

            migrationBuilder.DropTable(
                name: "SavedProperties");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BalconyM2 = table.Column<int>(type: "int", nullable: true),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    BrochureUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrokerAssociationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BrokerLogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrokerOfficeId = table.Column<int>(type: "int", nullable: true),
                    BrokerPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CVBoilerBrand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CVBoilerYear = table.Column<int>(type: "int", nullable: true),
                    CadastralDesignation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConstructionPeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContextAmenitiesScore = table.Column<double>(type: "float", nullable: true),
                    ContextCompositeScore = table.Column<double>(type: "float", nullable: true),
                    ContextEnvironmentScore = table.Column<double>(type: "float", nullable: true),
                    ContextReport = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextSafetyScore = table.Column<double>(type: "float", nullable: true),
                    ContextSocialScore = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnergyLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExternalStorageM2 = table.Column<int>(type: "int", nullable: true),
                    Features = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiberAvailable = table.Column<bool>(type: "bit", nullable: true),
                    FloorPlanUrls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FundaId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GardenM2 = table.Column<int>(type: "int", nullable: true),
                    GardenOrientation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HasGarage = table.Column<bool>(type: "bit", nullable: false),
                    HeatingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InsulationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSoldOrRented = table.Column<bool>(type: "bit", nullable: false),
                    Labels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastFundaFetchUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    ListedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LivingAreaM2 = table.Column<int>(type: "int", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    NeighborhoodAvgPriceM2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NeighborhoodPopulation = table.Column<int>(type: "int", nullable: true),
                    NumberOfFloors = table.Column<int>(type: "int", nullable: true),
                    OpenHouseDates = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnershipType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ParkingType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlotAreaM2 = table.Column<int>(type: "int", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PropertyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RoofType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SaveCount = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VVEContribution = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: true),
                    VirtualTourUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VolumeM3 = table.Column<int>(type: "int", nullable: true),
                    YearBuilt = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.CheckConstraint("CK_Listing_Bedrooms", "[Bedrooms] >= 0");
                    table.CheckConstraint("CK_Listing_ContextAmenitiesScore", "[ContextAmenitiesScore] >= 0 AND [ContextAmenitiesScore] <= 100");
                    table.CheckConstraint("CK_Listing_ContextCompositeScore", "[ContextCompositeScore] >= 0 AND [ContextCompositeScore] <= 100");
                    table.CheckConstraint("CK_Listing_ContextEnvironmentScore", "[ContextEnvironmentScore] >= 0 AND [ContextEnvironmentScore] <= 100");
                    table.CheckConstraint("CK_Listing_ContextSafetyScore", "[ContextSafetyScore] >= 0 AND [ContextSafetyScore] <= 100");
                    table.CheckConstraint("CK_Listing_ContextSocialScore", "[ContextSocialScore] >= 0 AND [ContextSocialScore] <= 100");
                    table.CheckConstraint("CK_Listing_LivingAreaM2", "[LivingAreaM2] > 0");
                    table.CheckConstraint("CK_Listing_Price", "[Price] > 0");
                });

            migrationBuilder.CreateTable(
                name: "PriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistories", x => x.Id);
                    table.CheckConstraint("CK_PriceHistory_Price", "[Price] > 0");
                    table.ForeignKey(
                        name: "FK_PriceHistories_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedListings_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SavedListings_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedListings_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListingComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SavedListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reactions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ListingComments_ListingComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "ListingComments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ListingComments_SavedListings_SavedListingId",
                        column: x => x.SavedListingId,
                        principalTable: "SavedListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingComments_ParentCommentId",
                table: "ListingComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingComments_SavedListingId",
                table: "ListingComments",
                column: "SavedListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ListingComments_UserId",
                table: "ListingComments",
                column: "UserId");

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
                name: "IX_Listings_City_LastFundaFetchUtc_Price",
                table: "Listings",
                columns: new[] { "City", "LastFundaFetchUtc", "Price" });

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
                name: "IX_Listings_Latitude_Longitude",
                table: "Listings",
                columns: new[] { "Latitude", "Longitude" });

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

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_RecordedAt",
                table: "PriceHistories",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_AddedByUserId",
                table: "SavedListings",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_ListingId",
                table: "SavedListings",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_WorkspaceId_CreatedAt",
                table: "SavedListings",
                columns: new[] { "WorkspaceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedListings_WorkspaceId_ListingId",
                table: "SavedListings",
                columns: new[] { "WorkspaceId", "ListingId" },
                unique: true);
        }
    }
}
