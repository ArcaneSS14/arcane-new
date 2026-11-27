using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    public static readonly CVarDef<float> AhelpHistoryRateLimitPeriod =
        CVarDef.Create("ahelp.history_rate_limit_period", 5f, CVar.SERVERONLY);

    public static readonly CVarDef<int> AhelpHistoryRateLimitCount =
        CVarDef.Create("ahelp.history_rate_limit_count", 2, CVar.SERVERONLY);
}
