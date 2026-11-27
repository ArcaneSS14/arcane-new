// Arcane-Start
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Content.Server.Database;

internal static class ModelJobAlternateTitles
{
    /// <summary>
    /// Stores <see cref="Profile.JobAlternateTitles"/> as a JSON object, the way Trauma stores knowledge mastery.
    /// </summary>
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>()
            .Property(p => p.JobAlternateTitles)
            .HasColumnName("job_alternate_titles")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?) null),
                s => string.IsNullOrEmpty(s)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(s, (JsonSerializerOptions?) null) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
                (a, b) => a != null && b != null && a.Count == b.Count && !a.Except(b).Any(),
                dict => dict.Aggregate(0, (hash, pair) => hash ^ pair.GetHashCode()),
                dict => new Dictionary<string, string>(dict)));
    }
}
// Arcane-End
