using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components;

public sealed partial class GhostRoleComponent
{
    /// <summary>
    /// Job checked for role bans only, never given to the player. Lets admins ban someone from a group of ghost roles
    /// (wizard, xenomorph, wraith...) by banning that job.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? BanJob;
}
