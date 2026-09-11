using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaudeAutomationDemo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderProcessDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessDate",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessDate",
                table: "Orders");
        }
    }
}
