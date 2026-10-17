using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class ContactosEPesquisa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VisivelPesquisa",
                table: "Utilizadores",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ContactosUtilizador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SolicitanteId = table.Column<int>(type: "int", nullable: false),
                    DestinatarioId = table.Column<int>(type: "int", nullable: false),
                    Aceite = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactosUtilizador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactosUtilizador_Utilizadores_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactosUtilizador_Utilizadores_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ContactosUtilizador_DestinatarioId",
                table: "ContactosUtilizador",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactosUtilizador_SolicitanteId_DestinatarioId",
                table: "ContactosUtilizador",
                columns: new[] { "SolicitanteId", "DestinatarioId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactosUtilizador");

            migrationBuilder.DropColumn(
                name: "VisivelPesquisa",
                table: "Utilizadores");
        }
    }
}
