using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriaYazarKasaRestApi.Data.Acces.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqliteSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BekoBasketOperations",
                columns: table => new
                {
                    BasketId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OperationId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StatusCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StatusMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsFinal = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReceiptResultJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BekoBasketOperations", x => x.BasketId);
                });

            migrationBuilder.CreateTable(
                name: "HuginOperationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConnectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperationName = table.Column<string>(type: "TEXT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HuginOperationLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BekoBasketOperations");

            migrationBuilder.DropTable(
                name: "HuginOperationLogs");
        }
    }
}
