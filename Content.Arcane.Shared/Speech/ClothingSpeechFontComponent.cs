using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Speech;

/// <summary>
/// Optional speech presentation supplied by worn clothing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClothingSpeechFontComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField]
    public string Font = "LDFComicSans";

    [DataField]
    public Color Color = Color.FromHex("#e93790");
}
