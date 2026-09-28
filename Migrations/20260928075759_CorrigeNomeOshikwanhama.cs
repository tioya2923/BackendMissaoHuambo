using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class CorrigeNomeOshikwanhama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Idiomas",
                keyColumn: "Id",
                keyValue: 5,
                column: "Nome",
                value: "Oshikwanhama");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Idiomas",
                keyColumn: "Id",
                keyValue: 5,
                column: "Nome",
                value: "Otchikwanyama");
        }
    }
}
