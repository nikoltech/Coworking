using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coworking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LimitBookingDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_bookings_max_duration",
                table: "booking",
                sql: "end_time - start_time <= interval '2160 hours'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_bookings_max_duration",
                table: "booking");
        }
    }
}
