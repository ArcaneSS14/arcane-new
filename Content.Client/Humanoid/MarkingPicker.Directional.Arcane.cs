// Arcane-Start
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
// Arcane-End

namespace Content.Client.Humanoid; // Arcane

// Arcane-Start
public sealed partial class MarkingPicker
{
    private bool _showEarsAboveHairOption;

    public event Action<bool>? EarsAboveHairChanged;

    public bool ShowEarsAboveHairOption
    {
        get => _showEarsAboveHairOption;
        set
        {
            _showEarsAboveHairOption = value;
            UpdateEarVisibility();
        }
    }

    public bool EarsAboveHair
    {
        get => EarsAboveHairToggle.Pressed;
        set => EarsAboveHairToggle.Pressed = value;
    }

    private void OnMarkingsChanged(ProtoId<OrganCategoryPrototype> organ, HumanoidVisualLayers layer)
        => UpdateEarVisibility();

    private void UpdateEarVisibility()
    {
        EarsAboveHairToggle.Visible = _showEarsAboveHairOption && _markingsModel != null &&
            _markingsModel.Markings.Values.SelectMany(organ => organ.Values).SelectMany(markings => markings)
                .Any(marking => _marking.TryGetMarking(marking, out var prototype) && prototype.UnderHair);
    }
}
// Arcane-End
