namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Discord roles (names of <c>DiscordRole</c>) that unlock this <see cref="SponsorOnly"/> marking. Empty means
    /// any sponsor role.
    /// </summary>
    [DataField]
    public List<string> SponsorRoles { get; private set; } = new();
}
