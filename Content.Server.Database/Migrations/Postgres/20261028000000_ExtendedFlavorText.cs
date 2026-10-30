// Arcane-Start: Extended flavor text columns for PostgreSQL (Orion #165, #183)
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres;

[DbContext(typeof(PostgresServerDbContext))]
[Migration("20261028000000_ExtendedFlavorText")]
public sealed class ExtendedFlavorText : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "character_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "ooc_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "tags_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "links_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "green_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "yellow_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "red_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "nsfw_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "nsfw_ooc_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "nsfw_links_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "nsfw_tags_flavor_text", table: "profile", type: "text", nullable: false, defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "character_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "ooc_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "tags_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "links_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "green_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "yellow_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "red_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "nsfw_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "nsfw_ooc_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "nsfw_links_flavor_text", table: "profile");
        migrationBuilder.DropColumn(name: "nsfw_tags_flavor_text", table: "profile");
    }
}
// Arcane-End
