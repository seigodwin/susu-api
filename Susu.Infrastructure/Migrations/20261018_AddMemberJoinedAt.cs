using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Susu.Infrastructure.Migrations
{
    public partial class AddMemberJoinedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                table: "Members",
                type: "timestamp without time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "Members");
        }
    }
}
