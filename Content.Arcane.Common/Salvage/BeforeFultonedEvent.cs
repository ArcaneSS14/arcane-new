namespace Content.Arcane.Common.Salvage;

/// <summary>
/// Raised on the user before they start fultoning <see cref="Target"/>; cancel it to stop the fulton.
/// </summary>
public sealed class BeforeFultonedEvent(EntityUid target) : CancellableEntityEventArgs
{
    public readonly EntityUid Target = target;
}
