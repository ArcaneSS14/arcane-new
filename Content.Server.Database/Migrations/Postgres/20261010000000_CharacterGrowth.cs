// Arcane-Start: Persist character growth settings for PostgreSQL
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres;

[DbContext(typeof(PostgresServerDbContext))]
[Migration("20261010000000_CharacterGrowth")]
public sealed class CharacterGrowth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<float>(name: "height", table: "profile", type: "real", nullable: false, defaultValue: 1f);
        migrationBuilder.AddColumn<float>(name: "width", table: "profile", type: "real", nullable: false, defaultValue: 1f);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "height", table: "profile");
        migrationBuilder.DropColumn(name: "width", table: "profile");
    }
}
// Arcane-End
