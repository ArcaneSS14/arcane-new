using Robust.Shared.GameStates;
using Content.Arcane.Common.Preferences; // Arcane

namespace Content.Shared.DetailExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DetailExaminableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;

    // Arcane-Start: extended flavor text, sent to the viewer only through the examine window
    [DataField]
    public ExtendedFlavorText Extended = new();
    // Arcane-End
}
