using Content.Medical.Common.Body;
using Content.Shared.Examine;

namespace Content.Arcane.Shared.Body;

/// <summary>
/// Shows on examine that a body has no brain (Orion #252).
/// </summary>
public sealed partial class DebrainedExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebrainedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<DebrainedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("comp-brain-examine-debrained", ("entity", ent.Owner)));
    }
}
