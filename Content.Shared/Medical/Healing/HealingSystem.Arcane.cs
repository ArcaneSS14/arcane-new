// Arcane-Start
using Content.Arcane.Common.Medical.Skills;

namespace Content.Shared.Medical.Healing;

public sealed partial class HealingSystem
{
    private TimeSpan GetHealingDelay(Entity<HealingComponent> healing, EntityUid target, EntityUid user)
    {
        // a multiplier of 0 is the healing toolbox, which has no self heal penalty
        var delay = user != target || healing.Comp.SelfHealPenaltyMultiplier == 0f
            ? healing.Comp.Delay
            : healing.Comp.Delay * GetScaledHealingPenalty(target, healing.Comp.SelfHealPenaltyMultiplier);

        return delay * GetSkillTimeMultiplier(user);
    }

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
