using Content.Arcane.Shared.ModSuits;
using Content.Client.UserInterface.Controls;

namespace Content.Arcane.Client.ModSuits;

public sealed class ModStatusReadoutWindow : FancyWindow
{
    private readonly Label _readings = new();

    public ModStatusReadoutWindow()
    {
        Title = Loc.GetString("modsuit-status-title");
        MinSize = new Vector2(340, 210);
        ContentsContainer.AddChild(_readings);
    }

    public void UpdateReadout(ModStatusReadoutState state)
    {
        var unknown = Loc.GetString("modsuit-status-unknown");
        var text = Loc.GetString("modsuit-status-readings",
            ("name", state.Name),
            ("state", Loc.GetString($"modsuit-status-state-{state.State.ToString().ToLowerInvariant()}")),
            ("damage", state.Damage.ToString("N1")),
            ("temperature", state.Temperature is { } temperature ? (temperature - 273.15f).ToString("N1") : unknown),
            ("blood", state.Blood is { } blood ? (blood * 100).ToString("N0") : unknown),
            ("stamina", state.StaminaDamage?.ToString("N1") ?? unknown));
        if (state.Fingerprint is { } fingerprint)
            text += "\n" + Loc.GetString("modsuit-status-fingerprint", ("fingerprint", fingerprint));
        _readings.Text = text;
    }
}
