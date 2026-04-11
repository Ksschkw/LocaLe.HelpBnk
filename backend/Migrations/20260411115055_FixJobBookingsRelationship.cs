using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaLe.EscrowApi.Migrations
{
    /// <inheritdoc />
    public partial class FixJobBookingsRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Jobs_JobId1",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_JobId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "JobId1",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
