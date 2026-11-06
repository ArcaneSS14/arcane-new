// Arcane-Start
using Content.Arcane.Common.Medical.Skills;

namespace Content.Shared.Medical.Healing;

public sealed partial class HealingSystem
{
    private float GetSkillTimeMultiplier(EntityUid user)
    {
        var ev = new GetHealingTimeMultiplierEvent(1f);
        RaiseLocalEvent(user, ref ev);
        return ev.Multiplier;
    }

    private void RaiseAppliedHealing(EntityUid user)
    {
        var ev = new UserAppliedHealingEvent();
        RaiseLocalEvent(user, ref ev);
    }
}
// Arcane-End
