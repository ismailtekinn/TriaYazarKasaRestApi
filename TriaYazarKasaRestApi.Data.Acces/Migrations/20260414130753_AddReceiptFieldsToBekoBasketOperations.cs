using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriaYazarKasaRestApi.Data.Acces.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptFieldsToBekoBasketOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentsJson",
                table: "BekoBasketOperations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptNo",
                table: "BekoBasketOperations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uuid",
                table: "BekoBasketOperations",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZNo",
                table: "BekoBasketOperations",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentsJson",
                table: "BekoBasketOperations");

            migrationBuilder.DropColumn(
                name: "ReceiptNo",
                table: "BekoBasketOperations");

            migrationBuilder.DropColumn(
                name: "Uuid",
                table: "BekoBasketOperations");

            migrationBuilder.DropColumn(
                name: "ZNo",
                table: "BekoBasketOperations");
        }
    }
}
