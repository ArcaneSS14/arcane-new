namespace Content.Arcane.Server.CloneProjector;

[RegisterComponent]
public sealed partial class GeminiProjectionComponent : Component
{
    public EntityUid Projector;
    public HashSet<EntityUid> Equipment = [];
    public bool Deployed;
    public bool Retiring;
}

[RegisterComponent]
public sealed partial class GeminiWearerComponent : Component
{
    public EntityUid Projector;
}

[RegisterComponent]
public sealed partial class GeminiOrganComponent : Component;

[RegisterComponent]
public sealed partial class GeminiEquipmentComponent : Component;
