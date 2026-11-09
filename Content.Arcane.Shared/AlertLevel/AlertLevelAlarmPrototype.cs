using Robust.Shared.Audio;

namespace Content.Arcane.Shared.AlertLevel;

/// <summary>
/// Alarm played by emergency lights while a station is on the alert level with the same id.
/// </summary>
[Prototype]
public sealed partial class AlertLevelAlarmPrototype : IPrototype
{
    /// <summary>
    /// Id of the alert level this alarm belongs to.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Time between alarms. Clamped to at least one second.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(30);
}
