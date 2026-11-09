using System.Diagnostics.CodeAnalysis;

namespace Content.Arcane.Common.Sponsors;

/// <summary>
/// Optional provider of sponsor-only loadout prototypes that are injected into the inventory loadout group.
/// </summary>
public interface ISharedLoadoutsManager
{
    public void Initialize();
    public bool TryGetServerPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public List<string> GetClientPrototypes();
}
