using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Arcane.Server.Species.Slime;

/// <summary>
/// Enables a slime person's regeneration and keeps the appearance of detached parts.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeRegrowComponent : Component
{
    public EntityUid? Action;
    public bool Regrowing;

    public readonly Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> LostProfiles = new();
    public readonly Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> LostMarkings = new();
}
