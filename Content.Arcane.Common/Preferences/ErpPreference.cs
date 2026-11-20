using Robust.Shared.Serialization;

namespace Content.Arcane.Common.Preferences;

/// <summary>
/// Whether a character takes part in ERP interactions. Stored in the character profile.
/// </summary>
[Serializable, NetSerializable]
public enum ErpPreference : byte
{
    No = 0,
    Ask = 1,
    Yes = 2,
}
