// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Server.Achievements;
using Content.Arcane.Server.Gambling;
using Content.Arcane.Shared.Gambling;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests._Arcane;

[EnsureCVar(Side.Server, typeof(ArcaneCVars), nameof(ArcaneCVars.AchievementsEnabled), true)]
public sealed class GamblingTennisRuntimeTest : InteractionTest
{
    private const string Slot = "TestArcaneP18Slot";
    private const string Coin = "TestArcaneP18Coin";
    private const string Claw = "TestArcaneP18Claw";
    private const string PrizeItem = "TestArcaneP18PrizeItem";
    private const string DeliveredPrize = "TestArcaneP18DeliveredPrize";
    private const string UnlimitedCash = "TestArcaneP18UnlimitedCash";
    private EntityUid? _mind;

    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: TestArcaneP18PrizeItem
  parent: BaseItem

- type: prize
  id: TestArcaneP18DeliveredPrize
  weight: 1
  prizeTable: !type:AllSelector
    children:
    - id: TestArcaneP18PrizeItem
    - id: TestArcaneP18PrizeItem

- type: entity
  id: TestArcaneP18Slot
  parent: SlotMachine
  components:
  - type: ApcPowerReceiver
    needsPower: false
  - type: SlotMachine
    doAfterTime: 0.75
    prizes: [TestArcaneP18DeliveredPrize]

- type: entity
  id: TestArcaneP18Coin
  parent: CoinFlipper
  components:
  - type: ApcPowerReceiver
    needsPower: false
  - type: CoinFlipper
    doAfterTime: 0.75
  - type: P18GamblingRuntimeProbe

- type: entity
  id: TestArcaneP18Claw
  parent: ClawMachine
  components:
  - type: ApcPowerReceiver
    needsPower: false
  - type: ClawMachine
    doAfterTime: 0.75
    prizes: [TestArcaneP18DeliveredPrize]
    # Keep the actual inherited evilPrizes: this must detect an added ClawLose.
  - type: P18GamblingRuntimeProbe

- type: entity
  id: TestArcaneP18UnlimitedCash
  parent: SpaceCash
  components:
  - type: Stack
    unlimited: true

""";

    public override async Task DoSetup()
    {
        await base.DoSetup();
        await OverrideCVar(Side.Server, ArcaneCVars.AchievementsExcludedPresets, "");
        await OverrideCVar(Side.Server, ArcaneCVars.AchievementsExcludedMaps, "");
        await Server.WaitPost(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            _mind = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(_mind.Value, SPlayer);
            SEntMan.System<SharedRoleSystem>().MindAddJobRole(_mind.Value, silent: true, jobPrototype: "Passenger");
            SEntMan.AddComponent<P18GamblingRuntimeProbeComponent>(SPlayer);
        });
    }

    protected override async Task TearDown()
    {
        await Server.WaitPost(() =>
        {
            if (_mind is { } mind)
                SEntMan.System<SharedMindSystem>().WipeMind(mind);
        });
        await base.TearDown();
    }

    [Test]
    public async Task SlotsCommitOneStakeAndFinishAfterWithdrawalAndDeparture()
    {
        await SpawnTarget(Slot);
        var machine = STarget!.Value;
        await SetPower(machine, true);
        await InteractUsing("SpaceCash", 750);
        EntityUid cash = default;
        await Server.WaitAssertion(() => cash = Slots.GetItemOrNull(machine, "money")!.Value);

        await Activate(awaitDoAfters: false);
        await Activate(awaitDoAfters: false);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Stack.GetCount((cash, null)), Is.EqualTo(500), "Repeated activation charged another stake.");
            Assert.That(Pending(machine).Length, Is.EqualTo(1));
            Assert.That(CountPrizeItems(), Is.Zero, "A positive delay delivered synchronously.");
            Assert.That(Slots.TryEject(machine, "money", SPlayer, out var ejected, doAfter: false), Is.True);
            Assert.That(ejected, Is.EqualTo(cash));
            MovePlayer(5);
        });
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Stack.GetCount((cash, null)), Is.EqualTo(500));
            Assert.That(CountPrizeItems(), Is.EqualTo(2), "The complete table must be delivered exactly once.");
            Assert.That(PlayerProbe.Prizes, Is.EqualTo(new[] { DeliveredPrize }));
            Assert.That(PlayerProbe.UndeliveredEvents, Is.Zero);
        });

        // A cancelled paid spin must not later deliver when power returns.
        await Server.WaitPost(() => MovePlayer(0));
        await InteractUsing("SpaceCash", 500);
        await Activate(awaitDoAfters: false);
        await SetPower(machine, false);
        await RunSeconds(1.1f);
        await SetPower(machine, true);
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(CountPrizeItems(), Is.EqualTo(2));
            Assert.That(Stack.GetCount((Slots.GetItemOrNull(machine, "money")!.Value, null)), Is.EqualTo(250));
            Assert.That(Slots.TryEject(machine, "money", SPlayer, out _, doAfter: false), Is.True);
        });

        await DepositUnlimitedCash(machine, 750);
        await Activate(awaitDoAfters: false);
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Pending(machine), Is.Empty);
            Assert.That(CountPrizeItems(), Is.EqualTo(2), "Unlimited cash bought a free spin.");
            Assert.That(Stack.GetCount((Slots.GetItemOrNull(machine, "money")!.Value, null)), Is.EqualTo(750));
        });

        await InteractUsing("Emag", awaitDoAfters: false);
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<EmaggedComponent>(machine), Is.True);
            Assert.That(CountPrizeItems(), Is.EqualTo(4), "The free spin did not deliver the configured prize.");
            Assert.That(PlayerProbe.Prizes, Is.EqualTo(new[] { DeliveredPrize }), "The free spin was credited as a paid win.");
            Assert.That(Stack.GetCount((Slots.GetItemOrNull(machine, "money")!.Value, null)), Is.EqualTo(750));
        });
    }

    [TestCase(true)]
    public async Task CoinUsesTheCommittedWholeStackRatherThanLaterDeposits(bool win)
    {
        await SpawnTarget(Coin);
        var machine = STarget!.Value;
        await SetPower(machine, true);
        await InteractUsing("SpaceCash", 117);
        await Activate(awaitDoAfters: false);
        await Activate(awaitDoAfters: false);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Slots.GetItemOrNull(machine, "money"), Is.Null, "The entire old stake was not consumed at start.");
            Assert.That(Pending(machine).Length, Is.EqualTo(1));
            ArmFinalRng(machine, SeedFor(win));
        });
        await InteractUsing("SpaceCash", 31);
        EntityUid nextDeposit = default;
        await Server.WaitAssertion(() => nextDeposit = Slots.GetItemOrNull(machine, "money")!.Value);
        await Server.WaitPost(() => MovePlayer(5));
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(FreeCashExcept(nextDeposit), Is.EqualTo(win ? 234 : 0), "Payout did not use the captured old stake.");
            Assert.That(Stack.GetCount((nextDeposit, null)), Is.EqualTo(31), "The next deposit was changed by the old wager.");
        });

        // Give the next wager a winning roll, then cancel it through the real power system.
        await Server.WaitPost(() => MovePlayer(0));
        await Activate(awaitDoAfters: false);
        await Server.WaitPost(() => ArmFinalRng(machine, SeedFor(true)));
        await SetPower(machine, false);
        await RunSeconds(1.1f);
        await SetPower(machine, true);
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() => Assert.That(FreeCashExcept(EntityUid.Invalid), Is.EqualTo(win ? 234 : 0)));

        await DepositUnlimitedCash(machine, 500);
        await Activate(awaitDoAfters: false);
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Pending(machine), Is.Empty);
            Assert.That(Stack.GetCount((Slots.GetItemOrNull(machine, "money")!.Value, null)), Is.EqualTo(500));
            Assert.That(FreeCashExcept(Slots.GetItemOrNull(machine, "money")!.Value), Is.EqualTo(win ? 234 : 0));
        });
    }

    private Task DepositUnlimitedCash(EntityUid machine, int amount)
        => Server.WaitAssertion(() =>
        {
            var cash = SEntMan.SpawnAtPosition(UnlimitedCash, Position(machine));
            Stack.SetCount((cash, null), amount);
            Assert.That(Stack.IsUnlimited((cash, null)), Is.True);
            Assert.That(Slots.TryInsert(machine, "money", cash, SPlayer), Is.True);
        });

    [Test]
    public async Task ClawCancelsOnMovementAndPowerButEmagAlwaysDelivers()
    {
        await SpawnTarget(Claw);
        var machine = STarget!.Value;
        await SetPower(machine, true);
        await Activate(awaitDoAfters: false);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Pending(SPlayer).Length, Is.EqualTo(1));
            MovePlayer(5);
        });
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() => Assert.That(PlayerProbe.Prizes, Is.Empty));

        await Server.WaitPost(() => MovePlayer(0));
        await Activate(awaitDoAfters: false);
        await SetPower(machine, false);
        await RunSeconds(1.1f);
        await SetPower(machine, true);
        await Server.WaitAssertion(() => Assert.That(PlayerProbe.Prizes, Is.Empty));

        await InteractUsing("Emag", awaitDoAfters: false);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<EmaggedComponent>(machine), Is.True));
        await DropAll();
        await Activate(awaitDoAfters: false);
        await Server.WaitPost(() => ArmFinalRng(machine, SeedForLowFloat(), SPlayer));
        await RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(PlayerProbe.Prizes, Is.EqualTo(new[] { "ClawGameEvil" }));
            Assert.That(PlayerProbe.UndeliveredEvents, Is.Zero);
            Assert.That(PlayerProbe.DeliveredEntities.All(uid => SEntMan.EntityExists(uid)), Is.True);
            var item = PlayerProbe.DeliveredEntities.Single();
            Assert.That(SEntMan.HasComponent<EvilClawPrizeComponent>(item), Is.True);
            var original = SEntMan.GetComponent<MetaDataComponent>(item).EntityName;
            Assert.That(Identity.Name(item, SEntMan), Is.EqualTo(Loc.GetString("arcane-clawmachine-evil-prize-name", ("name", original))));
            Assert.That(CountPrizeItems(), Is.Zero, "A cancelled earlier normal play eventually delivered.");
        });
    }

    private ItemSlotsSystem Slots => SEntMan.System<ItemSlotsSystem>();
    private P18GamblingRuntimeProbeComponent PlayerProbe => SEntMan.GetComponent<P18GamblingRuntimeProbeComponent>(SPlayer);

    private Content.Shared.DoAfter.DoAfter[] Pending(EntityUid user)
        => SEntMan.GetComponent<DoAfterComponent>(user).DoAfters.Values.Where(d => !d.Cancelled && !d.Completed).ToArray();

    private void MovePlayer(float x)
        => Transform.SetCoordinates(SPlayer, Xform(SPlayer), ToServer(PlayerCoords).Offset(new Vector2(x, 0)));

    private async Task SetPower(EntityUid machine, bool powered)
    {
        await Server.WaitPost(() => SEntMan.System<PowerReceiverSystem>().SetPowerDisabled(machine, !powered));
        for (var tick = 0; tick < 30; tick++)
        {
            var matches = false;
            await Server.WaitPost(() => matches = SEntMan.System<PowerReceiverSystem>().IsPowered(machine) == powered);
            if (matches)
                break;
            await RunTicks(1);
        }
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<PowerReceiverSystem>().IsPowered(machine), Is.EqualTo(powered)));
    }

    private int CountPrizeItems()
        => SEntMan.AllEntities<MetaDataComponent>().Count(e => e.Comp.EntityPrototype?.ID == PrizeItem
            && Xform(e.Owner).MapID == MapId);

    private int FreeCashExcept(EntityUid excluded)
        => SEntMan.AllEntities<StackComponent>().Where(e => e.Owner != excluded && SEntMan.HasComponent<CashComponent>(e.Owner)
            && Xform(e.Owner).MapID == MapId).Sum(e => Stack.GetCount((e.Owner, e.Comp)));

    private void ArmFinalRng(EntityUid machine, int seed, EntityUid? doAfterUser = null)
    {
        // A test-only system seeds RNG before the ordinary DoAfter update; its arguments remain unchanged.
        var pending = Pending(doAfterUser ?? machine);
        Assert.That(pending.Length, Is.EqualTo(1));
        SEntMan.GetComponent<P18GamblingRuntimeProbeComponent>(machine).FinalSeed = seed;
    }

    private static int SeedFor(bool win)
    {
        var sample = new RobustRandom();
        for (var seed = 0; seed < 100; seed++)
        {
            sample.SetSeed(seed);
            if (sample.Prob(0.5f) == win)
                return seed;
        }
        throw new InvalidOperationException("No deterministic coin seed found.");
    }

    private static int SeedForLowFloat()
    {
        var sample = new RobustRandom();
        for (var seed = 0; seed < 100; seed++)
        {
            sample.SetSeed(seed);
            if (sample.NextFloat() < 0.5f)
                return seed;
        }
        throw new InvalidOperationException("No deterministic low-roll seed found.");
    }
}

[RegisterComponent]
public sealed partial class P18GamblingRuntimeProbeComponent : Component
{
    public int? FinalSeed;
    public readonly List<string> Prizes = [];
    public readonly List<EntityUid> DeliveredEntities = [];
    public int UndeliveredEvents;
}

// Loaded from the integration-test assembly, as InteractionTestSystem is. Unique probe anchors avoid production pairs.
public sealed partial class P18GamblingRuntimeProbeSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SharedDoAfterSystem));
        SubscribeLocalEvent<P18GamblingRuntimeProbeComponent, PrizeWonEvent>(OnPrize);
    }

    private void OnPrize(Entity<P18GamblingRuntimeProbeComponent> ent, ref PrizeWonEvent args)
    {
        ent.Comp.Prizes.Add(args.Prize.Id);
        ent.Comp.DeliveredEntities.Add(args.PrizeEntity);
        if (!Exists(args.PrizeEntity) || TerminatingOrDeleted(args.PrizeEntity) || EntityManager.IsQueuedForDeletion(args.PrizeEntity))
            ent.Comp.UndeliveredEvents++;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<P18GamblingRuntimeProbeComponent>();
        while (query.MoveNext(out var comp))
            if (comp.FinalSeed is { } seed)
                _random.SetSeed(seed);
    }
}
// Arcane-End
