// Arcane-Start
using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ArcaneAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievement_progress",
                columns: table => new
                {
                    achievement_progress_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    achievement_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_progress", x => x.achievement_progress_id);
                    table.ForeignKey(
                        name: "FK_achievement_progress_player_player_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "achievement_progress_batch",
                columns: table => new
                {
                    achievement_progress_batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    achievement_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    delta = table.Column<int>(type: "integer", nullable: false),
                    marks_unlocked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_progress_batch", x => x.achievement_progress_batch_id);
                    table.ForeignKey(
                        name: "FK_achievement_progress_batch_player_player_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_achievement_id",
                table: "achievement_progress",
                column: "achievement_id");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_player_user_id_achievement_id",
                table: "achievement_progress",
                columns: new[] { "player_user_id", "achievement_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievement_progress_batch_player_user_id",
                table: "achievement_progress_batch",
                column: "player_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievement_progress");

            migrationBuilder.DropTable(
                name: "achievement_progress_batch");
        }
    }
}
// Arcane-End
