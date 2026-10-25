// Arcane-Start: Add the character scale preference for PostgreSQL
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres;

[DbContext(typeof(PostgresServerDbContext))]
[Migration("20261011000000_CharacterScale")]
public sealed class CharacterScale : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<float>(name: "scale", table: "profile", type: "real", nullable: false, defaultValue: 1f);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "scale", table: "profile");
    }
}
// Arcane-End
