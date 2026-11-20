using Content.Arcane.Common.Input;
﻿using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.OfferItem;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.OfferItem;

public sealed partial class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        _inputManager.Contexts.GetContext("human").AddFunction(ArcaneKeyFunctions.OfferItem);
        Subs.CVar(_cfg, ArcaneCVars.OfferModeIndicatorsPointShow, OnShowOfferIndicatorsChanged, true);
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<OfferItemIndicatorsOverlay>();
        base.Shutdown();
    }

    public bool IsInOfferMode()
    {
        var entity = _playerManager.LocalEntity;

        return entity is not null && IsInOfferMode(entity.Value);
    }

    private void OnShowOfferIndicatorsChanged(bool isShow)
    {
        if (isShow)
            _overlayManager.AddOverlay(new OfferItemIndicatorsOverlay(_inputManager, EntityManager, _eye, this));
        else
            _overlayManager.RemoveOverlay<OfferItemIndicatorsOverlay>();
    }
}
