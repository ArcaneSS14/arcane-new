// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Weapons.CounterattackWeapon;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Shared.ItemSwitch;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class CounterattackCombatModeTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task CounterattackRestoresCombatModeAndRespectsPacifism(bool combatMode, bool pacified)
    {
        await SpawnTarget("MobHuman");
        var attacker = STarget!.Value;
        var betty = ToServer(await PlaceInHands("Betty"));

        await Server.WaitPost(() =>
        {
            var combat = SEntMan.System<SharedCombatModeSystem>();
            Assert.That(SEntMan.System<SharedItemSwitchSystem>().Switch(betty, "on", SPlayer), Is.True);
            Assert.That(SEntMan.HasComponent<CounterattackWeaponComponent>(betty), Is.True);
            combat.SetInCombatMode(SPlayer, combatMode);
            if (pacified)
                SEntMan.AddComponent<PacifiedComponent>(SPlayer);

            var attacked = new AttackedEvent(attacker, attacker, Xform(SPlayer).Coordinates);
            SEntMan.EventBus.RaiseLocalEvent(SPlayer, attacked);

            Assert.That(combat.IsInCombatMode(SPlayer), Is.EqualTo(combatMode));
            var damage = SEntMan.System<DamageableSystem>().GetTotalDamage(attacker);
            Assert.That(damage, pacified ? Is.EqualTo(FixedPoint2.Zero) : Is.GreaterThan(FixedPoint2.Zero));
        });
    }
}
