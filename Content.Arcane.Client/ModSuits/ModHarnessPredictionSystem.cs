// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.ModSuits.Components;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.Physics.Systems;

namespace Content.Arcane.Client.ModSuits;

public sealed partial class ModHarnessPredictionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModMagnetizedGunComponent, AfterAutoHandleStateEvent>(OnHarnessState);
        SubscribeLocalEvent<ModMagnetizedGunComponent, ComponentShutdown>(OnHarnessShutdown);
        SubscribeLocalEvent<ModMagnetizedGunComponent, UpdateIsPredictedEvent>(OnPredicted);
    }

    private void OnHarnessState(Entity<ModMagnetizedGunComponent> gun, ref AfterAutoHandleStateEvent args) =>
        _physics.UpdateIsPredicted(gun);

    private void OnHarnessShutdown(Entity<ModMagnetizedGunComponent> gun, ref ComponentShutdown args) =>
        _physics.UpdateIsPredicted(gun);

    private void OnPredicted(Entity<ModMagnetizedGunComponent> gun, ref UpdateIsPredictedEvent args)
    {
        if (gun.Comp.LooseSince != null && gun.Comp.Wearer == _player.LocalEntity)
            args.IsPredicted = true;
    }
}
