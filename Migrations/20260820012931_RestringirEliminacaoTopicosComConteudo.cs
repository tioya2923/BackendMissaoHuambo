using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class RestringirEliminacaoTopicosComConteudo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Canticos_Topicos_TopicoId",
                table: "Canticos");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosLat_CatecismoLatTopicos_CatecismoLatTopicoId",
                table: "CatecismosLat");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosUb_CatecismoUbTopicos_CatecismoUbTopicoId",
                table: "CatecismosUb");

            migrationBuilder.AddForeignKey(
                name: "FK_Canticos_Topicos_TopicoId",
                table: "Canticos",
                column: "TopicoId",
                principalTable: "Topicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosLat_CatecismoLatTopicos_CatecismoLatTopicoId",
                table: "CatecismosLat",
                column: "CatecismoLatTopicoId",
                principalTable: "CatecismoLatTopicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt",
                column: "CatecismoPtTopicoId",
                principalTable: "CatecismoPtTopicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosUb_CatecismoUbTopicos_CatecismoUbTopicoId",
                table: "CatecismosUb",
                column: "CatecismoUbTopicoId",
                principalTable: "CatecismoUbTopicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Canticos_Topicos_TopicoId",
                table: "Canticos");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosLat_CatecismoLatTopicos_CatecismoLatTopicoId",
                table: "CatecismosLat");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt");

            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosUb_CatecismoUbTopicos_CatecismoUbTopicoId",
                table: "CatecismosUb");

            migrationBuilder.AddForeignKey(
                name: "FK_Canticos_Topicos_TopicoId",
                table: "Canticos",
                column: "TopicoId",
                principalTable: "Topicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosLat_CatecismoLatTopicos_CatecismoLatTopicoId",
                table: "CatecismosLat",
                column: "CatecismoLatTopicoId",
                principalTable: "CatecismoLatTopicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt",
                column: "CatecismoPtTopicoId",
                principalTable: "CatecismoPtTopicos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosUb_CatecismoUbTopicos_CatecismoUbTopicoId",
                table: "CatecismosUb",
                column: "CatecismoUbTopicoId",
                principalTable: "CatecismoUbTopicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
