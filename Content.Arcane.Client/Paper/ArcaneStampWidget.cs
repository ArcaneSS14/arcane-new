using System.Numerics;
using Content.Client.Paper.UI;
using Content.Shared.Paper;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Arcane.Client.Paper;

public sealed class ArcaneStampWidget : StampWidget
{
    private readonly StyleBox? _textBorder;

    public ArcaneStampWidget()
    {
        _textBorder = PanelOverride;
    }

    public override StampDisplayInfo StampInfo
    {
        set
        {
            base.StampInfo = value;
            Label.Visible = true;
            PanelOverride = _textBorder;
            MinSize = Vector2.Zero;
            SetSize = new Vector2(float.NaN);
            if (value.StampLargeIcon is not { } path ||
                !IoCManager.Resolve<IResourceCache>().TryGetResource<TextureResource>("/Textures/" + path, out var texture))
                return;

            // These source PNGs contain their own ink colors.
            ModulateSelfOverride = Color.White;
            Label.Visible = false;
            PanelOverride = new StyleBoxTexture { Texture = texture.Texture };
            MinSize = texture.Texture.Size * 1.5f;
            SetSize = MinSize;
        }
    }
}
