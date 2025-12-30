using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MissaoBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixUmbunduRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CanticosUmb_TopicosUmb_TopicoId",
                table: "CanticosUmb");

            migrationBuilder.AddForeignKey(
                name: "FK_CanticosUmb_TopicosUmb_TopicoId",
                table: "CanticosUmb",
                column: "TopicoId",
                principalTable: "TopicosUmb",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CanticosUmb_TopicosUmb_TopicoId",
                table: "CanticosUmb");

            migrationBuilder.AddForeignKey(
                name: "FK_CanticosUmb_TopicosUmb_TopicoId",
                table: "CanticosUmb",
                column: "TopicoId",
                principalTable: "TopicosUmb",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
