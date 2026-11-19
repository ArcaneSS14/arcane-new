using Content.Arcane.Server.Silicons.Synths.Battery;
using Content.Server.Zombies;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.Zombies;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Arcane.Server.Zombies;

public sealed partial class SiliconZombieSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ZombieSystem _zombies = default!;

    private static readonly ProtoId<DamageTypePrototype> Ion = "Ion";
    private static readonly ProtoId<DamageTypePrototype> Poison = "Poison";
    private static readonly EntProtoId Sparks = "EffectSparks";
    private static readonly SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks");

    [SubscribeLocalEvent]
    private void OnPendingStartup(Entity<PendingZombieComponent> ent, ref ComponentStartup args)
    {
        if (!HasComp<SiliconZombieComponent>(ent) ||
            CompOrNull<InjurableComponent>(ent)?.DamageContainer is not { } containerId)
            return;

        var container = ProtoMan.Index(containerId);
        if (!Supports(container, Poison) && Supports(container, Ion))
            ent.Comp.Damage = new DamageSpecifier(ProtoMan.Index(Ion), ent.Comp.Damage.GetTotal());
    }

    private bool Supports(DamageContainerPrototype container, ProtoId<DamageTypePrototype> type)
    {
        if (container.SupportedTypes.Contains(type))
            return true;

        foreach (var group in container.SupportedGroups)
        {
            if (ProtoMan.Index(group).DamageTypes.Contains(type))
                return true;
        }
        return false;
    }

    [SubscribeLocalEvent(after: [typeof(SynthBatteryPowerSystem)])]
    private void OnEmp(Entity<SiliconZombieComponent> ent, ref EmpPulseEvent args)
    {
        var wasZombie = HasComp<ZombieComponent>(ent);
        if (!_zombies.TryPurgeSiliconZombie(ent))
            return;

        args.Affected = true;
        Spark(ent);
        if (!wasZombie)
            _popup.PopupEntity(Loc.GetString("silicon-zombie-purged"), ent, ent, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnSymptom(Entity<SiliconZombieComponent> ent, ref PendingZombieSymptomEvent args)
    {
        Spark(ent);
        args.Handled = true;
    }

    private void Spark(EntityUid uid)
    {
        SpawnAtPosition(Sparks, Transform(uid).Coordinates);
        _audio.PlayPvs(SparkSound, uid);
    }
}
