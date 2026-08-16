using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspiresample.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class FixParticipantDeleteCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionParticipants_ExternalParticipants_ExternalPartic~",
                table: "CompetitionParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionParticipants_Members_MemberId",
                table: "CompetitionParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionResults_ExternalParticipants_ExternalParticipant~",
                table: "CompetitionResults");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionResults_Members_MemberId",
                table: "CompetitionResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAttendances_ExternalParticipants_ExternalParticipan~",
                table: "TrainingAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAttendances_Members_MemberId",
                table: "TrainingAttendances");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionParticipants_ExternalParticipants_ExternalPartic~",
                table: "CompetitionParticipants",
                column: "ExternalParticipantId",
                principalTable: "ExternalParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionParticipants_Members_MemberId",
                table: "CompetitionParticipants",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionResults_ExternalParticipants_ExternalParticipant~",
                table: "CompetitionResults",
                column: "ExternalParticipantId",
                principalTable: "ExternalParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionResults_Members_MemberId",
                table: "CompetitionResults",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAttendances_ExternalParticipants_ExternalParticipan~",
                table: "TrainingAttendances",
                column: "ExternalParticipantId",
                principalTable: "ExternalParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAttendances_Members_MemberId",
                table: "TrainingAttendances",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionParticipants_ExternalParticipants_ExternalPartic~",
                table: "CompetitionParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionParticipants_Members_MemberId",
                table: "CompetitionParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionResults_ExternalParticipants_ExternalParticipant~",
                table: "CompetitionResults");

            migrationBuilder.DropForeignKey(
                name: "FK_CompetitionResults_Members_MemberId",
                table: "CompetitionResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAttendances_ExternalParticipants_ExternalParticipan~",
                table: "TrainingAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingAttendances_Members_MemberId",
                table: "TrainingAttendances");

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionParticipants_ExternalParticipants_ExternalPartic~",
                table: "CompetitionParticipants",
                column: "ExternalParticipantId",
                principalTable: "ExternalParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionParticipants_Members_MemberId",
                table: "CompetitionParticipants",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionResults_ExternalParticipants_ExternalParticipant~",
                table: "CompetitionResults",
                column: "ExternalParticipantId",
                principalTable: "ExternalParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CompetitionResults_Members_MemberId",
                table: "CompetitionResults",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAttendances_ExternalParticipants_ExternalParticipan~",
                table: "TrainingAttendances",
                column: "ExternalParticipantId",
                principalTable: "ExternalParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingAttendances_Members_MemberId",
                table: "TrainingAttendances",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
