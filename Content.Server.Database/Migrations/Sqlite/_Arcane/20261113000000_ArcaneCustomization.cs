using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite;

/// <summary>
/// Profile columns for the custom species name and the hair gradient.
/// </summary>
[DbContext(typeof(SqliteServerDbContext))]
[Migration("20261113000000_ArcaneCustomization")]
public sealed class ArcaneCustomization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "custom_species_name", table: "profile", type: "TEXT", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "hair_gradient_data", table: "profile", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "hair_gradient_enabled", table: "profile", type: "INTEGER", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "custom_species_name", table: "profile");
        migrationBuilder.DropColumn(name: "hair_gradient_data", table: "profile");
        migrationBuilder.DropColumn(name: "hair_gradient_enabled", table: "profile");
    }
}
