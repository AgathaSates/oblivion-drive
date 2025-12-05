using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OblivionDrive.Infrastructure.Orm.Migrations
{
    /// <inheritdoc />
    public partial class vehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicensePlate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FuelType = table.Column<int>(type: "int", nullable: false),
                    FuelTankCapacityInLiters = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    VehicleGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhotoBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleGroups_VehicleGroupId",
                        column: x => x.VehicleGroupId,
                        principalTable: "VehicleGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleGroupId",
                table: "Vehicles",
                column: "VehicleGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
