using Content.Arcane.Shared.Medical.Heart;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Arcane.Shared.Medical.Defibrillator;

public sealed partial class ManualDefibrillatorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ManualDefibrillatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ManualDefibrillatorComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<ManualDefibrillatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<ManualDefibrillatorComponent> ent, ref MapInitEvent args)
    {
        SetMode(ent, Math.Clamp(ent.Comp.Mode, 1, 4));
    }

    private void OnGetVerbs(Entity<ManualDefibrillatorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var category = new VerbCategory("arcane-defibrillator-mode-category", null);
        for (var mode = 1; mode <= 4; mode++)
        {
            if (!ent.Comp.BpmZapMode.TryGetValue(mode, out var change) ||
                !ent.Comp.BpmZapFlatlineMode.TryGetValue(mode, out var restart))
                continue;

            var restartText = restart > 0
                ? Loc.GetString("arcane-defibrillator-mode-restart", ("bpm", restart))
                : Loc.GetString("arcane-defibrillator-mode-no-restart");
            var selected = mode;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("arcane-defibrillator-mode", ("mode", mode),
                    ("sign", change > 0 ? "+" : ""), ("change", change), ("restart", restartText)),
                Category = category,
                Priority = 1,
                Disabled = mode == ent.Comp.Mode,
                Act = () => SetMode(ent, selected),
            });
        }
    }

    private void OnExamined(Entity<ManualDefibrillatorComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("arcane-defibrillator-current-mode", ("mode", ent.Comp.Mode)));
    }

    public bool SetMode(Entity<ManualDefibrillatorComponent> ent, int mode)
    {
        if (mode is < 1 or > 4 || TerminatingOrDeleted(ent)
            || !TryComp<HeartDefibrillatorComponent>(ent, out var defibrillator)
            || !ent.Comp.BpmZapMode.TryGetValue(mode, out var change)
            || !ent.Comp.BpmZapFlatlineMode.TryGetValue(mode, out var restart))
            return false;

        ent.Comp.Mode = mode;
        defibrillator.BpmZapHeal = change;
        defibrillator.BpmZapHealFlatline = restart;
        Dirty(ent);
        return true;
    }
}
