using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EventAPI.Migrations
{
    /// <inheritdoc />
    public partial class dbseed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Lecturers",
                columns: new[] { "Id", "ExpertiseArea", "Name", "Surname", "Title" },
                values: new object[,]
                {
                    { 1, "IT", "John", "Doe", "Professor" },
                    { 2, "AI", "Jane", "Smith", "Dr" }
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "Address", "Capacity", "Name" },
                values: new object[,]
                {
                    { 1, "Main Street 1", 100, "Hall A" },
                    { 2, "Main Street 2", 200, "Hall B" }
                });

            migrationBuilder.InsertData(
                table: "Types",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Seminar" },
                    { 2, "Workshop" }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "Agenda", "DateTime", "DurationInHours", "LocationId", "Name", "Price", "TypeId" },
                values: new object[] { 1, "Tech topics", new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5m, 1, "Tech Conference", 100m, 1 });

            migrationBuilder.InsertData(
                table: "EventLectures",
                columns: new[] { "Id", "DateTime", "DurationInHours", "EventId", "LecturerId" },
                values: new object[] { 1, new DateTime(2025, 5, 1, 10, 0, 0, 0, DateTimeKind.Unspecified), 2m, 1, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EventLectures",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Lecturers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Types",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Lecturers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Types",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
