using Content.Arcane.Shared.ModSuits.Systems;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, Access(typeof(ModSuitSystem))]
public sealed partial class ModGrantComponentsModuleComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public ModGrantTarget Target = ModGrantTarget.Part;

    [DataField]
    public string? Slot;

    public EntityUid? GrantedTo;

    public TimeSpan NextAutoInjection;
}

public enum ModGrantTarget : byte
{
    Part,
    Suit,
    Wearer,
}
