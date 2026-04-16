using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadRoofer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVapiAssistantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VapiAssistantId",
                table: "ServiceLocations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VapiPhoneNumberId",
                table: "ServiceLocations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ServiceLocations",
                keyColumn: "Id",
                keyValue: new Guid("22222222-0000-0000-0000-000000000002"),
                columns: new[] { "VapiAssistantId", "VapiPhoneNumberId" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VapiAssistantId",
                table: "ServiceLocations");

            migrationBuilder.DropColumn(
                name: "VapiPhoneNumberId",
                table: "ServiceLocations");
        }
    }
}
