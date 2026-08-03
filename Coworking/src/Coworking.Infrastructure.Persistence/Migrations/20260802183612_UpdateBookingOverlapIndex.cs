using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coworking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingOverlapIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_overlap_check",
                table: "booking");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_overlap_check",
                table: "booking",
                columns: new[] { "desk_id", "start_time" })
                .Annotation("Npgsql:IndexInclude", new[] { "end_time", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_overlap_check",
                table: "booking");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_overlap_check",
                table: "booking",
                columns: new[] { "desk_id", "start_time", "end_time", "status" });
        }
    }
}
