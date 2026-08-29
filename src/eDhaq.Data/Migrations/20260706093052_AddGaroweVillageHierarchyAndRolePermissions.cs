using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eDhaq.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGaroweVillageHierarchyAndRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubVillageId",
                table: "Addresses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VillageId",
                table: "Addresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Villages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Villages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Villages_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SubVillages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VillageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubVillages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubVillages_Villages_VillageId",
                        column: x => x.VillageId,
                        principalTable: "Villages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_SubVillageId",
                table: "Addresses",
                column: "SubVillageId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_VillageId",
                table: "Addresses",
                column: "VillageId");

            migrationBuilder.CreateIndex(
                name: "IX_SubVillages_VillageId_Name",
                table: "SubVillages",
                columns: new[] { "VillageId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Villages_CityId_Name",
                table: "Villages",
                columns: new[] { "CityId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_SubVillages_SubVillageId",
                table: "Addresses",
                column: "SubVillageId",
                principalTable: "SubVillages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Villages_VillageId",
                table: "Addresses",
                column: "VillageId",
                principalTable: "Villages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_SubVillages_SubVillageId",
                table: "Addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Villages_VillageId",
                table: "Addresses");

            migrationBuilder.DropTable(
                name: "SubVillages");

            migrationBuilder.DropTable(
                name: "Villages");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_SubVillageId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_VillageId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "SubVillageId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "VillageId",
                table: "Addresses");
        }
    }
}
