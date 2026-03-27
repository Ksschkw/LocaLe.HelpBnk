using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaLe.EscrowApi.Migrations
{
    /// <inheritdoc />
    public partial class Initial_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_Users_UserId1",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserId1",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Wallets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Wallets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId1",
                table: "Wallets",
                column: "UserId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_Users_UserId1",
                table: "Wallets",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
