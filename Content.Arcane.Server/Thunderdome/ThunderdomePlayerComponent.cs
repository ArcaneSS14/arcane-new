using Robust.Shared.Map;

namespace Content.Arcane.Server.Thunderdome;

[RegisterComponent, Access(typeof(ThunderdomeSystem))]
public sealed partial class ThunderdomePlayerComponent : Component
{
    public EntityUid OriginalMind;
    public EntityUid TemporaryMind;
    public EntityUid? OriginalBody;
    public EntityUid Rule;
    public NetUserId User;
    public EntityCoordinates ReturnPosition;
    public bool Closing;
    public int Streak;
}

[RegisterComponent, Access(typeof(ThunderdomeSystem))]
public sealed partial class ThunderdomeOriginalBodyComponent : Component
{
    public EntityUid Avatar;
}

[RegisterComponent, Access(typeof(ThunderdomeSystem))]
public sealed partial class ThunderdomeArenaItemComponent : Component
{
    public MapId Map;
    public TimeSpan? DeleteAfter;
}
