using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaLe.EscrowApi.Migrations
{
    /// <inheritdoc />
    public partial class GuestVouchRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "VoucherId",
                table: "Vouches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "GuestIpAddress",
                table: "Vouches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "Vouches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "Vouches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestUserAgent",
                table: "Vouches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDiscoveryAdminOverridden",
                table: "Services",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestIpAddress",
                table: "Vouches");

            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "Vouches");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "Vouches");

            migrationBuilder.DropColumn(
                name: "GuestUserAgent",
                table: "Vouches");

            migrationBuilder.DropColumn(
                name: "IsDiscoveryAdminOverridden",
                table: "Services");

            migrationBuilder.AlterColumn<Guid>(
                name: "VoucherId",
                table: "Vouches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
