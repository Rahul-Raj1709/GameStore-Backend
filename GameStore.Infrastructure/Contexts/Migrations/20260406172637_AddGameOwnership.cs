using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStore.Infrastructure.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddGameOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Games_OwnerId",
                table: "Games",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Users_OwnerId",
                table: "Games",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Users_OwnerId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_OwnerId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Games");
        }
    }
}
