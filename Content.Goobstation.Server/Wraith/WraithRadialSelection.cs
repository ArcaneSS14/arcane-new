using Content.Trauma.Common.RadialSelector;

namespace Content.Goobstation.Server.Wraith;

internal static class WraithRadialSelection
{
    public static bool Contains(IReadOnlyList<RadialSelectorEntry> entries, string prototype)
    {
        foreach (var entry in entries)
        {
            if (entry.Prototype == prototype || entry.Category is { } category && Contains(category.Entries, prototype))
                return true;
        }

        return false;
    }
}
