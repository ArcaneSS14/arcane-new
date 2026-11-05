using Content.Trauma.Common.CCVar;
using Robust.Shared.Configuration;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class GraphicsTab
{
    private sealed class OptionParticleQuality : BaseOption
    {
        private readonly IConfigurationManager _cfg;
        private readonly OptionDropDown _dropDown;

        private const int QualityOff    = 0;
        private const int QualityLow    = 1;
        private const int QualityMedium = 2;
        private const int QualityHigh   = 3;
        private const int QualityDefault = QualityHigh;

        public OptionParticleQuality(OptionsTabControlRow controller, IConfigurationManager cfg, OptionDropDown dropDown) : base(controller)
        {
            _cfg = cfg;
            _dropDown = dropDown;
            var button = dropDown.Button;
            // Arcane-Edit-Start: populate labels through the shared refresh path.
            button.AddItem(string.Empty,    QualityOff);
            button.AddItem(string.Empty,    QualityLow);
            button.AddItem(string.Empty, QualityMedium);
            button.AddItem(string.Empty,   QualityHigh);
            // Arcane-Edit-End
            ReloadLocalization(); // Arcane
            button.OnItemSelected += args =>
            {
                _dropDown.Button.SelectId(args.Id);
                ValueChanged();
            };
        }

        // Arcane-Start
        public override void ReloadLocalization()
        {
            _dropDown.Button.SetItemText(_dropDown.Button.GetIdx(QualityOff), Loc.GetString("ui-options-particles-off"));
            _dropDown.Button.SetItemText(_dropDown.Button.GetIdx(QualityLow), Loc.GetString("ui-options-particles-low"));
            _dropDown.Button.SetItemText(_dropDown.Button.GetIdx(QualityMedium), Loc.GetString("ui-options-particles-medium"));
            _dropDown.Button.SetItemText(_dropDown.Button.GetIdx(QualityHigh), Loc.GetString("ui-options-particles-high"));
        }
        // Arcane-End

        public override void LoadValue()
        {
            var value = _cfg.GetCVar(TraumaCVars.ParticleQuality);
            value = Math.Clamp(value, QualityOff, QualityHigh);
            _dropDown.Button.SelectId(value);
        }

        public override void SaveValue()
        {
            _cfg.SetCVar(TraumaCVars.ParticleQuality, _dropDown.Button.SelectedId);
        }

        public override void ResetToDefault()
        {
            _dropDown.Button.SelectId(QualityDefault);
        }

        public override bool IsModified()
        {
            return _dropDown.Button.SelectedId != _cfg.GetCVar(TraumaCVars.ParticleQuality);
        }

        public override bool IsModifiedFromDefault()
        {
            return _dropDown.Button.SelectedId != QualityDefault;
        }
    }
}
