// Arcane-Start: Replace character height/width with a single scale for SQLite
// EF Core 10's SQLite provider cannot generate DropColumn for migrations without
// a Designer file, so the column removal is emitted as raw SQL (SQLite >= 3.35).
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
        migrationBuilder.Sql("ALTER TABLE \"profile\" DROP COLUMN \"height\";");
        migrationBuilder.Sql("ALTER TABLE \"profile\" DROP COLUMN \"width\";");
        migrationBuilder.AddColumn<float>(name: "scale", table: "profile", type: "REAL", nullable: false, defaultValue: 1f);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"profile\" DROP COLUMN \"scale\";");
        migrationBuilder.AddColumn<float>(name: "height", table: "profile", type: "REAL", nullable: false, defaultValue: 1f);
        migrationBuilder.AddColumn<float>(name: "width", table: "profile", type: "REAL", nullable: false, defaultValue: 1f);
    }
}
// Arcane-End