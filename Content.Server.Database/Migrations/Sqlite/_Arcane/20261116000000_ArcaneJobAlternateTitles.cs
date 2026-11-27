// Arcane-Start
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite;

/// <summary>
/// Profile column for the alternate job titles, a JSON object of job id to title id. Empty means none.
/// </summary>
[DbContext(typeof(SqliteServerDbContext))]
[Migration("20261116000000_ArcaneJobAlternateTitles")]
public sealed class ArcaneJobAlternateTitles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "job_alternate_titles", table: "profile", type: "TEXT", nullable: false, defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "job_alternate_titles", table: "profile");
    }
}
// Arcane-End
