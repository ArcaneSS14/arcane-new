using Content.Server.Lathe;
using Content.Server.Power.Components;
using Content.Server.Research.Systems;
using Content.Shared.AlertLevel;
using Content.Shared.Lathe;
using Content.Shared.Localizations;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Station.Systems;
using System.Linq;

namespace Content.Arcane.Server.Research;

public sealed partial class TechFabRecipeRadioSystem : EntitySystem
{
    [Dependency] private AlertLevelSystem _alert = default!;
    [Dependency] private LatheSystem _lathe = default!;
    [Dependency] private SharedRadioSystem _radio = default!;
    [Dependency] private StationSystem _station = default!;

    private const int RecipeNameLimit = 5;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TechFabRecipeRadioComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified, after: [typeof(ResearchSystem)]);
    }

    private void OnDatabaseModified(Entity<TechFabRecipeRadioComponent> ent, ref TechnologyDatabaseModifiedEvent args)
    {
        if (args.NewlyUnlockedRecipes is not { Count: > 0 } unlocked
            || !TryComp<LatheComponent>(ent, out var lathe)
            || !TryComp<ApcPowerReceiverComponent>(ent, out var power)
            || !power.Powered
            || !Transform(ent).Anchored
            || _station.GetOwningStation(ent.Owner) is not { } station)
            return;

        if (TryComp<ResearchClientComponent>(ent, out var client) && client.Server is { } server
            && _station.GetOwningStation(server) != station)
            return;

        var staticRecipes = new HashSet<ProtoId<LatheRecipePrototype>>();
        _lathe.AddRecipesFromPacks(staticRecipes, lathe.StaticPacks);
        var newlyUnlocked = unlocked.ToHashSet(StringComparer.Ordinal);
        _alert.TryGetLevel(station, out var currentAlert);

        var names = new List<string>(RecipeNameLimit);
        var total = 0;
        foreach (var id in _lathe.GetAvailableRecipes(ent, lathe))
        {
            if (!newlyUnlocked.Contains(id.Id) || staticRecipes.Contains(id) || !ProtoMan.TryIndex(id, out var recipe))
                continue;

            if (recipe.RequiredAlerts is { } required && (currentAlert is not { } alert || !required.Contains(alert)))
                continue;

            total++;
            if (names.Count < RecipeNameLimit)
                names.Add(_lathe.GetRecipeName(recipe));
        }

        if (total == 0)
            return;

        var message = Loc.GetString("techfab-radio-new-recipes",
            ("recipes", ContentLocalizationManager.FormatList(names)), ("remaining", total - names.Count));
        _radio.SendRadioMessage(ent, message, ent.Comp.Channel, ent);
    }
}
