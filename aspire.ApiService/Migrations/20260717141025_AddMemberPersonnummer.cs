using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aspiresample.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberPersonnummer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Personnummer",
                table: "Members",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_Personnummer",
                table: "Members",
                column: "Personnummer",
                unique: true,
                filter: "\"Personnummer\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_Personnummer",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Personnummer",
                table: "Members");
        }
    }
}
