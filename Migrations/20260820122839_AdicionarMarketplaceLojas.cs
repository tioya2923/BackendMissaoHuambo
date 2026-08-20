using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarMarketplaceLojas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Produtos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "GrupoId",
                table: "Encomendas",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Encomendas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Lojas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Morada = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Categoria = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    InfoPagamento = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Aprovada = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataRegisto = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lojas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_LojaId",
                table: "Produtos",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Encomendas_LojaId",
                table: "Encomendas",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Lojas_Email",
                table: "Lojas",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Encomendas_Lojas_LojaId",
                table: "Encomendas",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Lojas_LojaId",
                table: "Produtos",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Encomendas_Lojas_LojaId",
                table: "Encomendas");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Lojas_LojaId",
                table: "Produtos");

            migrationBuilder.DropTable(
                name: "Lojas");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_LojaId",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_Encomendas_LojaId",
                table: "Encomendas");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "GrupoId",
                table: "Encomendas");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Encomendas");
        }
    }
}
