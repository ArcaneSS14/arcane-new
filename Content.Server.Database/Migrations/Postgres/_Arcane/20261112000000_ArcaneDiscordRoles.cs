using System;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres;

/// <summary>
/// Adds the Discord role snapshot that sponsorship reads. Existing patrons get their tier's Discord role, so
/// nobody loses access before the Discord integration fills the table. RMC patron tiers stay as they are.
/// </summary>
[DbContext(typeof(PostgresServerDbContext))]
[Migration("20261112000000_ArcaneDiscordRoles")]
public sealed class ArcaneDiscordRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "roles_updated_at",
            table: "rmc_discord_accounts",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "rmc_discord_account_roles",
            columns: table => new
            {
                discord_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rmc_discord_account_roles", x => new { x.discord_id, x.role_id });
                table.ForeignKey(
                    name: "FK_rmc_discord_account_roles_rmc_discord_accounts_discord_id",
                    column: x => x.discord_id,
                    principalTable: "rmc_discord_accounts",
                    principalColumn: "rmc_discord_accounts_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_rmc_discord_account_roles_role_id",
            table: "rmc_discord_account_roles",
            column: "role_id");

        migrationBuilder.Sql("""
            INSERT INTO rmc_discord_account_roles (discord_id, role_id)
            SELECT DISTINCT linked.discord_id, tier.discord_role
            FROM rmc_patrons AS patron
            INNER JOIN rmc_linked_accounts AS linked ON linked.player_id = patron.player_id
            INNER JOIN rmc_patron_tiers AS tier ON tier.rmc_patron_tiers_id = patron.tier_id
            ON CONFLICT (discord_id, role_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "rmc_discord_account_roles");

        migrationBuilder.DropColumn(name: "roles_updated_at", table: "rmc_discord_accounts");
    }
}
