using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSubtopicosCatecismo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "CatecismoPtTopicos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatecismoPtTopicos_ParentId",
                table: "CatecismoPtTopicos",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CatecismoPtTopicos_CatecismoPtTopicos_ParentId",
                table: "CatecismoPtTopicos",
                column: "ParentId",
                principalTable: "CatecismoPtTopicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatecismoPtTopicos_CatecismoPtTopicos_ParentId",
                table: "CatecismoPtTopicos");

            migrationBuilder.DropIndex(
                name: "IX_CatecismoPtTopicos_ParentId",
                table: "CatecismoPtTopicos");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "CatecismoPtTopicos");
        }
    }
}
