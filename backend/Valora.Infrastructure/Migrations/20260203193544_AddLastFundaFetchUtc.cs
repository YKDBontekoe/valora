using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Valora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastFundaFetchUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentName",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BalconyM2",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrochureUrl",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrokerAssociationCode",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrokerLogoUrl",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BrokerOfficeId",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrokerPhone",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CVBoilerBrand",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVBoilerYear",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CadastralDesignation",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConstructionPeriod",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnergyLabel",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalStorageM2",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "Listings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "FiberAvailable",
                table: "Listings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FloorPlanUrls",
                table: "Listings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GardenM2",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GardenOrientation",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasGarage",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HeatingType",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrls",
                table: "Listings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InsulationType",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSoldOrRented",
                table: "Listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Labels",
                table: "Listings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFundaFetchUtc",
                table: "Listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Listings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Listings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NeighborhoodAvgPriceM2",
                table: "Listings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeighborhoodPopulation",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfFloors",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenHouseDates",
                table: "Listings",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnershipType",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParkingType",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublicationDate",
                table: "Listings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoofType",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaveCount",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VVEContribution",
                table: "Listings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VirtualTourUrl",
                table: "Listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolumeM3",
                table: "Listings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearBuilt",
                table: "Listings",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentName",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BalconyM2",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BrochureUrl",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BrokerAssociationCode",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BrokerLogoUrl",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BrokerOfficeId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "BrokerPhone",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "CVBoilerBrand",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "CVBoilerYear",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "CadastralDesignation",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ConstructionPeriod",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "EnergyLabel",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ExternalStorageM2",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "FiberAvailable",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "FloorPlanUrls",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "GardenM2",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "GardenOrientation",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "HasGarage",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "HeatingType",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "InsulationType",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "IsSoldOrRented",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Labels",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "LastFundaFetchUtc",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "NeighborhoodAvgPriceM2",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "NeighborhoodPopulation",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "NumberOfFloors",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "OpenHouseDates",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ParkingType",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PublicationDate",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "RoofType",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SaveCount",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "VVEContribution",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "VirtualTourUrl",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "VolumeM3",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "YearBuilt",
                table: "Listings");
        }
    }
}
