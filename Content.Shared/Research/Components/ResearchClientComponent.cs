using Robust.Shared.Serialization;

// Arcane-Start
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
// Arcane-End

// Arcane-Start
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Content.Arcane.Common.Construction.Prototypes;
// Arcane-End

using Content.Arcane.Common.Research; // Arcane

namespace Content.Shared.Research.Components
{
    /// <summary>
    /// This is an entity that is able to connect to a <see cref="ResearchServerComponent"/>
    /// </summary>
    [RegisterComponent]
    public sealed partial class ResearchClientComponent : Component
    {
        public bool ConnectedToServer => Server != null;

        /// <summary>
        /// The server the client is connected to
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? Server { get; set; }
    }

    /// <summary>
    /// Raised on the client whenever its server is changed
    /// </summary>
    /// <param name="Server">Its new server</param>
    [ByRefEvent]
    public readonly record struct ResearchRegistrationChangedEvent(EntityUid? Server);

    /// <summary>
    ///     Sent to the server when the client deselects a research server.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ResearchClientServerDeselectedMessage : BoundUserInterfaceMessage
    {
    }

    /// <summary>
    ///     Sent to the server when the client chooses a research server.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ResearchClientServerSelectedMessage : BoundUserInterfaceMessage
    {
        public int ServerId;

        public ResearchClientServerSelectedMessage(int serverId)
        {
            ServerId = serverId;
        }
    }

    /// <summary>
    ///     Request that the server updates the client.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ResearchClientSyncMessage : BoundUserInterfaceMessage
    {
    }

    [NetSerializable, Serializable]
    public enum ResearchClientUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ResearchClientBoundInterfaceState : BoundUserInterfaceState
    {
        public int ServerCount;
        public string[] ServerNames;
        public int[] ServerIds;
        public int SelectedServerId;

        public ResearchClientBoundInterfaceState(int serverCount, string[] serverNames, int[] serverIds, int selectedServerId = -1)
        {
            ServerCount = serverCount;
            ServerNames = serverNames;
            ServerIds = serverIds;
            SelectedServerId = selectedServerId;
        }
    }

    // Arcane-Start
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ExperiScannerComponent : Component
    {
        [DataField, AutoNetworkedField]
        public float ScanRange = 2f;

        [DataField]
        public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

        [DataField]
        public SoundSpecifier FailureSound = new SoundPathSpecifier("/Audio/Machines/scanbuzz.ogg");

        [DataField]
        public string LastResult = string.Empty;
    }
    // Arcane-End

    // Arcane-Start
    [RegisterComponent]
    public sealed partial class ExperimentalDestructiveScannerComponent : Component
    {
        [DataField]
        public string ContainerId = "experimental-destructive-scanner-container";

        [DataField]
        public TimeSpan ScanDuration = TimeSpan.FromSeconds(3.5f);

        /// <summary>
        /// Scan duration without parts, taken at the first parts refresh before any upgrade applies.
        /// </summary>
        [DataField]
        public TimeSpan? BaseScanDuration;

        [DataField]
        public TimeSpan CapsuleStepDuration = TimeSpan.FromSeconds(1.15f);

        public bool IsProcessing;

        public string LastSubject = string.Empty;

        public string LastResult = string.Empty;

        [DataField]
        public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

        [DataField]
        public SoundSpecifier FailureSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");

        [DataField]
        public AudioParams AudioParams = AudioParams.Default.WithVolume(-8f).WithVariation(0.25f);

        [DataField]
        public ProtoId<MachinePartPrototype> ServoPart = "Servo";
    }
    // Arcane-End

    // Arcane-Start
    [RegisterComponent]
    public sealed partial class ResearchAnalyzableComponent : Component
    {
        [DataField]
        public Dictionary<string, List<ResearchPointAmount>> MethodPointRewards = new();

        [DataField]
        public List<string> SupportedMethods = new();

        [DataField]
        public List<string> UnlockTechnologies = new();

        [DataField]
        public List<string> RevealTechnologies = new();

        [DataField]
        public string? DiscoveryTrigger;

        [DataField]
        public List<string> ExperimentActions = new();
    }
    // Arcane-End
}
