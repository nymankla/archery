using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspiresample.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalParticipantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAttendances", x => x.Id);
                    table.CheckConstraint("CK_TrainingAttendance_SingleParticipant", "(\"MemberId\" IS NOT NULL AND \"ExternalParticipantId\" IS NULL) OR (\"MemberId\" IS NULL AND \"ExternalParticipantId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TrainingAttendances_ExternalParticipants_ExternalParticipan~",
                        column: x => x.ExternalParticipantId,
                        principalTable: "ExternalParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingAttendances_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrainingAttendances_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendances_ExternalParticipantId",
                table: "TrainingAttendances",
                column: "ExternalParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendances_MemberId",
                table: "TrainingAttendances",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendances_TrainingSessionId",
                table: "TrainingAttendances",
                column: "TrainingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendances_TrainingSessionId_ExternalParticipantId",
                table: "TrainingAttendances",
                columns: new[] { "TrainingSessionId", "ExternalParticipantId" },
                unique: true,
                filter: "\"ExternalParticipantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAttendances_TrainingSessionId_MemberId",
                table: "TrainingAttendances",
                columns: new[] { "TrainingSessionId", "MemberId" },
                unique: true,
                filter: "\"MemberId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_Date",
                table: "TrainingSessions",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingAttendances");

            migrationBuilder.DropTable(
                name: "TrainingSessions");
        }
    }
}
