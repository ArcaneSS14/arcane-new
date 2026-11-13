// Arcane-Start
#nullable enable
using Content.Goobstation.Shared.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Trauma.Common.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P15BlobRuntimeTest : InteractionTest
{
    private static readonly EntProtoId CorePrototype = "CoreBlobTile";
    private static readonly EntProtoId Observer = "MobObserverBlob";
    private static readonly EntProtoId Human = "MobHuman";
    private static readonly EntProtoId Wall = "WallSolid";
    private static readonly ProtoId<BlobTilePrototype> FactoryTile = "Factory";
    private static readonly ProtoId<BlobTilePrototype> Node = "Node";
    private static readonly ProtoId<BlobChemPrototype> Chain = "ChainCoating";
    private static readonly ProtoId<BlobChemPrototype> Comatose = "ComatoseFiber";
    private static readonly ProtoId<BlobChemPrototype> Sinewy = "SinewyTendons";
    private static readonly ProtoId<BlobChemPrototype> Corrosive = "CorrosiveSlime";
    private static readonly ProtoId<BlobChemPrototype> Cryogenic = "CryogenicPoison";
    private static readonly ProtoId<BlobChemPrototype> Explosive = "ExplosiveLattice";
    private static readonly ProtoId<DamageModifierSetPrototype> ChainTile = "ChainCoatingBlob";
    private static readonly ProtoId<DamageModifierSetPrototype> ChainNaut = "BlobBlobbernautChainCoating";
    private static readonly ProtoId<DamageModifierSetPrototype> DefaultNaut = "BlobBlobbernaut";
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [TestCase("ChainCoating", 9, 23)]
    [TestCase("ComatoseFiber", 6, 6)]
    [TestCase("CryogenicPoison", 6, 15)]
    public async Task GrowthChargesTheCurrentChemistryOnFloorAndInSpace(string chemistry, int floorCost, int spaceCost)
    {
        var east = Center.Offset(Vector2.UnitX);
        var north = Center.Offset(Vector2.UnitY);
        await SetTile(Plating, SEntMan.GetNetCoordinates(east), MapData.Grid);
        await SetTile(null, SEntMan.GetNetCoordinates(north), MapData.Grid);

        await Server.WaitAssertion(() =>
        {
            var core = CreateCore();
            var system = SEntMan.System<BlobCoreSystem>();
            system.ChangeChem(core.AsNullable(), chemistry);

            core.Comp.CurrentPoints = floorCost;
            Interact(core, east);
            Assert.That(core.Comp.CurrentPoints, Is.Zero);
            Assert.That(system.GetTargetTile(east)?.Comp.Core, Is.EqualTo(core.Owner));

            core.Comp.CurrentPoints = spaceCost - 1;
            Interact(core, north);
            Assert.That(core.Comp.CurrentPoints, Is.EqualTo(spaceCost - 1));
            Assert.That(system.GetTargetTile(north), Is.Null);

            core.Comp.CurrentPoints = spaceCost;
            Interact(core, north);
            Assert.That(core.Comp.CurrentPoints, Is.Zero);
            Assert.That(system.GetTargetTile(north)?.Comp.Core, Is.EqualTo(core.Owner));
        });
    }

    [Test]
    public async Task FactoryAppliesNewChemistryToBlobbernautAndRespectsPodLimitAcrossSwitches()
    {
        var east = Center.Offset(Vector2.UnitX);
        await SetTile(Plating, SEntMan.GetNetCoordinates(east), MapData.Grid);
        await Server.WaitAssertion(() =>
        {
            var core = CreateCore();
            var system = SEntMan.System<BlobCoreSystem>();
            var node = SEntMan.GetComponent<BlobNodeComponent>(core);
            system.ChangeChem(core.AsNullable(), Chain);
            Assert.That(SEntMan.GetComponent<DamageableComponent>(core).DamageModifierSetId, Is.EqualTo(ChainTile));
            Assert.That(node.NextPulse, Is.EqualTo(Server.ResolveDependency<IGameTiming>().CurTime + TimeSpan.FromSeconds(6)));

            var id = system.TransformBlobTile(null, core.AsNullable(), (core.Owner, node), FactoryTile, east, doEffects: false);
            Assert.That(id, Is.Not.Null);
            var factory = SEntMan.GetComponent<BlobFactoryComponent>(id!.Value);
            Assert.That(SEntMan.System<BlobFactorySystem>().ProduceBlobbernaut((id.Value, factory)), Is.True);
            var naut = SEntMan.GetComponent<DamageableComponent>(factory.Blobbernaut!.Value);
            Assert.That(naut.DamageModifierSetId, Is.EqualTo(ChainNaut));

            system.ChangeChem(core.AsNullable(), Sinewy);
            Assert.That(naut.DamageModifierSetId, Is.EqualTo(DefaultNaut));
            Assert.That(node.NextPulse, Is.EqualTo(Server.ResolveDependency<IGameTiming>().CurTime + TimeSpan.FromSeconds(2.5)));
            var nodes = SEntMan.System<BlobNodeSystem>();
            var pulsesPastLimit = (factory.AccumulateToSpawn + 1) * 6;
            for (var i = 0; i < pulsesPastLimit; i++)
                nodes.PulseNode((core.Owner, node), core, SProtoMan.Index(Sinewy));

            Assert.That(factory.BlobPods, Has.Count.EqualTo(5));
            var smoke = SEntMan.GetComponent<SmokeOnTriggerComponent>(factory.BlobPods[0]).Solution;
            Assert.That(smoke.GetTotalPrototypeQuantity("Ipecac"), Is.EqualTo(Content.Shared.FixedPoint.FixedPoint2.New(15)));
            Assert.That(smoke.GetTotalPrototypeQuantity("Profanol"), Is.EqualTo(Content.Shared.FixedPoint.FixedPoint2.New(15)));
            system.ChangeChem(core.AsNullable(), Corrosive);
            Assert.That(system.GetTileCost(core.Comp, Node), Is.EqualTo(100));
            SEntMan.DeleteEntity(factory.BlobPods[^1]);
            for (var i = 0; i <= factory.AccumulateToSpawn; i++)
                nodes.PulseNode((core.Owner, node), core, SProtoMan.Index(Corrosive));
            Assert.That(factory.BlobPods, Has.Count.EqualTo(4));

            system.ChangeChem(core.AsNullable(), Chain);
            Assert.That(naut.DamageModifierSetId, Is.EqualTo(ChainNaut));
            var nativeResistance = SEntMan.GetComponent<ExplosionResistanceComponent>(core).DamageCoefficient;
            system.ChangeChem(core.AsNullable(), Explosive);
            Assert.That(SEntMan.GetComponent<ExplosionResistanceComponent>(core).DamageCoefficient, Is.Zero);
            system.ChangeChem(core.AsNullable(), Chain);
            Assert.That(SEntMan.GetComponent<ExplosionResistanceComponent>(core).DamageCoefficient, Is.EqualTo(nativeResistance));
            system.ChangeChem(core.AsNullable(), Cryogenic);
            Assert.That(naut.DamageModifierSetId, Is.EqualTo(SProtoMan.Index(Cryogenic).DamageModifiers));
        });
    }

    [Test]
    public async Task SinewyAttacksHealInjuredMobsAndDamageStructures()
    {
        var east = Center.Offset(Vector2.UnitX);
        await SetTile(Plating, SEntMan.GetNetCoordinates(east), MapData.Grid);
        await Server.WaitAssertion(() =>
        {
            var core = CreateCore();
            SEntMan.System<BlobCoreSystem>().ChangeChem(core.AsNullable(), Sinewy);
            var human = SEntMan.SpawnAttachedTo(Human, east);
            var wall = SEntMan.SpawnAttachedTo(Wall, east);
            var damage = SEntMan.System<DamageableSystem>();
            damage.ChangeDamage(human, new DamageSpecifier { DamageDict = new() { [Blunt] = 48 } },
                ignoreResistances: true, targetPart: TargetBodyPart.All);
            var before = damage.GetTotalDamage(human);
            Assert.That(before, Is.GreaterThan(Content.Shared.FixedPoint.FixedPoint2.Zero));
            Interact(core, east, human);
            Assert.That(damage.GetTotalDamage(human), Is.LessThan(before));
            Interact(core, east, wall);
            Assert.That(damage.GetTotalDamage(wall), Is.GreaterThan(Content.Shared.FixedPoint.FixedPoint2.Zero));
        });
    }

    private Entity<BlobCoreComponent> CreateCore()
    {
        var uid = SEntMan.SpawnAttachedTo(CorePrototype, Center);
        var core = SEntMan.GetComponent<BlobCoreComponent>(uid);
        var observer = SEntMan.SpawnAttachedTo(Observer, Center);
        core.Observer = observer;
        SEntMan.GetComponent<BlobObserverComponent>(observer).Core = uid;
        return (uid, core);
    }

    private void Interact(Entity<BlobCoreComponent> core, EntityCoordinates target, EntityUid? entity = null)
    {
        core.Comp.NextAction = TimeSpan.Zero;
        var observer = core.Comp.Observer!.Value;
        var attempt = new UserInteractAttemptEvent(observer, entity, target, CanReach: true);
        SEntMan.EventBus.RaiseLocalEvent(observer, ref attempt);
    }
}
// Arcane-End
