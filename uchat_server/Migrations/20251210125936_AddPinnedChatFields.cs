using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uchat_server.Migrations
{
    /// <inheritdoc />
    public partial class AddPinnedChatFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_chat_pinned",
                table: "chat_members",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "pinned_at",
                table: "chat_members",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_chat_pinned",
                table: "chat_members");

            migrationBuilder.DropColumn(
                name: "pinned_at",
                table: "chat_members");
        }
    }
}
