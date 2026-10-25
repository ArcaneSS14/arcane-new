// Arcane-Start: Add the character scale preference for SQLite
// The obsolete height/width columns are dropped by an earlier migration, so this
// only needs to add the scale column without relying on SQLite's DROP COLUMN.
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite;

[DbContext(typeof(SqliteServerDbContext))]
[Migration("20261011000000_CharacterScale")]
public sealed class CharacterScale : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<float>(name: "scale", table: "profile", type: "REAL", nullable: false, defaultValue: 1f);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "scale", table: "profile");
    }
}
// Arcane-End
