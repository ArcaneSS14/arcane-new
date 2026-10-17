using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Overlays;

public sealed partial class BlackAndWhiteOverlaySystem : EquipmentHudSystem<BlackAndWhiteOverlayComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private BlackAndWhiteOverlay _overlay = default!;
    private readonly Dictionary<EntitySystem, float> _intensities = new(); // Arcane

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new() { Intensity = 0f }; // Arcane-Edit: start at full intensity > enable on demand
    }

    // Arcane-Start
    public override void Shutdown()
    {
        base.Shutdown();
        _intensities.Clear();
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Dispose();
    }

    /// <summary>
    /// Sets one system's contribution; zero removes it. The strongest source controls the overlay.
    /// </summary>
    public void SetIntensity(EntitySystem source, float intensity)
    {
        intensity = Math.Clamp(intensity, 0f, 1f);
        if (intensity <= 0f)
        {
            if (!_intensities.Remove(source))
                return;
        }
        else
        {
            if (_intensities.TryGetValue(source, out var previous) && previous == intensity)
                return;

            _intensities[source] = intensity;
        }

        var combined = 0f;
        foreach (var value in _intensities.Values)
            combined = Math.Max(combined, value);

        if (_overlay.Intensity == combined)
            return;

        _overlay.Intensity = combined;
        if (combined > 0f)
            _overlayMan.AddOverlay(_overlay);
        else
            _overlayMan.RemoveOverlay(_overlay);
    }
    // Arcane-End

    protected override void UpdateInternal(RefreshEquipmentHudEvent<BlackAndWhiteOverlayComponent> component)
    {
        base.UpdateInternal(component);

        SetIntensity(this, 1f); // Arcane-Edit: equipment owns one contribution
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        SetIntensity(this, 0f); // Arcane-Edit: remove only equipment's contribution
    }
}
