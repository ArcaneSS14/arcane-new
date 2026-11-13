using Content.Arcane.Server.CloneProjector;
using Content.Arcane.Shared.CloneProjector;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P20GeminiRuntimeTest : InteractionTest
{
    [Test]
    public async Task GeminiRecallsAndReleasesOriginalItemsAndMindOnRemoval()
    {
        EntityUid clone = default;
        EntityUid copy = default;
        EntityUid original = default;
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            var host = SEntMan.SpawnAttachedTo("MobHuman", position);
            var projector = SEntMan.SpawnAttachedTo("ClothingBeltGeminiHoloProjector", position);
            var backpack = SEntMan.SpawnAttachedTo("ClothingBackpack", position);
            original = SEntMan.SpawnAttachedTo("Crowbar", position);
            var inventory = SEntMan.System<InventorySystem>();
            var containers = SEntMan.System<SharedContainerSystem>();
            var actions = SEntMan.System<SharedActionsSystem>();
            var minds = SEntMan.System<SharedMindSystem>();
            var mechs = SEntMan.System<SharedMechSystem>();
            var data = SEntMan.GetComponent<CloneProjectorComponent>(projector);

            Assert.That(inventory.TryEquip(host, backpack, "back", silent: true, force: true), Is.True);
            Assert.That(containers.Insert(original, SEntMan.GetComponent<StorageComponent>(backpack).Container), Is.True);
            Assert.That(inventory.TryEquip(host, projector, "belt", silent: true, force: true), Is.True);
            Assert.That(data.ActionEntity, Is.Not.Null);
            var action = data.ActionEntity!.Value;
            var actionEntity = (action, SEntMan.GetComponent<ActionComponent>(action));
            Assert.That(actions.PerformAction(host, actionEntity), Is.True);
            Assert.That(data.CloneUid, Is.Not.Null);
            clone = data.CloneUid!.Value;
            Assert.That(inventory.TryGetSlotEntity(clone, "back", out var equipped), Is.True);
            copy = equipped!.Value;
            Assert.That(copy, Is.Not.EqualTo(backpack));
            var copyStorage = SEntMan.GetComponent<StorageComponent>(copy).Container;
            Assert.That(copyStorage.Count, Is.Zero);
            Assert.That(SEntMan.HasComponent<GhostRoleComponent>(clone), Is.True);
            Assert.That(SEntMan.System<MobThresholdSystem>().TryGetDeadThreshold(clone, out var threshold), Is.True);
            Assert.That((float) threshold!.Value, Is.EqualTo(42f));

            var mind = minds.CreateMind(null, "Gemini runtime fixture");
            var roles = SEntMan.System<SharedRoleSystem>();
            minds.TransferTo(mind.Owner, clone, mind: mind.Comp);
            roles.MindAddRole(mind.Owner, "MindRoleGemini", mind: mind.Comp);
            Assert.That(containers.Insert(original, copyStorage), Is.True);

            var mech = SEntMan.SpawnAttachedTo("MechRipleyBattery", position);
            var gun = SEntMan.SpawnAttachedTo("WeaponMechDebugLaser", position);
            var shot = new ShotAttemptedEvent { User = mech, Used = (gun, SEntMan.GetComponent<GunComponent>(gun)) };
            SEntMan.EventBus.RaiseLocalEvent(mech, ref shot);
            Assert.That(shot.Cancelled, Is.False);
            Assert.That(mechs.TryInsert(mech, clone), Is.True);
            SEntMan.EventBus.RaiseLocalEvent(mech, ref shot);
            Assert.That(shot.Cancelled, Is.True);

            Assert.That(actions.PerformAction(host, actionEntity), Is.True);
            Assert.That(SEntMan.HasComponent<MechPilotComponent>(clone), Is.False);
            Assert.That(SEntMan.GetComponent<MechComponent>(mech).PilotSlot.ContainedEntity, Is.Null);
            Assert.That(SEntMan.HasComponent<GhostRoleComponent>(clone), Is.False);
            Assert.That(mind.Comp.OwnedEntity, Is.EqualTo(clone));
            Assert.That(copyStorage.Count, Is.Zero);
            Assert.That(containers.TryGetContainingContainer(original, out _), Is.False);

            Assert.That(actions.PerformAction(host, actionEntity), Is.True);
            Assert.That(SEntMan.HasComponent<GhostRoleComponent>(clone), Is.False);
            var emp = new EmpPulseEvent(0f, false, false, TimeSpan.FromSeconds(10), null);
            SEntMan.EventBus.RaiseLocalEvent(clone, ref emp);
            Assert.That(emp.Affected && emp.Disabled, Is.True);
            Assert.That(SEntMan.GetComponent<GeminiProjectionComponent>(clone).Deployed, Is.False);
            Assert.That(actions.PerformAction(host, actionEntity), Is.False);

            Assert.That(inventory.TryUnequip(host, "belt", silent: true, force: true), Is.True);
            Assert.That(data.CloneUid, Is.Null);
            Assert.That(mind.Comp.OwnedEntity, Is.Not.EqualTo(clone));
            Assert.That(roles.MindHasRole<GeminiRoleComponent>(mind.Owner), Is.False);
            Assert.That(SEntMan.EntityExists(original), Is.True);
        });

        await RunTicksSync(1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(clone), Is.False);
            Assert.That(SEntMan.EntityExists(copy), Is.False);
            Assert.That(SEntMan.EntityExists(original), Is.True);
        });
    }

    [Test]
    public async Task GeminiDoesNotCreateTankContentsAndRecallsOnDeath()
    {
        await Server.WaitAssertion(() =>
        {
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            var host = SEntMan.SpawnAttachedTo("MobHuman", position);
            var projector = SEntMan.SpawnAttachedTo("ClothingBeltGeminiHoloProjector", position);
            var tank = SEntMan.SpawnAttachedTo("ClothingBackpackFirefighterTank", position);
            var inventory = SEntMan.System<InventorySystem>();
            var actions = SEntMan.System<SharedActionsSystem>();
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            var damage = SEntMan.System<DamageableSystem>();
            var data = SEntMan.GetComponent<CloneProjectorComponent>(projector);
            Assert.That(inventory.TryEquip(host, tank, "back", silent: true, force: true), Is.True);
            Assert.That(inventory.TryEquip(host, projector, "belt", silent: true, force: true), Is.True);
            var action = data.ActionEntity!.Value;
            var actionEntity = (action, SEntMan.GetComponent<ActionComponent>(action));
            Assert.That(actions.PerformAction(host, actionEntity), Is.True);
            var clone = data.CloneUid!.Value;
            Assert.That(inventory.TryGetSlotEntity(clone, "back", out var copiedTank), Is.True);
            Assert.That(solutions.TryGetSolution(tank, "tank", out _, out var original), Is.True);
            Assert.That(solutions.TryGetSolution(copiedTank!.Value, "tank", out _, out var copy), Is.True);
            Assert.That(original!.Volume > 0, Is.True);
            Assert.That(copy!.Volume == 0, Is.True);

            var hit = new DamageSpecifier { DamageDict = { ["Blunt"] = 50 } };
            damage.TryChangeDamage(clone, hit, ignoreResistances: true, canMiss: false);
            Assert.That(SEntMan.GetComponent<GeminiProjectionComponent>(clone).Deployed, Is.False);
            Assert.That(SEntMan.HasComponent<GhostRoleComponent>(clone), Is.False);
            Assert.That(damage.GetTotalDamage(host) > 0, Is.True);
            var integrity = damage.GetTotalDamage(clone);
            damage.TryChangeDamage(clone, hit, ignoreResistances: true, canMiss: false);
            Assert.That(damage.GetTotalDamage(clone), Is.EqualTo(integrity));
            Assert.That(actions.PerformAction(host, actionEntity), Is.False);
        });
    }
}
