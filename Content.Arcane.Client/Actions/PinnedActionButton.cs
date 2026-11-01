using Content.Client.Actions;
using Content.Client.Actions.UI;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Arcane.Client.Actions;

/// <summary>
/// Adds pin and unavailable feedback while retaining the current action button's sprites and cooldown.
/// </summary>
public sealed class PinnedActionButton : ActionButton
{
    private readonly IEntityManager _entities;
    private readonly ActionBarUIController _customization;
    private readonly IPrototypeManager _prototypes;
    private readonly PanelContainer _pin;
    private readonly TextureRect _placeholder;
    private EntityUid? _slot;
    private bool _unavailable;

    public PinnedActionButton(IEntityManager entities, ActionBarUIController customization) : base(entities)
    {
        _entities = entities;
        _customization = customization;
        _prototypes = IoCManager.Resolve<IPrototypeManager>();
        _pin = new PanelContainer
        {
            MouseFilter = MouseFilterMode.Ignore,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.Transparent,
                BorderColor = Color.FromHex("#8fd3ff"),
                BorderThickness = new Thickness(2)
            },
            Visible = false
        };
        _placeholder = new TextureRect
        {
            MouseFilter = MouseFilterMode.Ignore,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            Visible = false
        };
        AddChild(_placeholder);
        AddChild(_pin);
        OnKeyBindDown += OnPinPressed;
        var originalTooltip = TooltipSupplier;
        TooltipSupplier = control =>
        {
            var content = originalTooltip?.Invoke(control);
            if (_slot is not { } slot || !_customization.IsPinned(slot))
                return content;
            if (content is ActionAlertTooltip tooltip)
            {
                tooltip.AddFooter(PinStatus());
                return tooltip;
            }
            return PinTooltip(slot);
        };
    }

    private void OnPinPressed(GUIBoundKeyEventArgs args)
    {
        if (args.Function != ContentKeyFunctions.ToggleActionPin || _slot is not { } slot)
            return;
        _customization.TogglePin(slot, GetPositionInParent());
        args.Handle();
    }

    private Control? PinTooltip(EntityUid slot)
    {
        if (_customization.GetPrototype(slot) is not { } prototype
            || !_prototypes.TryIndex(prototype, out var entityPrototype))
            return null;

        return new ActionAlertTooltip(FormattedMessage.FromUnformatted(entityPrototype.Name),
            FormattedMessage.FromUnformatted(entityPrototype.Description), PinStatus());
    }

    private string PinStatus() => Loc.GetString(_unavailable ? "arcane-action-pinned-unavailable" : "arcane-action-pinned");

    public override void UpdateData(EntityUid? actionId, ActionsSystem system)
    {
        base.UpdateData(actionId, system);
        _slot = actionId;
        _unavailable = actionId is { } slot && _customization.IsUnavailable(slot);
        _pin.Visible = actionId is { } id && _customization.IsPinned(id);
        _placeholder.Visible = Action == null && _unavailable;
        _placeholder.Texture = null;
        if (_placeholder.Visible && actionId is { } placeholder
            && _customization.GetPrototype(placeholder) is { } prototype && _prototypes.HasIndex(prototype))
        {
            _placeholder.Texture = _entities.System<SpriteSystem>().GetPrototypeIcon(prototype).Default;
        }
        Modulate = _unavailable ? Color.Gray : Color.White;
        Label.Visible = actionId != null;
    }

    public override void ClearData()
    {
        base.ClearData();
        _slot = null;
        _unavailable = false;
        _pin.Visible = false;
        _placeholder.Visible = false;
        _placeholder.Texture = null;
        Modulate = Color.White;
    }
}
