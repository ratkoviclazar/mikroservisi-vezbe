using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventAPI.SagaOrchestratorService.Migrations
{
    /// <inheritdoc />
    public partial class ChangedSagaState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventAgenda",
                table: "SagaStates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EventDateTime",
                table: "SagaStates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "EventDurationInHours",
                table: "SagaStates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EventName",
                table: "SagaStates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EventPrice",
                table: "SagaStates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LectureDateTime",
                table: "SagaStates",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "LectureDurationInHours",
                table: "SagaStates",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SimulateLectureCreationFailure",
                table: "SagaStates",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventAgenda",
                table: "SagaStates");

            migrationBuilder.DropColumn(
                name: "EventDateTime",
                table: "SagaStates");

            migrationBuilder.DropColumn(
                name: "EventDurationInHours",
                table: "SagaStates");

            migrationBuilder.DropColumn(
                name: "EventName",
                table: "SagaStates");

            migrationBuilder.DropColumn(
                name: "EventPrice",
                table: "SagaStates");

            migrationBuilder.DropColumn(
                name: "LectureDateTime",
                table: "SagaStates");

            migrationBuilder.DropColumn(
                name: "LectureDurationInHours",
                table: "SagaStates");

            migrationBuilder.DropColumn(
                name: "SimulateLectureCreationFailure",
                table: "SagaStates");
        }
    }
}
