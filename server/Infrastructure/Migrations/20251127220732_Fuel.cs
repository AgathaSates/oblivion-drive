using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OblivionDrive.Infrastructure.Orm.Migrations
{
    /// <inheritdoc />
    public partial class Fuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fuelPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gasoline = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Gas = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Diesel = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Alcohol = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LastUpdate = table.Column<DateOnly>(type: "date", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuelPrices", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fuelPrices");
        }
    }
}
