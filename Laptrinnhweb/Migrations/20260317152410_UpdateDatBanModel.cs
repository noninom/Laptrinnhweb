using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laptrinnhweb.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatBanModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrangThai",
                table: "DatBans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "DatBans");
        }
    }
}
