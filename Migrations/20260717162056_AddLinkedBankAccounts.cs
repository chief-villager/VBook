using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookkeeping.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedBankAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "linked_bank_accounts",
                schema: "transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InstitutionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccountNumberMasked = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linked_bank_accounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_linked_bank_accounts_BusinessId_ExternalAccountId",
                schema: "transactions",
                table: "linked_bank_accounts",
                columns: new[] { "BusinessId", "ExternalAccountId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linked_bank_accounts",
                schema: "transactions");
        }
    }
}
