// Arcane-Start
using Content.Shared.Body.Components;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Arcane - brains are named after the body they came from (Orion #252).
/// </summary>
public sealed partial class BrainSystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    private void TryRenameBrain(Entity<BrainComponent> ent, EntityUid body)
    {
        if (ent.Comp.Renamed || _timing.ApplyingState || TerminatingOrDeleted(ent) || TerminatingOrDeleted(body))
            return;

        _metaData.SetEntityName(ent, Loc.GetString("comp-brain-name", ("name", Name(ent)), ("player", Name(body))));
        ent.Comp.Renamed = true;
        Dirty(ent);
    }
}
// Arcane-End
