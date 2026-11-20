using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ArcaneEarsAboveHair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Arcane-Start
            migrationBuilder.AddColumn<bool>(
                name: "ears_above_hair",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
            // Arcane-End
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Arcane-Start
            migrationBuilder.DropColumn(
                name: "ears_above_hair",
                table: "profile");
            // Arcane-End
        }
    }
}
