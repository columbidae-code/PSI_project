using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace beveikZinojau.lt.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedQuestionTextTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GameId",
                table: "QuestionTexts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTexts_GameId",
                table: "QuestionTexts",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionTexts_Games_GameId",
                table: "QuestionTexts",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionTexts_Games_GameId",
                table: "QuestionTexts");

            migrationBuilder.DropIndex(
                name: "IX_QuestionTexts_GameId",
                table: "QuestionTexts");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "QuestionTexts");
        }
    }
}
