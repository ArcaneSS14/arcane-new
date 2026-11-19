using Content.Arcane.Shared.Paint;
using Robust.Shared.Audio;
using Content.Shared.DoAfter;

namespace Content.Arcane.Server.Paint;

[RegisterComponent]
public sealed partial class PhotocopierComponent : Component
{
    [DataField]
    public string FirstSlot = "canvas_a";

    [DataField]
    public string SecondSlot = "canvas_b";

    [DataField]
    public TimeSpan ScanTime = TimeSpan.FromSeconds(2.4);

    [DataField]
    public TimeSpan PrintTime = TimeSpan.FromSeconds(2.4);

    [DataField]
    public SoundSpecifier? ScanSound = new SoundPathSpecifier("/Audio/Machines/scanning.ogg");

    [DataField]
    public SoundSpecifier? PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    [ViewVariables]
    public PhotocopierState State = PhotocopierState.Idle;

    [ViewVariables]
    public DoAfterId? DoAfter;

    [ViewVariables]
    public int Operation;

    [ViewVariables]
    public EntityUid? FirstCanvas;

    [ViewVariables]
    public EntityUid? SecondCanvas;

    [ViewVariables]
    public EntityUid? Original;

    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public EntityUid? User;
}
