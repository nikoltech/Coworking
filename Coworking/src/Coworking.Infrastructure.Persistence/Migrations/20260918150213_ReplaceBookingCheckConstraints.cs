using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coworking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBookingCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_bookings_max_duration",
                table: "booking");

            migrationBuilder.AddCheckConstraint(
                name: "ck_bookings_access_code_v7",
                table: "booking",
                sql: "get_byte(uuid_send(access_code), 6) >> 4 = 7");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_bookings_access_code_v7",
                table: "booking");

            migrationBuilder.AddCheckConstraint(
                name: "ck_bookings_max_duration",
                table: "booking",
                sql: "end_time - start_time <= interval '2160 hours'");
        }
    }
}
