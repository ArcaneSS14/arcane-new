using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

[CVarDefs]
public sealed partial class ArcaneCCVars
{
    public static readonly CVarDef<bool> EnableLightsGlowing =
        CVarDef.Create("light.light.enable_lights_glowing", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> LightingShaderQuality =
        CVarDef.Create("light.shader_highlights.quality", 2, CVar.CLIENTONLY | CVar.ARCHIVE);
}
