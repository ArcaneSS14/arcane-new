#nullable enable
using System.Linq;
using Content.Arcane.Client.Paper;
using Content.Arcane.Server.Paper;
using Content.Arcane.Shared.ChameleonStamp;
using Content.Arcane.Shared.Paper;
using Content.Goobstation.Shared.Devil.Contract;
using Content.Trauma.Shared.Paper;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Paper;
using Content.Trauma.Common.Paper;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PaperPortTest : InteractionTest
{

    [Test]
    public async Task FieldsUseCurrentDocumentAndServerSignature()
    {
        const string original = "[bold]Name:[/bold] [form] [form] [check] [signature]";
        await SpawnTarget("Paper");
        await PlaceInHands("Pen");
        await Server.WaitPost(() => SEntMan.System<PaperSystem>().SetContent(STarget!.Value, original));
        await Activate();
        await Pair.RunUntilSynced();
        Assert.That(TryGetBui(PaperComponent.PaperUiKey.Key, out var bui), Is.True);
        Assert.That(bui, Is.TypeOf<ArcanePaperBoundUserInterface>());

        await SendBui(PaperComponent.PaperUiKey.Key,
            new PaperFieldMessage(PaperFieldKind.Signature, 0, "Forged", original));
        await AssertContent(original);
        const string hostile = "[signature][color=red]Somebody[/color]";
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperFieldMessage(PaperFieldKind.Form, 1, hostile, original));
        var filled = "[bold]Name:[/bold] [form] " + FormattedMessage.EscapeText(hostile) + " [check] [signature]";
        await AssertContent(filled);
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperFieldMessage(PaperFieldKind.Form, 0, "Stale", original));
        await AssertContent(filled);

        await Server.WaitPost(() => SEntMan.System<MetaDataSystem>().SetEntityName(SPlayer, "A [signature]"));
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperFieldMessage(PaperFieldKind.Signature, 0, "", filled));
        filled = filled.Replace(" [signature]", " " + FormattedMessage.EscapeText("A [signature]"));
        await AssertContent(filled);
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<PaperComponent>(STarget!.Value).StampedBy, Is.Empty,
            "An inline signature leaves other fields available until the document is stamped."));

        await InteractUsing("RubberStampCaptain");
        var finalized = filled.Replace("[form]", "").Replace("[check]", "☐");
        await AssertContent(finalized);
        await Server.WaitAssertion(() =>
        {
            var paper = SEntMan.GetComponent<PaperComponent>(STarget!.Value);
            Assert.That(paper.StampedBy.Single().StampLargeIcon, Does.EndWith("large_stamp-cap.png"));
            var copy = SEntMan.SpawnEntity("Paper", SEntMan.GetCoordinates(PlayerCoords));
            SEntMan.System<PaperSystem>().CopyStamps((STarget.Value, paper), copy);
            Assert.That(SEntMan.GetComponent<PaperComponent>(copy).StampedBy, Is.EqualTo(paper.StampedBy));
        });
        await CloseBui(PaperComponent.PaperUiKey.Key);
    }

    [Test]
    public async Task InlineSignaturesPreserveDevilContractPhasesAndLockOtherFields()
    {
        var content = string.Empty;
        var shoesBefore = 0;
        EntityUid owner = default;
        await SpawnTarget("PaperDevilContractBlank");
        await PlaceInHands("Pen");
        await Server.WaitAssertion(() =>
        {
            var coords = SEntMan.GetCoordinates(PlayerCoords);
            owner = SEntMan.SpawnEntity("InteractionTestMob", coords);
            Assert.That(HandSys.TryPickup(owner, SEntMan.SpawnEntity("Pen", coords)), Is.True);
            SEntMan.System<MetaDataSystem>().SetEntityName(SPlayer, "Victim");
            SEntMan.System<MetaDataSystem>().SetEntityName(owner, "Contractor");
            SEntMan.GetComponent<DevilContractComponent>(STarget!.Value).ContractOwner = owner;
            var contractee = Loc.GetString("devil-contract-contractee");
            content = $"{contractee}: brokeness\n{contractee}: strength\n[signature] [signature] [signature] [form]";
            SEntMan.System<PaperSystem>().SetContent(STarget.Value, content);
            shoesBefore = CountShoes();
            Assert.That(Sign(owner), Is.False, "The owner cannot sign before the victim.");
        });
        await Activate();
        await Pair.RunUntilSynced();
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperFieldMessage(PaperFieldKind.Signature, 0, "", content));
        await Server.WaitAssertion(() =>
        {
            var uid = STarget!.Value;
            var paper = SEntMan.GetComponent<PaperComponent>(uid);
            var contract = SEntMan.GetComponent<DevilContractComponent>(uid);
            Assert.That(contract.IsVictimSigned, Is.True);
            Assert.That(paper.EditingDisabled, Is.True);
            Assert.That(Sign(SPlayer), Is.False, "Only the owner may supply the second signature.");
            Assert.That(SEntMan.System<PaperFieldSystem>().TryFill((uid, paper), owner,
                PaperFieldKind.Form, 0, "Changed terms", paper.Content), Is.False);
            Assert.That(Sign(owner), Is.True, "The contract's editing lock must still permit its owner's signature.");
            Assert.That(contract.IsContractFullySigned, Is.True);
            Assert.That(CountShoes(), Is.EqualTo(shoesBefore + 1), "Completing the contract grants its real item clause once.");
            var duplicate = new PaperSignedEvent(owner);
            SEntMan.EventBus.RaiseLocalEvent(uid, ref duplicate);
            Assert.That(CountShoes(), Is.EqualTo(shoesBefore + 1), "A duplicate completion event must not grant the item again.");
            var completedText = paper.Content;
            Assert.That(Sign(owner), Is.False, "A third signature cannot apply a completed contract again.");
            Assert.That(paper.Content, Is.EqualTo(completedText));
            Assert.That(SEntMan.System<SignatureSystem>().TrySignPaper((uid, paper), owner,
                HandSys.GetActiveItem(owner)!.Value), Is.False, "The existing signing verb has the same one-time completion rule.");
        });
        await CloseBui(PaperComponent.PaperUiKey.Key);

        bool Sign(EntityUid actor)
        {
            var paper = SEntMan.GetComponent<PaperComponent>(STarget!.Value);
            return SEntMan.System<PaperFieldSystem>().TryFill((STarget.Value, paper), actor,
                PaperFieldKind.Signature, 0, "", paper.Content);
        }

        int CountShoes() => SEntMan.EntityQuery<MetaDataComponent>()
            .Count(meta => meta.EntityPrototype?.ID == "ClothingShoesRealJordansAutographed");
    }

    private async Task AssertContent(string expected) => await Server.WaitAssertion(() =>
        Assert.That(SEntMan.GetComponent<PaperComponent>(STarget!.Value).Content, Is.EqualTo(expected)));
}
