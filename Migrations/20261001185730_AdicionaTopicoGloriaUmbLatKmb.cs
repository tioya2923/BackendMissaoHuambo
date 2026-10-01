using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTopicoGloriaUmbLatKmb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O tópico "Glória" já existia só em Português (Id 99). Acrescenta o mesmo
            // tópico (vazio, sem cânticos ainda) em Umbundu, Latim e Kimbundu para que a
            // estrutura de tópicos fique igual em todos os idiomas que já têm cânticos.
            migrationBuilder.InsertData(
                table: "Topicos",
                columns: new[] { "Id", "Nome", "Slug", "IdiomaId" },
                values: new object[,]
                {
                    { 200, "Glória", "Gloria", 2 }, // Umbundu
                    { 201, "Glória", "Gloria", 3 }, // Latim
                    { 202, "Glória", "Gloria", 4 }, // Kimbundu
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Topicos", keyColumn: "Id", keyValue: 200);
            migrationBuilder.DeleteData(table: "Topicos", keyColumn: "Id", keyValue: 201);
            migrationBuilder.DeleteData(table: "Topicos", keyColumn: "Id", keyValue: 202);
        }
    }
}
