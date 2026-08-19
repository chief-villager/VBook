using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookkeeping.Migrations
{
    /// <inheritdoc />
    public partial class invoiceentityupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                schema: "invoices",
                table: "invoices",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "invoices",
                table: "invoices",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                schema: "invoices",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "invoices",
                table: "invoices");
        }
    }
}
