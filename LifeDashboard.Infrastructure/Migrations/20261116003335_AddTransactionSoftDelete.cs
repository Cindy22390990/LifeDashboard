using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedReason",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Transactions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeletedReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Transactions");
        }
    }
}
