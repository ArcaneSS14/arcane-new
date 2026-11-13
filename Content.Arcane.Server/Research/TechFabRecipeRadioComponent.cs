using Content.Shared.Radio;

namespace Content.Arcane.Server.Research;

[RegisterComponent]
public sealed partial class TechFabRecipeRadioComponent : Component
{
    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype> Channel;
}
