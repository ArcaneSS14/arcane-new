using Content.Shared.Atmos;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Arcane.Server.Power;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SupermatterEventsComponent : Component
{
    [DataField] public float EventThreshold = 2000f;
    [DataField] public float HarshThreshold = 5f;
    [DataField] public TimeSpan SurgeDuration = TimeSpan.FromMinutes(5);

    public float Anger;
    public float LastAnger;
    public bool Surging;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan SurgeUntil;

    public float PriorGasEfficiency;
    public float PriorRadiationFactor;

    public readonly float[] GasSample = new float[Atmospherics.TotalNumberOfGases];
    public float SampleMoles;
    public float SampleTemperature;
    public float WasteMultiplier;
}

[Prototype]
public sealed partial class SupermatterEventPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public Gas? GasToSpawn;
    [DataField] public EntProtoId? ProtoToSpawn;
    [DataField] public bool Surge;
    [DataField(required: true)] public LocId Announcement;
}
