using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookkeeping.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceTemplateentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "invoices");

            migrationBuilder.CreateTable(
                name: "invoice_templates",
                schema: "identity",
                columns: table => new
                {
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Terms = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_templates", x => x.BusinessId);
                    table.ForeignKey(
                        name: "FK_invoice_templates_businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "identity",
                        principalTable: "businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BillTo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line_items",
                schema: "invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_line_items_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "invoices",
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_InvoiceId",
                schema: "invoices",
                table: "invoice_line_items",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_BusinessId_InvoiceNumber",
                schema: "invoices",
                table: "invoices",
                columns: new[] { "BusinessId", "InvoiceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_line_items",
                schema: "invoices");

            migrationBuilder.DropTable(
                name: "invoice_templates",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "invoices");
        }
    }
}
