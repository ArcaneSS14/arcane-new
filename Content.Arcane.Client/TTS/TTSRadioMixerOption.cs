using Content.Arcane.Common.CCVar;
using Content.Client.Options.UI;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.TTS;

/// <summary>Stages the per-channel settings together with the other options on the tab.</summary>
internal sealed class TTSRadioMixerOption(OptionsTabControlRow controller, IConfigurationManager cfg) : BaseOption(controller)
{
    private readonly List<(int Frequency, CheckBox CheckBox, Slider Slider, Label Label, Func<float, string> Format)> _rows = new();
    private HashSet<int> _muted = new();
    private Dictionary<int, float> _volumes = new();
    private bool _loading;

    public void AddRow(int frequency, CheckBox checkBox, Slider slider, Label label, Func<float, string> format)
    {
        _rows.Add((frequency, checkBox, slider, label, format));
        checkBox.OnToggled += _ =>
        {
            if (_loading)
                return;
            if (checkBox.Pressed)
                _muted.Remove(frequency);
            else
                _muted.Add(frequency);
            UpdateRows();
            ValueChanged();
        };
        slider.OnValueChanged += _ =>
        {
            label.Text = format(slider.Value);
            if (_loading)
                return;
            if (slider.Value == 1f)
                _volumes.Remove(frequency);
            else
                _volumes[frequency] = slider.Value;
            UpdateRows();
            ValueChanged();
        };
    }

    public override void LoadValue() => SetValues(cfg.GetCVar(ArcaneCVars.TTSRadioChannelMuted), cfg.GetCVar(ArcaneCVars.TTSRadioChannelVolumes));

    public override void ResetToDefault() => SetValues(ArcaneCVars.TTSRadioChannelMuted.DefaultValue, ArcaneCVars.TTSRadioChannelVolumes.DefaultValue);

    public override void SaveValue()
    {
        cfg.SetCVar(ArcaneCVars.TTSRadioChannelMuted, TTSRadioChannelSettings.FormatMuted(_muted));
        cfg.SetCVar(ArcaneCVars.TTSRadioChannelVolumes, TTSRadioChannelSettings.FormatVolumes(_volumes));
    }

    public override bool IsModified() => DiffersFrom(cfg.GetCVar(ArcaneCVars.TTSRadioChannelMuted), cfg.GetCVar(ArcaneCVars.TTSRadioChannelVolumes));

    public override bool IsModifiedFromDefault() => DiffersFrom(ArcaneCVars.TTSRadioChannelMuted.DefaultValue, ArcaneCVars.TTSRadioChannelVolumes.DefaultValue);

    private bool DiffersFrom(string muted, string volumes)
    {
        return TTSRadioChannelSettings.FormatMuted(_muted) != TTSRadioChannelSettings.FormatMuted(TTSRadioChannelSettings.ParseMuted(muted))
               || TTSRadioChannelSettings.FormatVolumes(_volumes) != TTSRadioChannelSettings.FormatVolumes(TTSRadioChannelSettings.ParseVolumes(volumes));
    }

    private void SetValues(string muted, string volumes)
    {
        _muted = TTSRadioChannelSettings.ParseMuted(muted);
        _volumes = TTSRadioChannelSettings.ParseVolumes(volumes);
        UpdateRows();
    }

    private void UpdateRows()
    {
        _loading = true;
        try
        {
            foreach (var row in _rows)
            {
                row.CheckBox.Pressed = !_muted.Contains(row.Frequency);
                row.Slider.Value = _volumes.GetValueOrDefault(row.Frequency, 1f);
                row.Label.Text = row.Format(row.Slider.Value);
            }
        }
        finally
        {
            _loading = false;
        }
    }
}
