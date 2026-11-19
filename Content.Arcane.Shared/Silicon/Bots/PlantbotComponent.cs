using Robust.Shared.Audio;

namespace Content.Arcane.Shared.Silicon.Bots;

/// <summary>
/// Prototype configuration for automatic watering and weeding.
/// Emag state is owned by the standard EmaggedComponent.
/// </summary>
[RegisterComponent]
public sealed partial class PlantbotComponent : Component
{
    [DataField]
    public float WaterThreshold = 80f;

    [DataField]
    public float WeedThreshold = 1f;

    [DataField]
    public float WaterAmount = 10f;

    [DataField]
    public float WeedAmount = 1f;

    [DataField]
    public SoundSpecifier WaterSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");

    [DataField]
    public SoundSpecifier WeedSound = new SoundPathSpecifier("/Audio/Effects/plant_rustle.ogg");

    [DataField]
    public SoundSpecifier RemoveWaterSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
}
