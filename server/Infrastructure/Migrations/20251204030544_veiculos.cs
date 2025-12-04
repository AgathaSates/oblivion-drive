using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OblivionDrive.Infrastructure.Orm.Migrations
{
    /// <inheritdoc />
    public partial class veiculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VehicleGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyPlan_DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyPlan_PricePerKilometer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ControlledPlan_DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ControlledPlan_ExtraPricePerKilometer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreePlan_DailyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPlans_VehicleGroups_VehicleGroupId",
                        column: x => x.VehicleGroupId,
                        principalTable: "VehicleGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlans_VehicleGroupId",
                table: "BillingPlans",
                column: "VehicleGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingPlans");

            migrationBuilder.DropTable(
                name: "VehicleGroups");
        }
    }
}
