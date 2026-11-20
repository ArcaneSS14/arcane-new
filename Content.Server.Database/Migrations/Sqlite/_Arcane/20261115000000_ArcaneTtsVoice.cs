using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite;

/// <summary>
/// Profile column for the TTS voice. Empty means the default voice.
/// </summary>
[DbContext(typeof(SqliteServerDbContext))]
[Migration("20261115000000_ArcaneTtsVoice")]
public sealed class ArcaneTtsVoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "tts_voice", table: "profile", type: "TEXT", nullable: false, defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "tts_voice", table: "profile");
    }
}
