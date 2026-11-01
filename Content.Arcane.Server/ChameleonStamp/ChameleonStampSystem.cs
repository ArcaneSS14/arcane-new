using Content.Arcane.Shared.ChameleonStamp;
using Content.Shared.Paper;

namespace Content.Arcane.Server.ChameleonStamp;

public sealed partial class ChameleonStampSystem : SharedChameleonStampSystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    private const int MaxNameLength = 64;
    private const int MaxDescriptionLength = 256;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStampComponent, ChameleonStampApplySettingsMessage>(OnApplySettings);

        UpdatePresets();
    }

    private void OnApplySettings(Entity<ChameleonStampComponent> entity, ref ChameleonStampApplySettingsMessage args)
    {
        if (!TryComp<StampComponent>(entity, out var stamp))
            return;

        _ = GetAllPresets();

        if (!ValidatePreset(args.SelectedStampColorPrototype, out _, out var stampColorComponent))
            return;

        if (!ValidatePreset(args.SelectedStampStatePrototype, out _, out var stampStateComponent))
            return;

        if (!ValidatePreset(args.SelectedStampSpritePrototype, out var stampSpritePrototype, out var stampSpriteComponent))
            return;

        // Color
        entity.Comp.SelectedStampColorPrototype = args.SelectedStampColorPrototype;
        entity.Comp.CustomStampColor = args.CustomStampColor;
        stamp.StampedColor = stampColorComponent.StampedColor == args.CustomStampColor ? stampColorComponent.StampedColor : args.CustomStampColor;

        // StampedName
        stamp.StampedName = args.StampedName is { } stampedName
            ? Truncate(stampedName, MaxNameLength)
            : Loc.GetString(stampSpriteComponent.StampedName);

        // Metadata
        if (args.CustomName == null)
            ApplyName(entity, stampSpritePrototype.Name);
        else
            ApplyName(entity, Truncate(args.CustomName, MaxNameLength), true);

        if (args.CustomDescription == null)
            ApplyDescription(entity, stampSpritePrototype.Description);
        else
            ApplyDescription(entity, Truncate(args.CustomDescription, MaxDescriptionLength), true);

        // StampState
        entity.Comp.SelectedStampStatePrototype = args.SelectedStampStatePrototype;
        stamp.StampState = stampStateComponent.StampState;

        // StampSprite
        entity.Comp.SelectedStampSpritePrototype = args.SelectedStampSpritePrototype;

        Dirty(entity);
        Dirty(entity, stamp);

        void ApplyDescription(Entity<ChameleonStampComponent> stampEntity, string value, bool applyToComponent = false)
        {
            _metaData.SetEntityDescription(stampEntity, value);
            stampEntity.Comp.CustomDescription = applyToComponent ? value : null;
        }

        void ApplyName(Entity<ChameleonStampComponent> stampEntity, string value, bool applyToComponent = false)
        {
            _metaData.SetEntityName(stampEntity, value);
            stampEntity.Comp.CustomName = applyToComponent ? value : null;
        }
    }

    private static string Truncate(string text, int length)
    {
        return text.Length > length ? text[..length] : text;
    }
}
