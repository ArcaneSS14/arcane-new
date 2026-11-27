// Arcane-Start: radio frequencies and mute displays are per listener
using System.Linq;
using System.Reflection;
using Content.Arcane.Client.Radio;
using Content.Arcane.Server.Radio;
using Content.Arcane.Shared.Radio;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Radio;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class HeadsetChannelRuntimeTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    private static readonly ProtoId<RadioChannelPrototype> Command = "Command";
    private static readonly ProtoId<RadioChannelPrototype> Engineering = "Engineering";

    [Test]
    public async Task FractionalMuteAndOtherViewerKeepOwnDisplay()
    {
        EntityUid headset = default;
        EntityUid other = default;
        NetEntity networkHeadset = default;
        var commandFrequency = 0;
        var engineeringFrequency = 0;
        await Server.WaitAssertion(() =>
        {
            headset = SEntMan.SpawnAttachedTo("ClothingHeadsetAltEngineering", MapData.GridCoords);
            other = SEntMan.SpawnAttachedTo("MobHuman", MapData.GridCoords);
            var prototypes = Server.ResolveDependency<IPrototypeManager>();
            commandFrequency = prototypes.Index(Command).Frequency.Value;
            engineeringFrequency = prototypes.Index(Engineering).Frequency.Value;
            Assert.That(commandFrequency / 100, Is.EqualTo(engineeringFrequency / 100));
            Assert.That(commandFrequency, Is.Not.EqualTo(engineeringFrequency));
            var ui = SEntMan.System<SharedUserInterfaceSystem>();
            ui.SetUi(headset, HeadsetChannelUiKey.Key,
                new InterfaceData("HeadsetChannelBoundUserInterface", interactionRange: 0, requireInputValidation: false));
            Assert.That(ui.TryOpenUi(headset, HeadsetChannelUiKey.Key, SPlayer), Is.True);
            networkHeadset = SEntMan.GetNetEntity(headset);
        });

        await Pair.RunTicksSync(20);
        await Client.WaitAssertion(() =>
        {
            var clientHeadset = CEntMan.GetEntity(networkHeadset);
            var ui = CEntMan.System<SharedUserInterfaceSystem>();
            Assert.That(ui.TryGetOpenUi<HeadsetChannelBoundUserInterface>(clientHeadset, HeadsetChannelUiKey.Key, out var bui), Is.True);
            var field = typeof(HeadsetChannelBoundUserInterface).GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var window = (HeadsetChannelWindow) field.GetValue(bui)!;
            typeof(HeadsetChannelBoundUserInterface).GetMethod("Open", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(bui, null);
            Assert.That(field.GetValue(bui), Is.SameAs(window), "A repeated open must not create another window.");
            var rows = window.FindControl<BoxContainer>("ChannelList").Children.OfType<BoxContainer>().ToArray();
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var command = Loc.GetString(prototypes.Index(Command).Name);
            var row = rows.Single(r => r.Children.OfType<Label>().Any(label => label.Text == command));
            var checkbox = row.Children.OfType<CheckBox>().Single();
            Assert.That(checkbox.Pressed, Is.True);
            window.ChannelMuteToggled!.Invoke(commandFrequency, true);
        });

        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var system = Server.System<HeadsetChannelMuteSystem>();
            Assert.That(system.IsMuted(ServerSession!.UserId, commandFrequency), Is.True);
            Assert.That(system.IsMuted(ServerSession!.UserId, engineeringFrequency), Is.False);
            var ui = SEntMan.System<SharedUserInterfaceSystem>();
            Assert.That(ui.TryOpenUi(headset, HeadsetChannelUiKey.Key, other), Is.True);
            Assert.That(ui.IsUiOpen(headset, HeadsetChannelUiKey.Key, SPlayer), Is.True);
        });
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() =>
        {
            var ui = CEntMan.System<SharedUserInterfaceSystem>();
            Assert.That(ui.TryGetOpenUi<HeadsetChannelBoundUserInterface>(CEntMan.GetEntity(networkHeadset), HeadsetChannelUiKey.Key, out var bui), Is.True);
            var window = (HeadsetChannelWindow) typeof(HeadsetChannelBoundUserInterface)
                .GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bui)!;
            var command = Loc.GetString(Client.ResolveDependency<IPrototypeManager>().Index(Command).Name);
            var row = window.FindControl<BoxContainer>("ChannelList").Children.OfType<BoxContainer>()
                .Single(r => r.Children.OfType<Label>().Any(label => label.Text == command));
            Assert.That(row.Children.OfType<CheckBox>().Single().Pressed, Is.False);
        });
        await Server.WaitPost(() =>
        {
            var ui = SEntMan.System<SharedUserInterfaceSystem>();
            ui.CloseUi(headset, HeadsetChannelUiKey.Key, other);
            ui.CloseUi(headset, HeadsetChannelUiKey.Key, SPlayer);
        });
    }
}
// Arcane-End
