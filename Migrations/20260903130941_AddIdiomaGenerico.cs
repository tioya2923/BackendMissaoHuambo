using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddIdiomaGenerico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Topicos_Slug",
                table: "Topicos");

            migrationBuilder.DropIndex(
                name: "IX_Canticos_Slug",
                table: "Canticos");

            // defaultValue: 1 = "pt" (Português) — todo o conteúdo já existente nestas
            // tabelas era exclusivamente em português, por isso fica automaticamente
            // associado ao idioma Português assim que a coluna é criada.
            migrationBuilder.AddColumn<int>(
                name: "IdiomaId",
                table: "Topicos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "IdiomaId",
                table: "CatecismosPt",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "IdiomaId",
                table: "CatecismoPtTopicos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "IdiomaId",
                table: "Canticos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Idiomas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Idiomas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Idiomas",
                columns: new[] { "Id", "Ativo", "Codigo", "Nome", "Ordem" },
                values: new object[,]
                {
                    { 1, true, "pt", "Português", 1 },
                    { 2, true, "umb", "Umbundu", 2 },
                    { 3, true, "lat", "Latim", 3 },
                    { 4, true, "kmb", "Kimbundu", 4 },
                    { 5, true, "otc", "Otchikwanyama", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Topicos_IdiomaId_Slug",
                table: "Topicos",
                columns: new[] { "IdiomaId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatecismosPt_IdiomaId",
                table: "CatecismosPt",
                column: "IdiomaId");

            migrationBuilder.CreateIndex(
                name: "IX_CatecismoPtTopicos_IdiomaId",
                table: "CatecismoPtTopicos",
                column: "IdiomaId");

            migrationBuilder.CreateIndex(
                name: "IX_Canticos_IdiomaId_Slug",
                table: "Canticos",
                columns: new[] { "IdiomaId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Idiomas_Codigo",
                table: "Idiomas",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Canticos_Idiomas_IdiomaId",
                table: "Canticos",
                column: "IdiomaId",
                principalTable: "Idiomas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismoPtTopicos_Idiomas_IdiomaId",
                table: "CatecismoPtTopicos",
                column: "IdiomaId",
                principalTable: "Idiomas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosPt_Idiomas_IdiomaId",
                table: "CatecismosPt",
                column: "IdiomaId",
                principalTable: "Idiomas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Topicos_Idiomas_IdiomaId",
                table: "Topicos",
                column: "IdiomaId",
                principalTable: "Idiomas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Canticos_Idiomas_IdiomaId",
                table: "Canticos");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismoPtTopicos_Idiomas_IdiomaId",
                table: "CatecismoPtTopicos");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosPt_Idiomas_IdiomaId",
                table: "CatecismosPt");

            migrationBuilder.DropForeignKey(
                name: "FK_Topicos_Idiomas_IdiomaId",
                table: "Topicos");

            migrationBuilder.DropTable(
                name: "Idiomas");

            migrationBuilder.DropIndex(
                name: "IX_Topicos_IdiomaId_Slug",
                table: "Topicos");

            migrationBuilder.DropIndex(
                name: "IX_CatecismosPt_IdiomaId",
                table: "CatecismosPt");

            migrationBuilder.DropIndex(
                name: "IX_CatecismoPtTopicos_IdiomaId",
                table: "CatecismoPtTopicos");

            migrationBuilder.DropIndex(
                name: "IX_Canticos_IdiomaId_Slug",
                table: "Canticos");

            migrationBuilder.DropColumn(
                name: "IdiomaId",
                table: "Topicos");

            migrationBuilder.DropColumn(
                name: "IdiomaId",
                table: "CatecismosPt");

            migrationBuilder.DropColumn(
                name: "IdiomaId",
                table: "CatecismoPtTopicos");

            migrationBuilder.DropColumn(
                name: "IdiomaId",
                table: "Canticos");

            migrationBuilder.CreateIndex(
                name: "IX_Topicos_Slug",
                table: "Topicos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Canticos_Slug",
                table: "Canticos",
                column: "Slug",
                unique: true);
        }
    }
}
