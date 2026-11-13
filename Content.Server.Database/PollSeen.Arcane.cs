// Arcane-Start
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

/// <summary>
/// A view of a community poll, independent of whether the player voted.
/// This model must remain in the existing database assembly.
/// </summary>
[Table("poll_seen")]
[Index(nameof(PollId), nameof(PlayerUserId), IsUnique = true)]
[Index(nameof(PlayerUserId))]
public sealed class PollSeen
{
    [Key]
    public int Id { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; } = default!;
    public Guid PlayerUserId { get; set; }
    public Player Player { get; set; } = default!;
    public DateTime SeenAt { get; set; }
}

internal static class ModelPollSeen
{
    public static void OnModelCreating(ModelBuilder model)
    {
        model.Entity<PollSeen>().HasOne(s => s.Poll).WithMany()
            .HasForeignKey(s => s.PollId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<PollSeen>().HasOne(s => s.Player).WithMany()
            .HasForeignKey(s => s.PlayerUserId).HasPrincipalKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
// Arcane-End
