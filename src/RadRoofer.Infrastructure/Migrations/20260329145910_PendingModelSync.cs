using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadRoofer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000003"),
                column: "PasswordHash",
                value: "$2a$11$bMX0Dbive85wi3hnMEP/EeYS1Mb9EFHj/JiVzBlz6vrcek02.4M86");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000003"),
                column: "PasswordHash",
                value: "$2a$11$jry2mWIjtIuL4m9thqcb3.DHGWiDh3sHYsLRqiJRlCFmQ/K8qrG5S");
        }
    }
}
