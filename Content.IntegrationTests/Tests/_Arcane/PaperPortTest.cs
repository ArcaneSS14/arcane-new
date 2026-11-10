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
using Content.Shared.Stunnable;
using Content.Trauma.Common.Paper;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PaperPortTest : InteractionTest
{
    [TestCase("no-pen", "Unsaved draft")]
    [TestCase("disabled", "Unsaved draft")]
    [TestCase("sealed", "Unsaved draft")]
    [TestCase("disabled", "")]
    [TestCase("stunned", "Unsaved draft")]
    public async Task RejectedSavePreservesDraftAndAllowsRetry(string rejection, string draft)
    {
        await SpawnTarget("Paper");
        await PlaceInHands("Pen");
        await Server.WaitPost(() => SEntMan.System<PaperSystem>().SetContent(STarget!.Value, "Original"));
        await Interact();
        await Pair.RunUntilSynced();
        ArcanePaperWindow window = null!;
        Button save = null!;
        await Client.WaitAssertion(() =>
        {
            Assert.That(TryFindWindow(out ArcanePaperWindow? found), Is.True);
            window = found!;
            save = window.FindControl<Button>("SaveButton");
            window.Input.TextRope = Rope.Leaf.Empty;
            window.Input.CursorPosition = new TextEdit.CursorPos();
            window.Input.InsertAtCursor(draft);
        });

        if (rejection == "no-pen")
            await Drop();
        else if (rejection == "stunned")
            // InteractionTestMob has no status-effect container; use the real blocker component.
            await Server.WaitPost(() => SEntMan.EnsureComponent<StunnedComponent>(SPlayer));
        else
            await Server.WaitPost(() =>
            {
                var paper = SEntMan.GetComponent<PaperComponent>(STarget!.Value);
                if (rejection == "disabled")
                    paper.EditingDisabled = true;
                else
                    paper.StampedBy.Add(new StampDisplayInfo { StampedName = "sealed" });
            });

        await ClickSave();
        await Pair.RunUntilSynced();
        if (rejection == "stunned")
        {
            await Server.WaitAssertion(() => Assert.That(SEntMan.System<SharedStunSystem>().TryUnstun(SPlayer), Is.True));
            await Pair.RunUntilSynced();
            // An unrelated/late reply must not acknowledge the retained draft.
            await Server.WaitPost(() => SEntMan.System<SharedUserInterfaceSystem>().ServerSendUiMessage(
                STarget!.Value, PaperComponent.PaperUiKey.Key, new PaperComponent.PaperSaveResultMessage(Guid.NewGuid(), true), SPlayer));
            await Pair.RunUntilSynced();
        }
        await AssertContent("Original");
        await Client.WaitAssertion(() =>
        {
            Assert.That(Rope.Collapse(window.Input.TextRope), Is.EqualTo(draft));
            Assert.That(save.Disabled, Is.False, "A rejected save must allow retrying the retained draft.");
            // Another viewer or prediction correction can switch the shared mode.
            window.Populate(new("Original", [], PaperComponent.PaperAction.Read));
            window.Populate(new("Original", [], PaperComponent.PaperAction.Write));
            Assert.That(Rope.Collapse(window.Input.TextRope), Is.EqualTo(draft));
        });
        if (rejection == "no-pen")
            await PlaceInHands("Pen");
        await Server.WaitPost(() =>
        {
            var paper = SEntMan.GetComponent<PaperComponent>(STarget!.Value);
            paper.EditingDisabled = false;
            paper.StampedBy.Clear();
        });
        await Interact();
        await Pair.RunUntilSynced();
        await ClickSave();
        await Pair.RunUntilSynced();
        await AssertContent(draft);
        await CloseBui(PaperComponent.PaperUiKey.Key);

        async Task ClickSave()
        {
            GUIBoundKeyEventArgs down = null!;
            GUIBoundKeyEventArgs up = null!;
            await Client.WaitPost(() =>
            {
                var point = new ScreenCoordinates(save.GlobalPixelPosition + save.PixelSize / 2, save.Window?.Id ?? default);
                var relative = point.Position / save.UIScale - save.GlobalPosition;
                var pixels = point.Position - save.GlobalPixelPosition;
                down = new(EngineKeyFunctions.UIClick, BoundKeyState.Down, point, default, relative, pixels);
                up = new(EngineKeyFunctions.UIClick, BoundKeyState.Up, point, default, relative, pixels);
            });
            await Client.DoGuiEvent(save, down);
            await Pair.ReallyBeIdle(1);
            await Client.DoGuiEvent(save, up);
            await Pair.ReallyBeIdle(1);
        }
    }

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
    public async Task WritingRequiresCurrentPenAndEditableDocument()
    {
        await SpawnTarget("Paper");
        await Server.WaitPost(() => SEntMan.System<PaperSystem>().SetContent(STarget!.Value, "[form]"));
        await Server.WaitAssertion(() => Assert.That(Fill(), Is.False, "Empty hand is not a writing tool."));
        await PlaceInHands("Pen");
        await Server.WaitAssertion(() =>
        {
            var paper = SEntMan.GetComponent<PaperComponent>(STarget!.Value);
            paper.EditingDisabled = true;
            Assert.That(Fill(), Is.False);
            paper.EditingDisabled = false;
            paper.StampedBy.Add(new StampDisplayInfo { StampedName = "sealed" });
            Assert.That(Fill(), Is.False, "An open UI cannot bypass a later seal.");
            paper.StampedBy.Clear();
            Assert.That(Fill(), Is.True);
        });

        bool Fill() => SEntMan.System<PaperFieldSystem>().TryFill(
            (STarget!.Value, SEntMan.GetComponent<PaperComponent>(STarget.Value)), SPlayer,
            PaperFieldKind.Form, 0, "Written", "[form]");
    }

    [Test]
    public async Task AdvancedPenCanFillNewFieldsAfterStamping()
    {
        const string content = "[form] [signature]";
        await SpawnTarget("Paper");
        await InteractUsing("RubberStampCaptain");
        await PlaceInHands("CyberPen");
        await Interact();
        await Pair.RunUntilSynced();
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperComponent.PaperInputTextMessage(content));
        await AssertContent(content);
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperFieldMessage(PaperFieldKind.Form, 0, "Written", content));
        const string remaining = "Written [signature]";
        await AssertContent(remaining);
        await Client.WaitAssertion(() =>
        {
            using var window = new ArcanePaperWindow();
            window.Populate(new PaperComponent.PaperBoundUserInterfaceState(remaining,
                [new StampDisplayInfo { StampedName = "sealed" }]));
            var submitted = false;
            window.OnFieldSubmitted += (kind, index, value, snapshot) =>
            {
                submitted = true;
                Assert.That(kind, Is.EqualTo(PaperFieldKind.Signature));
                Assert.That(snapshot, Is.EqualTo(remaining));
            };
            window.OpenField(PaperFieldKind.Signature, 0);
            Assert.That(submitted, Is.True, "The client must not override the advanced pen's server permission.");
        });
        await PlaceInHands("Pen");
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperFieldMessage(PaperFieldKind.Signature, 0, "", remaining));
        await AssertContent(remaining);
        await PlaceInHands("CyberPen");
        await Server.WaitPost(() => SEntMan.System<MetaDataSystem>().SetEntityName(SPlayer, "Advanced Writer"));
        await SendBui(PaperComponent.PaperUiKey.Key, new PaperFieldMessage(PaperFieldKind.Signature, 0, "", remaining));
        await AssertContent("Written Advanced Writer");
        await CloseBui(PaperComponent.PaperUiKey.Key);
    }

    [Test]
    public async Task GraphicalStampsRetainTextFallbackAndChameleonState()
    {
        await SpawnTarget("RubberStampChameleon");
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<StampComponent>(STarget!.Value).StampLargeIcon,
            Does.EndWith("large_stamp-ok.png"), "The initial selected state has the same imprint as reselecting it."));
        await Activate();
        await Pair.RunUntilSynced();
        await SendBui(ChameleonStampUiKey.Key, Settings("RubberStampCentcom"));
        StampDisplayInfo graphic = default;
        await Server.WaitAssertion(() =>
        {
            var stamp = SEntMan.GetComponent<StampComponent>(STarget!.Value);
            Assert.That(stamp.StampLargeIcon, Does.EndWith("large_stamp-centcom.png"));
            graphic = new StampDisplayInfo { StampedName = stamp.StampedName, StampedColor = stamp.StampedColor,
                StampLargeIcon = stamp.StampLargeIcon };
        });
        await SendBui(ChameleonStampUiKey.Key, Settings("RubberStampWarden"));
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<StampComponent>(STarget!.Value).StampLargeIcon, Is.Null));
        await Client.WaitAssertion(() =>
        {
            using var widget = new ArcaneStampWidget { StampInfo = graphic };
            var texture = Client.ResolveDependency<IResourceCache>().GetResource<TextureResource>("/Textures/" + graphic.StampLargeIcon);
            Assert.That(((StyleBoxTexture) widget.PanelOverride!).Texture, Is.SameAs(texture.Texture));
            Assert.That(widget.MinSize, Is.EqualTo(texture.Texture.Size * 1.5f));
            Assert.That(widget.ModulateSelfOverride, Is.EqualTo(Color.White));
            widget.StampInfo = new StampDisplayInfo { StampedName = "Custom", StampedColor = Color.Blue };
            Assert.That(widget.MinSize, Is.EqualTo(System.Numerics.Vector2.Zero));
            Assert.That(widget.ModulateSelfOverride, Is.EqualTo(Color.Blue));
            Assert.That(((StyleBoxTexture) widget.PanelOverride!).Texture, Is.Not.SameAs(texture.Texture));
            widget.StampInfo = graphic;
            Assert.That(((StyleBoxTexture) widget.PanelOverride!).Texture, Is.SameAs(texture.Texture));
        });
        await CloseBui(ChameleonStampUiKey.Key);

        static ChameleonStampApplySettingsMessage Settings(string state) => new(
            "RubberStampApproved", Color.Blue, null, null, "Custom", "RubberStampCaptain", state);
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

    [Test]
    public void FieldParsingIgnoresEscapedAndParameterizedTagsAndEnforcesBounds()
    {
        const string content = "\\[form] [form=\"wrong\"] [form] [check] [signature]";
        Assert.That(PaperFields.TryFill(content, PaperFieldKind.Form, 0, "A [form]", 200, out var result), Is.True);
        Assert.That(result, Is.EqualTo("\\[form] [form=\"wrong\"] A \\[form] [check] [signature]"));
        Assert.That(PaperFields.TryFill(content, PaperFieldKind.Form, 1, "X", 200, out _), Is.False);
        Assert.That(PaperFields.TryFill(content, PaperFieldKind.Form, -1, "X", 200, out _), Is.False);
        Assert.That(PaperFields.TryFill(content, (PaperFieldKind) 255, 0, "X", 200, out _), Is.False);
        Assert.That(PaperFields.TryFill("[form]", PaperFieldKind.Form, 0, "[[[[", 6, out _), Is.False);
        Assert.That(PaperFields.TryFill("[form]", PaperFieldKind.Form, 0, "abcdef", 6, out result), Is.True);
        Assert.That(result, Is.EqualTo("abcdef"));
        Assert.That(PaperFields.TryFill("[form]", PaperFieldKind.Form, 0, "abcdefg", 6, out _), Is.False);
        Assert.That(PaperFields.TryFill("[form]", PaperFieldKind.Form, 0, "", 20, out _), Is.False);
        foreach (var choice in new[] { "☐", "✔", "✖" })
        {
            Assert.That(PaperFields.TryFill("[check]", PaperFieldKind.Check, 0, choice, 20, out result), Is.True);
            Assert.That(result, Is.EqualTo(choice));
        }
        Assert.That(PaperFields.TryFill("[check]", PaperFieldKind.Check, 0, "[signature]", 20, out _), Is.False);
        var display = PaperFields.ForDisplay("[form] [form] [signature]", false);
        Assert.That(display.Where(n => n.Name == "form").Select(n => n.Attributes[PaperFields.IndexAttribute].LongValue),
            Is.EqualTo(new long?[] { 0, 1 }));
        Assert.That(PaperFields.ForDisplay("[form]", true).First().Attributes[PaperFields.LockedAttribute].LongValue, Is.EqualTo(1));
        Assert.That(PaperFields.FinalizeDocument("\\[form] [form] [check] [signature]"), Is.EqualTo("\\[form]  ☐ "));
    }

    [Test]
    public void AuthorSuppliedFieldIndicesCannotRedirectControls()
    {
        const string text = "A: [form paperindex=0 paperlocked=0] B: [form]";
        var display = PaperFields.ForDisplay(text, false);
        var fields = display.Where(node => node.Name == "form").ToArray();
        Assert.That(fields, Has.Length.EqualTo(1));
        Assert.That(fields[0].Attributes[PaperFields.IndexAttribute].LongValue, Is.EqualTo(0));
        Assert.That(PaperFields.TryFill(text, PaperFieldKind.Form, 0, "B value", 200, out var result), Is.True);
        Assert.That(result, Is.EqualTo("A: [form paperindex=0 paperlocked=0] B: B value"));
    }

    private async Task AssertContent(string expected) => await Server.WaitAssertion(() =>
        Assert.That(SEntMan.GetComponent<PaperComponent>(STarget!.Value).Content, Is.EqualTo(expected)));
}
