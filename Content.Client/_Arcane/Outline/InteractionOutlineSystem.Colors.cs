using Content.Arcane.Common.CCVar;

namespace Content.Client.Outline;

public sealed partial class InteractionOutlineSystem
{
    private Color _validOutlineColor = Color.FromHex(ArcaneCVars.ValidInteractionOutlineColor.DefaultValue);
    private Color _invalidOutlineColor = Color.FromHex(ArcaneCVars.InvalidInteractionOutlineColor.DefaultValue);

    /// <summary>Updates outline colors and refreshes existing shader instances.</summary>
    public void SetOutlineColors(Color valid, Color invalid)
    {
        _validOutlineColor = valid;
        _invalidOutlineColor = invalid;
        RefreshOutlineColors();
    }

    private void RefreshOutlineColors()
    {
        foreach (var ((inRange, _), shader) in _shaderCache)
        {
            shader.SetParameter("outline_color", inRange ? _validOutlineColor : _invalidOutlineColor);
        }
    }
}
