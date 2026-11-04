#nullable enable
using Content.Goobstation.Shared.Weapons.CounterattackWeapon;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Shared.ItemSwitch;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Weapons;

public sealed class SecurityWeaponTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId Betty = "Betty";
    private static readonly EntProtoId Stunbaton = "Stunbaton";
    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId CounterattackWeapon = "TestArcaneCounterattackWeapon";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: BaseItem
  id: TestArcaneCounterattackWeapon
  components:
  - type: MeleeWeapon
    damage:
      types:
        Blunt: 1
  - type: ItemSwitch
    state: on
    states:
      off: !type:ItemSwitchState {}
      on: !type:ItemSwitchState
        components:
        - type: CounterattackWeapon
          setItemSwitch: off
""";

    [Test]
    public async Task BettyCounterattacksAndFolds()
    {
        await AddAtmosphere();
        var target = ToServer(await SpawnTarget(Human));
        var weapon = ToServer(await PlaceInHands(Betty));
        await SetCombatMode(true);

        await Server.WaitAssertion(() =>
        {
            var switching = SEntMan.System<SharedItemSwitchSystem>();
            Assert.That(switching.Switch(weapon, "on", SPlayer), Is.True);
            Assert.That(SComp<CounterattackWeaponUserComponent>(SPlayer).Weapons, Is.EqualTo(new[] { weapon }));

            var attacked = new AttackedEvent(target, target, SEntMan.GetCoordinates(PlayerCoords));
            SEntMan.EventBus.RaiseLocalEvent(SPlayer, attacked);

            Assert.That(SComp<ItemSwitchComponent>(weapon).State, Is.EqualTo("off"));
            Assert.That(SHasComp<CounterattackWeaponComponent>(weapon), Is.False);
            Assert.That(SHasComp<CounterattackWeaponUserComponent>(SPlayer), Is.False);
            Assert.That(SEntMan.System<DamageableSystem>().GetTotalDamage(target).Float(), Is.GreaterThan(0));
            Assert.That(HandSys.IsHolding(SPlayer, weapon), Is.True);
            Assert.That(HandSys.CountFreeHands(SPlayer), Is.EqualTo(1));

            Assert.That(switching.Switch(weapon, "on", SPlayer), Is.True);
            Assert.That(SComp<CounterattackWeaponUserComponent>(SPlayer).Weapons, Is.EqualTo(new[] { weapon }));
        });

        await Drop();
        await Server.WaitAssertion(() => Assert.That(SHasComp<CounterattackWeaponUserComponent>(SPlayer), Is.False));
    }

    [Test]
    public async Task ReciprocalCounterattacksFinishAndFoldBothWeapons()
    {
        await AddAtmosphere();
        var target = ToServer(await SpawnTarget(Human));
        var first = ToServer(await PlaceInHands(CounterattackWeapon));
        await SetCombatMode(true);

        await Server.WaitAssertion(() =>
        {
            var second = SSpawn(CounterattackWeapon, SEntMan.GetCoordinates(TargetCoords));
            Assert.That(HandSys.TryPickupAnyHand(target, second), Is.True);
            SEntMan.System<SharedCombatModeSystem>().SetInCombatMode(target, true);

            var attacked = new AttackedEvent(second, target, SEntMan.GetCoordinates(PlayerCoords));
            SEntMan.EventBus.RaiseLocalEvent(SPlayer, attacked);

            Assert.That(SComp<ItemSwitchComponent>(first).State, Is.EqualTo("off"));
            Assert.That(SComp<ItemSwitchComponent>(second).State, Is.EqualTo("off"));
            Assert.That(SHasComp<CounterattackWeaponUserComponent>(SPlayer), Is.False);
            Assert.That(SHasComp<CounterattackWeaponUserComponent>(target), Is.False);
        });
    }

    [Test]
    public async Task StunbatonDealsSixtyImmediateStaminaPerLightHit()
    {
        await AddAtmosphere();
        var target = ToServer(await SpawnTarget(Human));
        var weapon = ToServer(await PlaceInHands(Stunbaton));

        await Server.WaitAssertion(() =>
        {
            var stamina = SComp<StaminaComponent>(target);
            for (var hit = 1; hit <= 2; hit++)
            {
                var ev = new MeleeHitEvent([target], SPlayer, weapon, new DamageSpecifier(), null,
                    SEntMan.GetCoordinates(TargetCoords));
                SEntMan.EventBus.RaiseLocalEvent(weapon, ev);
                Assert.That(stamina.StaminaDamage, Is.EqualTo(Math.Min(60 * hit, stamina.CritThreshold)));
            }

            Assert.That(stamina.Critical, Is.True);
        });
    }
}
