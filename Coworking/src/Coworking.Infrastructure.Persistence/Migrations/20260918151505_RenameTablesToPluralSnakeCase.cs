using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coworking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameTablesToPluralSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booking_desk_desk_id",
                table: "booking");

            migrationBuilder.DropPrimaryKey(
                name: "pk_booking",
                table: "booking");

            migrationBuilder.RenameTable(
                name: "Desks",
                newName: "desks");

            migrationBuilder.RenameTable(
                name: "Coworkings",
                newName: "coworkings");

            migrationBuilder.RenameTable(
                name: "booking",
                newName: "bookings");

            migrationBuilder.RenameIndex(
                name: "ix_booking_created_at",
                table: "bookings",
                newName: "ix_bookings_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_booking_access_code",
                table: "bookings",
                newName: "ix_bookings_access_code");

            migrationBuilder.AddPrimaryKey(
                name: "pk_bookings",
                table: "bookings",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_desk_desk_id",
                table: "bookings",
                column: "desk_id",
                principalTable: "desks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bookings_desk_desk_id",
                table: "bookings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_bookings",
                table: "bookings");

            migrationBuilder.RenameTable(
                name: "desks",
                newName: "Desks");

            migrationBuilder.RenameTable(
                name: "coworkings",
                newName: "Coworkings");

            migrationBuilder.RenameTable(
                name: "bookings",
                newName: "booking");

            migrationBuilder.RenameIndex(
                name: "ix_bookings_created_at",
                table: "booking",
                newName: "ix_booking_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_bookings_access_code",
                table: "booking",
                newName: "ix_booking_access_code");

            migrationBuilder.AddPrimaryKey(
                name: "pk_booking",
                table: "booking",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_desk_desk_id",
                table: "booking",
                column: "desk_id",
                principalTable: "Desks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
