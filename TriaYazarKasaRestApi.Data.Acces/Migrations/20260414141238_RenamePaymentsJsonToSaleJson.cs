using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriaYazarKasaRestApi.Data.Acces.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentsJsonToSaleJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentsJson",
                table: "BekoBasketOperations",
                newName: "SaleJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SaleJson",
                table: "BekoBasketOperations",
                newName: "PaymentsJson");
        }
    }
}
