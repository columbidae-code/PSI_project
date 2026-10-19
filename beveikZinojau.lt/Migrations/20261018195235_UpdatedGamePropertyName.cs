using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace beveikZinojau.lt.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedGamePropertyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "gameMode",
                table: "Games",
                newName: "GameMode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GameMode",
                table: "Games",
                newName: "gameMode");
        }
    }
}
