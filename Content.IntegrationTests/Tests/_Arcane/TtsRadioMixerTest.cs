// Arcane-Start: keep radio settings distinct and honor the options controls
using System.Collections;
using System.Linq;
using System.Reflection;
using Content.Arcane.Client.TTS;
using Content.Arcane.Common.CCVar;
using Content.Client.Options.UI;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class TtsRadioMixerTest : GameTest
{
    [Test]
    public async Task FractionalChannelsHaveSeparateStagedSettings()
    {
        await OverrideCVar(Side.Client, ArcaneCVars.TTSRadioChannelMuted, "");
        await OverrideCVar(Side.Client, ArcaneCVars.TTSRadioChannelVolumes, "");
        await Client.WaitAssertion(() =>
        {
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            var tab = new TTSOptionsTab();
            var mixer = typeof(TTSOptionsTab).GetField("_mixer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(tab)!;
            var rowObjects = (IEnumerable) mixer.GetType().GetField("_rows", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mixer)!;
            var rows = rowObjects.Cast<object>().Select(row =>
            {
                var type = row.GetType();
                return (Frequency: (int) type.GetField("Item1")!.GetValue(row)!,
                    Slider: (Slider) type.GetField("Item3")!.GetValue(row)!);
            }).ToList();
            var group = rows.GroupBy(row => row.Frequency / 100)
                .First(g => g.Select(row => row.Frequency).Distinct().Count() > 1);
            var first = group.First();
            var second = group.First(row => row.Frequency != first.Frequency);
            var control = tab.Control;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;

            first.Slider.Value = 0.23f;
            Assert.That(second.Slider.Value, Is.EqualTo(1f));
            Assert.That(cfg.GetCVar(ArcaneCVars.TTSRadioChannelVolumes), Is.Empty, "Changes must remain staged.");
            Assert.That((bool) mixer.GetType().GetMethod("IsModified")!.Invoke(mixer, null)!, Is.True);
            typeof(OptionsTabControlRow).GetMethod("ResetButtonPressed", flags)!.Invoke(control, [null]);
            Assert.That(first.Slider.Value, Is.EqualTo(1f));

            first.Slider.Value = 0.23f;
            typeof(OptionsTabControlRow).GetMethod("ApplyButtonPressed", flags)!.Invoke(control, [null]);
            Assert.That(cfg.GetCVar(ArcaneCVars.TTSRadioChannelVolumes), Is.EqualTo($"{first.Frequency}=0.23"));
            Assert.That(second.Slider.Value, Is.EqualTo(1f));
            typeof(OptionsTabControlRow).GetMethod("DefaultButtonPressed", flags)!.Invoke(control, [null]);
            Assert.That(first.Slider.Value, Is.EqualTo(1f));
            Assert.That(cfg.GetCVar(ArcaneCVars.TTSRadioChannelVolumes), Is.Not.Empty, "Defaults are staged until applied.");
            typeof(OptionsTabControlRow).GetMethod("ApplyButtonPressed", flags)!.Invoke(control, [null]);
            Assert.That(cfg.GetCVar(ArcaneCVars.TTSRadioChannelVolumes), Is.Empty);
        });
    }
}
// Arcane-End
