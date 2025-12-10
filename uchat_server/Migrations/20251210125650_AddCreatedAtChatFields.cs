using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uchat_server.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtChatFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "chats",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "chats");
        }
    }
}
