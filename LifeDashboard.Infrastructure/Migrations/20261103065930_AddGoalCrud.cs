using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeDashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Goals",
                newName: "GoalType");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Goals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Goals",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Goals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Goals] SET [Status] = CASE WHEN [IsCompleted] = 1 THEN 2 ELSE 1 END;");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Goals");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserId",
                table: "Goals",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Users_UserId",
                table: "Goals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Users_UserId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Goals_UserId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Goals");

            migrationBuilder.RenameColumn(
                name: "GoalType",
                table: "Goals",
                newName: "Type");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Goals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [Goals] SET [IsCompleted] = CASE WHEN [Status] = 2 THEN 1 ELSE 0 END;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Goals");
        }
    }
}
