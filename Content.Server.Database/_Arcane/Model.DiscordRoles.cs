using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

/// <summary>
/// One Discord role of a linked Discord account. The external Discord integration replaces these rows
/// and stamps <c>rmc_discord_accounts.roles_updated_at</c>.
/// </summary>
[Table("rmc_discord_account_roles")]
[Index(nameof(RoleId))]
[PrimaryKey(nameof(DiscordId), nameof(RoleId))]
public sealed class ArcaneDiscordAccountRole
{
    public ulong DiscordId { get; set; }

    public RMCDiscordAccount Discord { get; set; } = default!;

    public ulong RoleId { get; set; }
}

internal static class ModelDiscordRoles
{
    public const string RolesUpdatedAt = "RolesUpdatedAt";

    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Kept as a shadow property so the RMC account model stays untouched.
        modelBuilder.Entity<RMCDiscordAccount>()
            .Property<DateTime?>(RolesUpdatedAt);

        modelBuilder.Entity<ArcaneDiscordAccountRole>()
            .HasOne(r => r.Discord)
            .WithMany()
            .HasForeignKey(r => r.DiscordId)
            .HasPrincipalKey(d => d.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
