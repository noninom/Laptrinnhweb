using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laptrinnhweb.Migrations
{
    /// <inheritdoc />
    public partial class AddDatBanIdToChiTiet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DatBanId",
                table: "ChiTietDatMons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatMons_DatBanId",
                table: "ChiTietDatMons",
                column: "DatBanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDatMons_DatBans_DatBanId",
                table: "ChiTietDatMons",
                column: "DatBanId",
                principalTable: "DatBans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDatMons_DatBans_DatBanId",
                table: "ChiTietDatMons");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietDatMons_DatBanId",
                table: "ChiTietDatMons");

            migrationBuilder.DropColumn(
                name: "DatBanId",
                table: "ChiTietDatMons");
        }
    }
}
