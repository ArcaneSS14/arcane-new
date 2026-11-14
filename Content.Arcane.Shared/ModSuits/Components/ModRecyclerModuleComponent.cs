using Content.Shared.Actions;
using Content.Shared.Materials;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent]
public sealed partial class ModRecyclerModuleComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float Range = 1.2f;

    [DataField]
    public float PullSpeed = 2.5f;

    [DataField]
    public float CatchDistance = 0.3f;

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(0.25);

    [DataField]
    public EntProtoId? Product;

    [DataField]
    public EntProtoId UnloadAction = "ActionModRecyclerUnload";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/trashbag1.ogg");

    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> Materials = new();

    [DataField]
    public int Products;

    [ViewVariables]
    public EntityUid? UnloadActionEntity;

    [ViewVariables]
    public TimeSpan NextCollect;
}

public sealed partial class ModRecyclerUnloadActionEvent : InstantActionEvent;
