using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaLe.EscrowApi.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCategoryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId1",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_JobId1",
                table: "Bookings",
                column: "JobId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Jobs_JobId1",
                table: "Bookings",
                column: "JobId1",
                principalTable: "Jobs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Jobs_JobId1",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_JobId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobId1",
                table: "Bookings");
        }
    }
}
