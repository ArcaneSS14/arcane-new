namespace Content.Server._Arcane.Telephone;

/// <summary>
/// Identifies the original speaker while a telephone emits one relayed message.
/// </summary>
[RegisterComponent]
public sealed partial class TelephoneRelaySpeechComponent : Component
{
    public EntityUid? Source;
}
