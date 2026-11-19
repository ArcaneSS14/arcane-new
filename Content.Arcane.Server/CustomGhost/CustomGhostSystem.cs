using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Ghost;
using Content.Arcane.Shared.CustomGhost;
using Content.Shared.Ghost.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.CustomGhost;

/// <summary>
/// Spawns the player's chosen custom ghost instead of the plain observer, if its restrictions allow (Orion #50, #126, #247).
/// </summary>
public sealed partial class CustomGhostSystem : EntitySystem
{
    [Dependency] private INetConfigurationManager _netCfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public const string DefaultGhost = "default";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostPrototypeEvent>(OnGhostPrototype);
        SubscribeLocalEvent<GhostSpawnedEvent>(OnGhostSpawned);
        SubscribeNetworkEvent<SetGhostAccessoriesEvent>(OnSetAccessories);
    }

    private void OnGhostPrototype(ref GhostPrototypeEvent args)
    {
        if (args.User is not { } userId
            || !_player.TryGetSessionById(userId, out var session)
            || _netCfg.GetClientCVar(session.Channel, ArcaneCVars.CustomGhost) is not { } chosen
            || chosen == DefaultGhost
            || !_proto.TryIndex<CustomGhostPrototype>(chosen, out var ghost)
            || ghost.Abstract
            || !ghost.CanUse(session))
            return;

        args.Prototype = ghost.GhostEntityPrototype;
        args.Custom = true;
    }

    private void OnGhostSpawned(ref GhostSpawnedEvent args)
    {
        if (args.User is not { } user || !_player.TryGetSessionById(user, out var session))
            return;

        ApplyAccessories(args.Ghost,
            _netCfg.GetClientCVar(session.Channel, ArcaneCVars.GhostHat),
            _netCfg.GetClientCVar(session.Channel, ArcaneCVars.GhostMask),
            _netCfg.GetClientCVar(session.Channel, ArcaneCVars.GhostParticles));
    }

    private void OnSetAccessories(SetGhostAccessoriesEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is { } ghost && HasComp<GhostComponent>(ghost))
            ApplyAccessories(ghost, ev.Hat, ev.Mask, ev.Particles);
    }

    private void ApplyAccessories(EntityUid ghost, string hatId, string maskId, string particlesId)
    {
        var hat = ReadAccessory(hatId, GhostAccessoryCategory.Hat);
        var mask = ReadAccessory(maskId, GhostAccessoryCategory.Mask);
        var particles = ReadAccessory(particlesId, GhostAccessoryCategory.Particles);
        if (hat is null && mask is null && particles is null)
        {
            RemComp<GhostAccessoriesComponent>(ghost);
            return;
        }

        var accessories = EnsureComp<GhostAccessoriesComponent>(ghost);
        if (accessories.Hat == hat && accessories.Mask == mask && accessories.Particles == particles)
            return;

        accessories.Hat = hat;
        accessories.Mask = mask;
        accessories.Particles = particles;
        Dirty(ghost, accessories);
    }

    private ProtoId<GhostAccessoryPrototype>? ReadAccessory(string id, GhostAccessoryCategory category)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64 || !_proto.TryIndex<GhostAccessoryPrototype>(id, out var accessory) || accessory.Category != category)
            return default(ProtoId<GhostAccessoryPrototype>?);

        return new ProtoId<GhostAccessoryPrototype>(accessory.ID);
    }
}
