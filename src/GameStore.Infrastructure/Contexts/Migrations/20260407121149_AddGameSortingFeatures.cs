using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameStore.Infrastructure.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSortingFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalLikes",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "AddedAt",
                table: "Games",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Games",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "27675df4-3a81-48b9-a446-7af04edbfee7");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "0df1e01a-1d4d-47b0-a13a-05206c911d93");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "cae79452-1249-428a-a6b2-26366fcf780e");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Games",
                newName: "AddedAt");

            migrationBuilder.AddColumn<int>(
                name: "TotalLikes",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "3cb2943b-f887-47fc-ad26-c08d68d036a6");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "13148cec-deeb-4a41-8a17-effc3e0ffe30");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3",
                column: "ConcurrencyStamp",
                value: "e328fae3-da23-49e1-8279-c0e11907fdc7");
        }
    }
}
