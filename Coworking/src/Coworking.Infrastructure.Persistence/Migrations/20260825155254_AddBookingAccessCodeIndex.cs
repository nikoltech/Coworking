using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coworking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingAccessCodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "row_version",
                table: "Desks");

            migrationBuilder.DropColumn(
                name: "version",
                table: "Coworkings");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Desks",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Coworkings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "booking",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "ix_booking_access_code",
                table: "booking",
                column: "access_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_booking_access_code",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Desks");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Coworkings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "booking");

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "Desks",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "version",
                table: "Coworkings",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
