using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class PartilhasEntreUtilizadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Utilizadores",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AudiosPartilha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DonoId = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Dados = table.Column<byte[]>(type: "LONGBLOB", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudiosPartilha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudiosPartilha_Utilizadores_DonoId",
                        column: x => x.DonoId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BloqueiosUtilizador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false),
                    BloqueadoId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueiosUtilizador", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloqueiosUtilizador_Utilizadores_BloqueadoId",
                        column: x => x.BloqueadoId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BloqueiosUtilizador_Utilizadores_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DenunciasPartilha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DenuncianteId = table.Column<int>(type: "int", nullable: true),
                    RemetenteId = table.Column<int>(type: "int", nullable: true),
                    RemetenteNome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemetenteCodigo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Titulo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Letra = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Motivo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenunciasPartilha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DenunciasPartilha_Utilizadores_DenuncianteId",
                        column: x => x.DenuncianteId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DenunciasPartilha_Utilizadores_RemetenteId",
                        column: x => x.RemetenteId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PartilhasCanticos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RemetenteId = table.Column<int>(type: "int", nullable: false),
                    DestinatarioId = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Letra = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Momento = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AudioId = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LidoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartilhasCanticos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartilhasCanticos_AudiosPartilha_AudioId",
                        column: x => x.AudioId,
                        principalTable: "AudiosPartilha",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PartilhasCanticos_Utilizadores_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartilhasCanticos_Utilizadores_RemetenteId",
                        column: x => x.RemetenteId,
                        principalTable: "Utilizadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Utilizadores_Codigo",
                table: "Utilizadores",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AudiosPartilha_DonoId",
                table: "AudiosPartilha",
                column: "DonoId");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueiosUtilizador_BloqueadoId",
                table: "BloqueiosUtilizador",
                column: "BloqueadoId");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueiosUtilizador_UtilizadorId_BloqueadoId",
                table: "BloqueiosUtilizador",
                columns: new[] { "UtilizadorId", "BloqueadoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasPartilha_DenuncianteId",
                table: "DenunciasPartilha",
                column: "DenuncianteId");

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasPartilha_RemetenteId",
                table: "DenunciasPartilha",
                column: "RemetenteId");

            migrationBuilder.CreateIndex(
                name: "IX_PartilhasCanticos_AudioId",
                table: "PartilhasCanticos",
                column: "AudioId");

            migrationBuilder.CreateIndex(
                name: "IX_PartilhasCanticos_DestinatarioId_CriadoEm",
                table: "PartilhasCanticos",
                columns: new[] { "DestinatarioId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_PartilhasCanticos_RemetenteId",
                table: "PartilhasCanticos",
                column: "RemetenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloqueiosUtilizador");

            migrationBuilder.DropTable(
                name: "DenunciasPartilha");

            migrationBuilder.DropTable(
                name: "PartilhasCanticos");

            migrationBuilder.DropTable(
                name: "AudiosPartilha");

            migrationBuilder.DropIndex(
                name: "IX_Utilizadores_Codigo",
                table: "Utilizadores");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Utilizadores");
        }
    }
}
