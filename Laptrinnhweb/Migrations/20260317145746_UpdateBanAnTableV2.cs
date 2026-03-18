using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laptrinnhweb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBanAnTableV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SucChua",
                table: "BanAns",
                newName: "SoChoNgoi");

            migrationBuilder.AlterColumn<string>(
                name: "TenMon",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Loai",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "HinhAnh",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ChiTietDatMons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BanAnId = table.Column<int>(type: "int", nullable: false),
                    MonAnId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDatMons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietDatMons_BanAns_BanAnId",
                        column: x => x.BanAnId,
                        principalTable: "BanAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDatMons_MonAns_MonAnId",
                        column: x => x.MonAnId,
                        principalTable: "MonAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatMons_BanAnId",
                table: "ChiTietDatMons",
                column: "BanAnId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatMons_MonAnId",
                table: "ChiTietDatMons",
                column: "MonAnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDatMons");

            migrationBuilder.RenameColumn(
                name: "SoChoNgoi",
                table: "BanAns",
                newName: "SucChua");

            migrationBuilder.AlterColumn<string>(
                name: "TenMon",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Loai",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HinhAnh",
                table: "MonAns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
