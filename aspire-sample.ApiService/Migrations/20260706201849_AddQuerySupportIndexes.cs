using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspiresample.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddQuerySupportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MembershipFees_Year",
                table: "MembershipFees",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_Date",
                table: "Competitions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionResults_CompetitionId",
                table: "CompetitionResults",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionParticipants_CompetitionId",
                table: "CompetitionParticipants",
                column: "CompetitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MembershipFees_Year",
                table: "MembershipFees");

            migrationBuilder.DropIndex(
                name: "IX_Competitions_Date",
                table: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_CompetitionResults_CompetitionId",
                table: "CompetitionResults");

            migrationBuilder.DropIndex(
                name: "IX_CompetitionParticipants_CompetitionId",
                table: "CompetitionParticipants");
        }
    }
}
