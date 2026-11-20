using Content.Arcane.Shared.Vision;
using Content.Trauma.Common.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Arcane.Client.Vision;

public sealed partial class UltraVisionSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IPlayerManager _player = default!;

    private UltraVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new UltraVisionOverlay();

        SubscribeLocalEvent<UltraVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<UltraVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<UltraVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<UltraVisionComponent, LocalPlayerDetachedEvent>(OnDetached);
        Subs.CVar(_configuration, TraumaCVars.NoVisionFilters, _ => Refresh(), true);
    }

    public override void Shutdown()
    {
        _overlays.RemoveOverlay(_overlay);
        base.Shutdown();
    }

    private void OnStartup(Entity<UltraVisionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner == _player.LocalEntity)
            Refresh();
    }

    private void OnShutdown(Entity<UltraVisionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            _overlays.RemoveOverlay(_overlay);
    }

    private void OnAttached(Entity<UltraVisionComponent> ent, ref LocalPlayerAttachedEvent args) => Refresh();

    private void OnDetached(Entity<UltraVisionComponent> ent, ref LocalPlayerDetachedEvent args) => _overlays.RemoveOverlay(_overlay);

    private void Refresh()
    {
        if (!_configuration.GetCVar(TraumaCVars.NoVisionFilters)
            && HasComp<UltraVisionComponent>(_player.LocalEntity))
        {
            if (!_overlays.HasOverlay<UltraVisionOverlay>())
                _overlays.AddOverlay(_overlay);
        }
        else
            _overlays.RemoveOverlay(_overlay);
    }
}
