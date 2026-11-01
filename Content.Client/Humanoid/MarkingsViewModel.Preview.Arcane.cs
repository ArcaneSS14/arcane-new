using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Client.Humanoid;

public sealed partial class MarkingsViewModel
{
    private bool _animatedPreview;
    private bool _previewReducedMotion;

    public event Action? PreviewChanged;

    public bool AnimatedPreview
    {
        get => _animatedPreview;
        set
        {
            if (_animatedPreview == value)
                return;

            _animatedPreview = value;
            PreviewChanged?.Invoke();
        }
    }

    public bool PreviewReducedMotion
    {
        get => _previewReducedMotion;
        set
        {
            if (_previewReducedMotion == value)
                return;

            _previewReducedMotion = value;
            PreviewChanged?.Invoke();
        }
    }

    public bool AnimatePreview => AnimatedPreview && !PreviewReducedMotion;

    /// <summary>
    /// Returns a separate display copy. Previewing never edits the selected markings or fires a save event.
    /// </summary>
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> GetPreviewMarkings()
    {
        return _markings.ToDictionary(
            organ => organ.Key,
            organ => organ.Value.ToDictionary(layer => layer.Key,
                layer => layer.Value.Select(PreviewMarking).ToList()));
    }

    private Marking PreviewMarking(Marking marking)
    {
        var id = marking.MarkingId.Id;
        if (AnimatePreview && _marking.TryGetWaggingMarking(id, out var animated))
            id = animated;
        else if (!AnimatePreview && _marking.TryGetStillMarking(id, out var still))
            id = still;

        return new Marking(id, marking.MarkingColors)
        {
            Forced = marking.Forced,
            IsChildMarking = marking.IsChildMarking,
        };
    }
}
