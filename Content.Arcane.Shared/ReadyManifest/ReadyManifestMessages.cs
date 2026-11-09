using Content.Shared.Eui;
using Content.Shared.Roles;

namespace Content.Arcane.Shared.ReadyManifest;

/// <summary>
/// Sent by a lobby client to open the ready manifest.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestReadyManifestMessage : EntityEventArgs;

/// <summary>
/// Character names of ready players, by the jobs they want.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReadyManifestEuiState(Dictionary<ProtoId<JobPrototype>, List<string>> jobCharacters) : EuiStateBase
{
    public readonly Dictionary<ProtoId<JobPrototype>, List<string>> JobCharacters = jobCharacters;
}
