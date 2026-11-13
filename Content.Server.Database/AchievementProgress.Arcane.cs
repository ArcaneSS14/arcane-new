// Arcane-Start
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("achievement_progress")]
[Index(nameof(AchievementId))]
[Index(nameof(PlayerUserId), nameof(AchievementId), IsUnique = true)]
public sealed class AchievementProgress
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid PlayerUserId { get; set; }

    public Player Player { get; set; } = default!;

    [Required, StringLength(128)]
    public string AchievementId { get; set; } = default!;

    public int Progress { get; set; }

    public DateTime? UnlockedAt { get; set; }
}

/// <summary>A write retained until its caller knows the batch committed and acknowledges it.</summary>
[Table("achievement_progress_batch")]
public sealed class AchievementProgressBatch
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerUserId { get; set; }

    public Player Player { get; set; } = default!;

    [Required, StringLength(128)]
    public string AchievementId { get; set; } = default!;

    public int Delta { get; set; }

    public bool MarksUnlocked { get; set; }
}

internal static class ModelAchievements
{
    public static void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AchievementProgress>().HasOne(a => a.Player).WithMany()
            .HasForeignKey(a => a.PlayerUserId).HasPrincipalKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        model.Entity<AchievementProgressBatch>().HasOne(a => a.Player).WithMany()
            .HasForeignKey(a => a.PlayerUserId).HasPrincipalKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
// Arcane-End
