using Content.Shared.Damage.Prototypes;

namespace Content.Arcane.Shared.Medical.Overlays;

/// <summary>
/// Configures the damage contributing to a medical greyscale effect.
/// </summary>
[Prototype]
public sealed partial class DamageGreyscalePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Damage types whose amounts are added before calculating the effect strength.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<DamageTypePrototype>> DamageTypes { get; private set; } = [];

    /// <summary>
    /// Positive total damage at which vision becomes fully monochrome.
    /// </summary>
    [DataField(required: true)]
    public float FullIntensityDamage { get; private set; }
}
