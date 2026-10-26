using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspiresample.ApiService.Migrations;

/// <inheritdoc />
public partial class UniqueTrainingSessionDate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TrainingSessions_Date",
            table: "TrainingSessions");

        migrationBuilder.CreateIndex(
            name: "IX_TrainingSessions_Date",
            table: "TrainingSessions",
            column: "Date",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TrainingSessions_Date",
            table: "TrainingSessions");

        migrationBuilder.CreateIndex(
            name: "IX_TrainingSessions_Date",
            table: "TrainingSessions",
            column: "Date");
    }
}
