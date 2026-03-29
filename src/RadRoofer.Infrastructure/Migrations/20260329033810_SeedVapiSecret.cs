using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadRoofer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedVapiSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ServiceLocations",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000002"),
                column: "VapiSecret",
                value: "dev-vapi-secret");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ServiceLocations",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000002"),
                column: "VapiSecret",
                value: null);
        }
    }
}
