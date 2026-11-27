// Arcane-Start: a cleared number must not send the previously selected room
using Content.Arcane.Client.InfinityDorm;
using Content.Arcane.Common.CCVar;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests._Arcane.InfinityDorm;

[TestFixture]
public sealed class InfinityDormWindowTest : GameTest
{
    [Test]
    public async Task ClearingNumberDisablesConfirmation()
    {
        await OverrideCVar(Side.Server, ArcaneCVars.InfinityDormsEnabled, true);
        await Client.WaitAssertion(() =>
        {
            using var window = new InfinityDormTeleporterWindow();
            var input = window.FindControl<LineEdit>("NumberInput");
            var confirm = window.FindControl<Button>("Confirm");
            Assert.That(confirm.Disabled, Is.True);
            input.SetText("123", true);
            Assert.That(window.SelectedNumber, Is.EqualTo(123));
            Assert.That(confirm.Disabled, Is.False);
            input.SetText("", true);
            Assert.That(window.SelectedNumber, Is.Zero);
            Assert.That(confirm.Disabled, Is.True);
            input.SetText("0", true);
            Assert.That(window.SelectedNumber, Is.Zero);
            Assert.That(confirm.Disabled, Is.True);
            input.SetText("456", true);
            Assert.That(window.SelectedNumber, Is.EqualTo(456));
            Assert.That(confirm.Disabled, Is.False);
        });
    }
}
// Arcane-End
