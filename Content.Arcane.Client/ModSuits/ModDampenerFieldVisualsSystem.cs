// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;

namespace Content.Arcane.Client.ModSuits;

public sealed partial class ModDampenerFieldVisualsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;

    private ModDampenerFieldOverlay _field = default!;

    public override void Initialize()
    {
        base.Initialize();
        _field = new ModDampenerFieldOverlay();
        _overlays.AddOverlay(_field);
    }

    public override void Shutdown()
    {
        _overlays.RemoveOverlay(_field);
        _field.Dispose();
        base.Shutdown();
    }
}
