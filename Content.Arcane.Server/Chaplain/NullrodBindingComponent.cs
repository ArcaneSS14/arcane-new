namespace Content.Arcane.Server.Chaplain;

/// <summary>One mind's actual nullrod entities. Recall never respawns deleted forms.</summary>
[RegisterComponent]
public sealed partial class NullrodBindingComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Forms = [];
}
