// Arcane-Start
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres;

/// <summary>
/// Profile column for the ERP preference. Existing characters start opted out (0 = No).
/// </summary>
[DbContext(typeof(PostgresServerDbContext))]
[Migration("20261114000000_ArcaneErpPreference")]
public sealed class ArcaneErpPreference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "erp_preference", table: "profile", type: "integer", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "erp_preference", table: "profile");
    }
}
// Arcane-End
