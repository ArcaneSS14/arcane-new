using Content.Arcane.Common.CCVar;
using Content.Client.Outline;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.Outline;

/// <summary>
/// Resolves interaction outline colors from the player's archived customization settings.
/// </summary>
public sealed partial class InteractionOutlineColorSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private InteractionOutlineSystem _interaction = default!;
    [Dependency] private TargetOutlineSystem _target = default!;

    private Color _validColor = Color.FromHex(ArcaneCVars.ValidInteractionOutlineColor.DefaultValue);
    private Color _invalidColor = Color.FromHex(ArcaneCVars.InvalidInteractionOutlineColor.DefaultValue);
    private bool _useCustomColors;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configuration, ArcaneCVars.ValidInteractionOutlineColor, _ => Refresh());
        Subs.CVar(_configuration, ArcaneCVars.InvalidInteractionOutlineColor, _ => Refresh());
        Subs.CVar(_configuration, ArcaneCVars.UseCustomInteractionOutlineColors, value =>
        {
            _useCustomColors = value;
            Refresh();
        }, true);
    }

    public Color GetOutlineColor(bool inRange)
    {
        return inRange ? _validColor : _invalidColor;
    }

    private void Refresh()
    {
        _validColor = GetColor(ArcaneCVars.ValidInteractionOutlineColor);
        _invalidColor = GetColor(ArcaneCVars.InvalidInteractionOutlineColor);
        _interaction.SetOutlineColors(_validColor, _invalidColor);
        _target.SetOutlineColors(_validColor, _invalidColor);
    }

    private Color GetColor(CVarDef<string> cvar)
    {
        var defaultColor = Color.FromHex(cvar.DefaultValue);
        if (!_useCustomColors)
            return defaultColor;

        return Color.TryFromHex(_configuration.GetCVar(cvar), out var color)
            ? color
            : defaultColor;
    }
}
