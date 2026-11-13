using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Codewords;

/// <summary>
/// Gamerule that provides codewords for other gamerules that rely on them.
/// </summary>
public sealed partial class CodewordSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnRoundStart(RoundStartingEvent ev)
    {
        var manager = Spawn();
        AddComp<CodewordManagerComponent>(manager);
    }

    /// <summary>
    /// Retrieves codewords for the faction specified.
    /// </summary>
    public string[] GetCodewords(ProtoId<CodewordFactionPrototype> faction)
    {
        var query = EntityQueryEnumerator<CodewordManagerComponent>();
        while (query.MoveNext(out  _, out var manager))
        {
            if (!manager.Codewords.TryGetValue(faction, out var codewordEntity))
                return GenerateForFaction(faction, ref manager);

            return Comp<CodewordComponent>(codewordEntity).Codewords;
        }

        Log.Warning("Codeword system not initialized. Returning empty array.");
        // While throwing in this situation would be cool, that causes a test fail (in SpawnAndDeleteEntityCountTest)
        // as the traitor codewords paper gets spawned in and calls this method,
        // but the "start round" event never gets called in this test case.
        return [];
    }

    private string[] GenerateForFaction(ProtoId<CodewordFactionPrototype> faction, ref CodewordManagerComponent manager)
    {
        var factionProto = ProtoMan.Index<CodewordFactionPrototype>(faction.Id);

        var codewords = GenerateCodewords(factionProto.Generator);
        var codewordsContainer = Spawn(prototype: null, MapCoordinates.Nullspace);
        EnsureComp<CodewordComponent>(codewordsContainer)
            .Codewords = codewords;
        manager.Codewords[faction] = codewordsContainer;
        _adminLogger.Add(LogType.EventStarted, LogImpact.Low, $"Codewords generated for faction {faction}: {string.Join(", ", codewords)}");

        return codewords;
    }

    /// <summary>
    /// Generates codewords as specified by the <see cref="CodewordGeneratorPrototype"/> codeword generator.
    /// </summary>
    public string[] GenerateCodewords(ProtoId<CodewordGeneratorPrototype> generatorId)
    {
        var generator = ProtoMan.Index(generatorId);

        // Arcane-Start: keep small datasets from being drowned out by large ones.
        if (generator.PickDatasetFirst)
        {
            var datasets = generator.Words.Distinct()
                .Select(dataset => ProtoMan.Index(dataset).Values.Select(id => Loc.GetString(id)).Distinct().ToList())
                .Where(dataset => dataset.Count > 0)
                .ToList();
            var picked = new List<string>();
            while (picked.Count < generator.Amount && datasets.Count > 0)
            {
                var word = _random.Pick(_random.Pick(datasets));
                picked.Add(word);
                foreach (var dataset in datasets)
                    dataset.Remove(word);

                datasets.RemoveAll(dataset => dataset.Count == 0);
            }

            return picked.ToArray();
        }
        // Arcane-End

        var codewordPool = new List<string>();
        foreach (var dataset in generator.Words
                     .Select(datasetPrototype => ProtoMan.Index(datasetPrototype)))
        {
            codewordPool.AddRange(dataset.Values);
        }

        var finalCodewordCount = Math.Min(generator.Amount, codewordPool.Count);
        var codewords = new string[finalCodewordCount];
        for (var i = 0; i < finalCodewordCount; i++)
        {
            codewords[i] = Loc.GetString(_random.PickAndTake(codewordPool));
        }
        return codewords;
    }
}
