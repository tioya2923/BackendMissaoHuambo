using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarMoedaLojaEncomenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Moeda",
                table: "Lojas",
                type: "longtext",
                nullable: false,
                defaultValue: "AOA")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Moeda",
                table: "Encomendas",
                type: "longtext",
                nullable: false,
                defaultValue: "AOA")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Moeda",
                table: "Lojas");

            migrationBuilder.DropColumn(
                name: "Moeda",
                table: "Encomendas");
        }
    }
}
