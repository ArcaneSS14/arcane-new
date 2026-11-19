// Arcane-Start
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
// Arcane-End

namespace Content.Shared.Humanoid.Markings; // Arcane

// Arcane-Start
public sealed partial class MarkingManager
{
    private FrozenDictionary<string, string> _waggingByStill = FrozenDictionary<string, string>.Empty;
    private FrozenDictionary<string, string> _stillByWagging = FrozenDictionary<string, string>.Empty;

    private void CacheWagging()
    {
        var forward = new Dictionary<string, string>();
        var reverse = new Dictionary<string, string>();
        foreach (var (id, prototype) in _markings)
        {
            var candidate = prototype.Wagging?.Id ?? id + "Animated";
            if (candidate == id || !_markings.TryGetValue(candidate, out var animated)
                || animated.BodyPart != prototype.BodyPart || animated.Sprites.Count != prototype.Sprites.Count)
                continue;

            forward[id] = candidate;
            reverse.TryAdd(candidate, id);
        }
        _waggingByStill = forward.ToFrozenDictionary();
        _stillByWagging = reverse.ToFrozenDictionary();
    }

    public bool IsWaggingVariant(string id) => _stillByWagging.ContainsKey(id);

    public bool TryGetWaggingMarking(string id, [NotNullWhen(true)] out string? animated)
        => _waggingByStill.TryGetValue(id, out animated);

    public bool TryGetStillMarking(string id, [NotNullWhen(true)] out string? still)
        => _stillByWagging.TryGetValue(id, out still);
}
// Arcane-End
