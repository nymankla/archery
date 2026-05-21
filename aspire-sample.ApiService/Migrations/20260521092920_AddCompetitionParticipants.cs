using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspiresample.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompetitionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BowClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AgeClass = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionParticipants", x => x.Id);
                    table.CheckConstraint("CK_CompetitionParticipant_SingleParticipant", "(\"MemberId\" IS NOT NULL AND \"ExternalParticipantId\" IS NULL) OR (\"MemberId\" IS NULL AND \"ExternalParticipantId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CompetitionParticipants_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitionParticipants_ExternalParticipants_ExternalPartic~",
                        column: x => x.ExternalParticipantId,
                        principalTable: "ExternalParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompetitionParticipants_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionParticipants_CompetitionId_ExternalParticipantId",
                table: "CompetitionParticipants",
                columns: new[] { "CompetitionId", "ExternalParticipantId" },
                unique: true,
                filter: "\"ExternalParticipantId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionParticipants_CompetitionId_MemberId",
                table: "CompetitionParticipants",
                columns: new[] { "CompetitionId", "MemberId" },
                unique: true,
                filter: "\"MemberId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionParticipants_ExternalParticipantId",
                table: "CompetitionParticipants",
                column: "ExternalParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionParticipants_MemberId",
                table: "CompetitionParticipants",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitionParticipants");
        }
    }
}
