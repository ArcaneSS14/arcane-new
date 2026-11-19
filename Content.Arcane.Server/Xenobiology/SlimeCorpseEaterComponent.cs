using Content.Shared.DoAfter;

namespace Content.Arcane.Server.Xenobiology;

[RegisterComponent]
public sealed partial class SlimeCorpseEaterComponent : Component
{
    [DataField]
    public TimeSpan EatDuration = TimeSpan.FromSeconds(5);

    public DoAfterId? ActiveDoAfter;
    public EntityUid? ActiveTarget;
}
