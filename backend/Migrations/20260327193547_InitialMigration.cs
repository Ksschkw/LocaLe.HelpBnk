using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaLe.EscrowApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Escrows_BuyerId",
                table: "Escrows",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Escrows_ProviderId",
                table: "Escrows",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Escrows_Users_BuyerId",
                table: "Escrows",
                column: "BuyerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Escrows_Users_ProviderId",
                table: "Escrows",
                column: "ProviderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Escrows_Users_BuyerId",
                table: "Escrows");

            migrationBuilder.DropForeignKey(
                name: "FK_Escrows_Users_ProviderId",
                table: "Escrows");

            migrationBuilder.DropIndex(
                name: "IX_Escrows_BuyerId",
                table: "Escrows");

            migrationBuilder.DropIndex(
                name: "IX_Escrows_ProviderId",
                table: "Escrows");
        }
    }
}
