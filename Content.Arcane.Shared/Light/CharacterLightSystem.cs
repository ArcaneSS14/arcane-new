using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Toggleable;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Light;

/// <summary>
/// Uses the current visual torso and inventory to colour and shield character lights.
/// Profile and organ changes are coalesced until the complete body update has finished.
/// </summary>
public sealed partial class CharacterLightSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private INetManager _network = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    private const SlotFlags WornSlots = SlotFlags.All & ~(SlotFlags.POCKET | SlotFlags.SUITSTORAGE | SlotFlags.PREVENTEQUIP);
    private readonly HashSet<EntityUid> _dirtyBodies = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyGlowComponent, ComponentStartup>(OnBodyStartup);
        SubscribeLocalEvent<BodyGlowComponent, MapInitEvent>(OnBodyMapInit);
        SubscribeLocalEvent<BodyGlowComponent, ComponentShutdown>(OnBodyShutdown);
        SubscribeLocalEvent<VisualBodyComponent, ApplyOrganProfileDataEvent>(OnProfile);
        SubscribeLocalEvent<VisualBodyComponent, OrganInsertedIntoEvent>(OnOrganInserted);
        SubscribeLocalEvent<VisualBodyComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        SubscribeLocalEvent<InventoryComponent, DidEquipEvent>(OnEquipped);
        SubscribeLocalEvent<InventoryComponent, DidUnequipEvent>(OnUnequipped);

        SubscribeLocalEvent<WearerLightColorComponent, MapInitEvent>(OnLightMapInit);
        SubscribeLocalEvent<WearerLightColorComponent, ClothingGotEquippedEvent>(OnLightEquipped);
        SubscribeLocalEvent<WearerLightColorComponent, ClothingGotUnequippedEvent>(OnLightUnequipped);
        SubscribeLocalEvent<WearerLightColorComponent, AttemptPointLightToggleEvent>(OnLightAttempt);
        SubscribeLocalEvent<WearerLightColorComponent, AfterAutoHandleStateEvent>(OnLightState);
    }

    private void OnBodyStartup(Entity<BodyGlowComponent> ent, ref ComponentStartup args) => _dirtyBodies.Add(ent);
    private void OnBodyMapInit(Entity<BodyGlowComponent> ent, ref MapInitEvent args) => _dirtyBodies.Add(ent);
    private void OnProfile(Entity<VisualBodyComponent> ent, ref ApplyOrganProfileDataEvent args) => _dirtyBodies.Add(ent);
    private void OnOrganInserted(Entity<VisualBodyComponent> ent, ref OrganInsertedIntoEvent args) => _dirtyBodies.Add(ent);
    private void OnOrganRemoved(Entity<VisualBodyComponent> ent, ref OrganRemovedFromEvent args) => _dirtyBodies.Add(ent);
    private void OnEquipped(Entity<InventoryComponent> ent, ref DidEquipEvent args) => _dirtyBodies.Add(ent);
    private void OnUnequipped(Entity<InventoryComponent> ent, ref DidUnequipEvent args) => _dirtyBodies.Add(ent);

    private void OnBodyShutdown(Entity<BodyGlowComponent> ent, ref ComponentShutdown args)
    {
        _dirtyBodies.Remove(ent);
        if (_network.IsServer && _lights.TryGetLight(ent, out var light))
            _lights.SetEnabled(ent, false, light);
    }

    private void OnLightMapInit(Entity<WearerLightColorComponent> ent, ref MapInitEvent args) => CaptureDefaultColor(ent);

    private void CaptureDefaultColor(Entity<WearerLightColorComponent> ent)
    {
        if (ent.Comp.DefaultColor == null && _lights.TryGetLight(ent, out var light))
        {
            ent.Comp.DefaultColor = light.Color;
            Dirty(ent);
        }
    }

    private void OnLightEquipped(Entity<WearerLightColorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateWearerLight(ent, args.Wearer);
        _dirtyBodies.Add(args.Wearer);
    }

    private void OnLightUnequipped(Entity<WearerLightColorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateWearerLight(ent, null);
        _dirtyBodies.Add(args.Wearer);
    }

    private void OnLightAttempt(Entity<WearerLightColorComponent> ent, ref AttemptPointLightToggleEvent args)
    {
        if (args.Enabled && ent.Comp.LightSuppressed)
            args.Cancelled = true;
    }

    private void OnLightState(Entity<WearerLightColorComponent> ent, ref AfterAutoHandleStateEvent args) => SyncLight(ent);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var body in _dirtyBodies)
        {
            if (TerminatingOrDeleted(body))
                continue;

            if (TryComp<BodyGlowComponent>(body, out var glow))
                UpdateBodyGlow((body, glow));

            if (!TryComp<InventoryComponent>(body, out var inventory))
                continue;

            var slots = _inventory.GetSlotEnumerator((body, inventory), WornSlots);
            while (slots.NextItem(out var item))
            {
                if (IsWornClothing(item) && TryComp<WearerLightColorComponent>(item, out var color))
                    UpdateWearerLight((item, color), body);
            }
        }
        _dirtyBodies.Clear();
    }

    private Color GetBodyColor(EntityUid body)
    {
        var torso = _body.GetOrgan(body, Torso);
        return TryComp<VisualOrganComponent>(torso, out var visual) ? visual.Profile.SkinColor : Color.White;
    }

    private void UpdateBodyGlow(Entity<BodyGlowComponent> ent)
    {
        if (!_network.IsServer || !_lights.TryGetLight(ent, out var light))
            return;

        var reduction = 0f;
        if (TryComp<InventoryComponent>(ent, out var inventory))
        {
            var slots = _inventory.GetSlotEnumerator((ent.Owner, inventory), WornSlots);
            while (slots.NextItem(out var item))
            {
                if (IsWornClothing(item) && TryComp<GlowShieldingComponent>(item, out var shielding))
                    reduction += shielding.Reduction;
            }
        }

        var factor = Math.Clamp(1f - reduction, 0f, 1f);
        ent.Comp.Factor = factor;
        _lights.SetColor(ent, GetBodyColor(ent), light);
        if (factor <= 0f)
        {
            _lights.SetEnabled(ent, false, light);
            return;
        }
        _lights.SetRadius(ent, ent.Comp.BaseRadius * factor, light);
        _lights.SetEnergy(ent, ent.Comp.BaseEnergy * factor, light);
        _lights.SetEnabled(ent, factor > 0f, light);
    }

    private bool IsWornClothing(EntityUid item)
    {
        return TryComp<ClothingComponent>(item, out var clothing)
            && clothing.InSlotFlag is { } slot
            && (clothing.Slots & slot & WornSlots) != SlotFlags.NONE;
    }

    private void UpdateWearerLight(Entity<WearerLightColorComponent> ent, EntityUid? wearer)
    {
        CaptureDefaultColor(ent);
        var color = ent.Comp.DefaultColor ?? Color.White;
        var suppressed = false;
        if (wearer is { } body)
        {
            var eligible = TryComp<HumanoidProfileComponent>(body, out var profile)
                && (ent.Comp.Species == null || profile.Species == ent.Comp.Species);
            suppressed = !eligible && ent.Comp.Species != null;
            if (eligible)
                color = GetBodyColor(body);
        }

        if (ent.Comp.LightSuppressed != suppressed)
        {
            ent.Comp.LightSuppressed = suppressed;
            Dirty(ent);
        }
        _appearance.SetData(ent, ToggleableVisuals.Color, color);
        if (!_lights.TryGetLight(ent, out var light))
            return;

        _lights.SetColor(ent, color, light);
        SyncLight(ent);
    }

    private void SyncLight(Entity<WearerLightColorComponent> ent)
    {
        if (_appearance.TryGetData<bool>(ent, ToggleableVisuals.Enabled, out var enabled) && enabled
            && _lights.TryGetLight(ent, out var light))
        {
            var suppressed = ent.Comp.LightSuppressed;
            _lights.SetEnabled(ent, !suppressed, light);
        }
    }
}
