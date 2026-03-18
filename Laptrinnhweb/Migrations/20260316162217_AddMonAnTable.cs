using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laptrinnhweb.Migrations
{
    /// <inheritdoc />
    public partial class AddMonAnTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonAns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenMon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonAns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatBans_BanAnId",
                table: "DatBans",
                column: "BanAnId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatBans_BanAns_BanAnId",
                table: "DatBans",
                column: "BanAnId",
                principalTable: "BanAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatBans_BanAns_BanAnId",
                table: "DatBans");

            migrationBuilder.DropTable(
                name: "MonAns");

            migrationBuilder.DropIndex(
                name: "IX_DatBans_BanAnId",
                table: "DatBans");
        }
    }
}
