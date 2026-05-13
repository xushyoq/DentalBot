using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTelegramId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Employees\" ALTER COLUMN \"TelegramId\" TYPE bigint USING \"TelegramId\"::bigint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TelegramId",
                table: "Employees",
                type: "text",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
