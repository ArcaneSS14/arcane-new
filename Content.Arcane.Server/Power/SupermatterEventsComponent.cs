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

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan SurgeUntil;

    public readonly float[] GasSample = new float[Atmospherics.TotalNumberOfGases];
    public float SampleMoles;
    public float SampleTemperature;
    public float WasteMultiplier;
}
