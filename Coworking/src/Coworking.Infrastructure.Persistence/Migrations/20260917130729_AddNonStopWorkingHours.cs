using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coworking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNonStopWorkingHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "open_time",
                table: "Coworkings",
                type: "time without time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "close_time",
                table: "Coworkings",
                type: "time without time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "Coworkings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_non_stop",
                table: "Coworkings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // equal hours used to mark non-stop; the flag replaces them and the hours are dropped
            migrationBuilder.Sql(
                """
                UPDATE "Coworkings"
                SET is_non_stop = TRUE, open_time = NULL, close_time = NULL
                WHERE open_time = close_time;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_coworkings_working_hours",
                table: "Coworkings",
                sql: "is_non_stop OR (open_time IS NOT NULL AND close_time IS NOT NULL AND open_time <> close_time)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "AddNonStopWorkingHours is irreversible: non-stop coworkings lost their original hours.");
        }
    }
}
