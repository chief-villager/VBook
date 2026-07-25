using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookkeeping.Migrations
{
    /// <inheritdoc />
    public partial class AddStagedBankTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staged_bank_transactions",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ProviderCategory = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SuggestedType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staged_bank_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staged_bank_transactions_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "transactions",
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_staged_bank_transactions_BusinessId_ExternalId",
                schema: "transactions",
                table: "staged_bank_transactions",
                columns: new[] { "BusinessId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staged_bank_transactions_CategoryId",
                schema: "transactions",
                table: "staged_bank_transactions",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staged_bank_transactions",
                schema: "transactions");
        }
    }
}
