namespace Content.Arcane.Shared.Medical.Defibrillator;

[Prototype]
public sealed partial class PulseStatePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SpriteSpecifier Sprite { get; private set; } = default!;
}
