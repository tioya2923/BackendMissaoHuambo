using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class MakeCatecismoPtTopicoOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt");

            migrationBuilder.AlterColumn<int>(
                name: "CatecismoPtTopicoId",
                table: "CatecismosPt",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt",
                column: "CatecismoPtTopicoId",
                principalTable: "CatecismoPtTopicos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt");

            migrationBuilder.AlterColumn<int>(
                name: "CatecismoPtTopicoId",
                table: "CatecismosPt",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismosPt_CatecismoPtTopicos_CatecismoPtTopicoId",
                table: "CatecismosPt",
                column: "CatecismoPtTopicoId",
                principalTable: "CatecismoPtTopicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
