// Arcane-Start
using System.Linq;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    /// <summary>
    /// Imports researched technologies directly into the database without triggering per-technology unlock effects.
    /// Intended for snapshot-style data transfers such as research data disks.
    /// </summary>
    public int ImportTechnologySnapshot(EntityUid uid,
        IEnumerable<string> technologies,
        IEnumerable<string>? independentRecipes = null,
        TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(uid, ref database, false))
            return 0;

        var imported = 0;
        foreach (var technologyId in technologies)
        {
            if (!ProtoMan.TryIndex<TechnologyPrototype>(technologyId, out _))
                continue;

            if (database.ResearchedTechnologies.Contains(technologyId))
                continue;

            database.ResearchedTechnologies.Add(technologyId);
            imported++;
        }

        foreach (var recipeId in independentRecipes ?? [])
        {
            if (!ProtoMan.HasIndex<LatheRecipePrototype>(recipeId) || database.IndependentRecipes.Contains(recipeId))
                continue;

            database.IndependentRecipes.Add(recipeId);
            imported++;
        }

        if (imported == 0)
            return 0;

        var previouslyUnlockedRecipes = database.UnlockedRecipes.ToHashSet();
        RecalculateTechnologyState(uid, database);
        UpdateTechnologyCards(uid, database);
        Dirty(uid, database);

        // One bulk notification so connected lathes pick the recipes up, without per-technology unlock rewards.
        var newlyUnlockedRecipes = database.UnlockedRecipes.Except(previouslyUnlockedRecipes).Select(recipe => recipe.Id).ToList();
        var ev = new TechnologyDatabaseModifiedEvent(newlyUnlockedRecipes);
        RaiseLocalEvent(uid, ref ev);
        return imported;
    }

    /// <summary>
    /// The server whose database a research network actually uses: clients and points go to it, so snapshots
    /// are read from and written to it too.
    /// </summary>
    public bool TryGetNetworkDatabase(EntityUid serverUid,
        out Entity<ResearchServerComponent, TechnologyDatabaseComponent> authority)
    {
        authority = default;
        if (!TryComp<ResearchServerComponent>(serverUid, out var server))
            return false;

        var authorityUid = GetNetworkAuthority(serverUid, server);
        if (!TryComp<ResearchServerComponent>(authorityUid, out var authorityServer) ||
            !TryComp<TechnologyDatabaseComponent>(authorityUid, out var database))
            return false;

        authority = (authorityUid, authorityServer, database);
        return true;
    }
}
// Arcane-End
