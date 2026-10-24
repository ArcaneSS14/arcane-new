# Deferred hunks / notes — pack0 (port/orion-content-pack0)

## PR #16 InteQ (batch later)
- Resources/Prototypes/_Orion/Entities/Clothing/Belt/belts.yml (PR31 skip)
- Resources/Prototypes/_Orion/Entities/Clothing/Uniforms/…_inteq.yml (PR57 skip)
- Resources/Prototypes/_Orion/Catalog/Fills/Backpacks/backpack.yml (PR57 skip)
- Resources/Prototypes/_Orion/GameRules/events.yml (PR17 delta only)
- Resources/Maps/_Orion/InteQ_Sizo.yml (PR57 skip)

## PR #20 — Lavaland Hermit ghost role (PORTED; earlier CUT note below overridden by user "#20 портируй")
- Investigated: our /Maps/_Lavaland/Lavaland/ruin_hermit.yml has NO ghost-role spawner (no SpawnPointHermitLavaland),
  so the original CUT reason ("hermit уже в пуле руин") did not hold; the old map is not the "real hermit ghost role".
- New map: Resources/Maps/_Arcane/Lavaland/ruin_hermit.yml (upstream _Europa path transformed); lavaland_grid_ruins.yml
  Hermit path repointed with Arcane-Edit marker. Old _Lavaland map file kept (now unreferenced by prototypes).
- Resources/Prototypes/_Lavaland/Procedural/lavaland_ruin_pools.yml already has Hermit: 2 (upstream target value,
  upstream file name is ruin_pools.yml) — no change needed for that hunk.
- CloningAppearance feature (component/event/system) imported from upstream Content.Server/_Europa/CloningAppearance
  into Content.Arcane.Server/CloningAppearance (flat namespace Content.Arcane.Server.CloningAppearance); the hermit
  ghost-role takeover flow requires it (marker entity spawns the player's own mob with gear/components).
- HermitInteQ selectable set DEFERRED: refs Clothing*InteQ* prototypes + _Arcane/Icons/inteq.rsi which arrive with the
  InteQ PR group. Locale keys selectable-set-hermit-inteq-* ARE imported. Re-add set block in
  Resources/Prototypes/_Arcane/Catalog/selectable_sets.yml and "- HermitInteQ" in
  Resources/Prototypes/_Arcane/Entities/Objects/Devices/set_selector.yml possibleSets.
- WallAsteroidCobblebrick (used by the new map) is not defined in this fork — pre-existing condition: maps
  Resources/Maps/Lavaland/hermit_base.yml, shinobi_graveyard.yml, _Trauma/nume.yml also reference it;
  Resources/migration.yml maps WallAsteroidCobblebrick -> WallConcrete.
- en-US did not receive role-type-ghost-role-name upstream (key exists only in ru); added in
  Resources/Locale/en-US/_Arcane/mind/role-types.ftl as "Ghost Role" (en source of truth).

## PR #69 (outside pack0 list) — uplink-energy-dome keys
- uplink-energy-dome-name/desc added manually to Resources/Locale/en-US/_Arcane/store/uplink-catalog.ftl
  (uplink catalog item introduced by #31; keys originally added by #69 which is not in pack0)
- ru-RU not created (en-only source of truth; user has not requested Russian localization).

## PR #17 — wisdom-say en gap (upstream deficit)
- 227 wisdom-say strings exist upstream only in ru-RU (_Orion/wisdom.ftl); en-US upstream does NOT
  define them. We did NOT invent en text. ent-* en names live inline in yml (name/description fields),
  so no ftl is needed for those. Flagged here as a known upstream localization gap.
- ru-RU wisdom.ftl imported faithfully into ru-RU/_Arcane/wisdom.ftl.

## PR #429 — reptilian_hiss.ogg mono fix (N/A)
- Modifies Resources/Audio/_Orion/Voice/Reptilian/reptilian_hiss.ogg (stereo->mono).
- Base file does NOT exist in our fork (never imported; not in pack0 scope).
- Skipped as not-applicable; revisit if/when the introducing audio PR is ported.

## PR #38 (crayon decals) — unicode filenames
- Cyrillic RSI state names kept as-is (faithful to upstream); YAMLLinter/build pass.

## PR #354 — DrinkGlassBase parent
- Upstream parent id "DrinkGlassBase" does not exist in our tree; mapped to our "DrinkGlass"
  (metamorphic glass, the common plain-glass parent used by drink prototypes in this fork).

## PR #283 — RecruitmentScanningSystem humanoid check
- Upstream used HumanoidAppearanceComponent; this fork has no such component (renamed/moved).
- Mapped to HumanoidProfileComponent (the fork's humanoid marker used elsewhere, e.g. cloning checks).

## pack0 linter debt cleanup (post-PR20)
- PR #283 recruitment pad: `RecruitmentScanning.implant: DeathRattleImplantInteQ` removed —
  the implant prototype is InteQ-group content (PR #16 family). Re-add the field together with
  the InteQ PR that defines DeathRattleImplantInteQ.
- PR #354 kvass: only en-US `reagent-name-kvass` added (fork rule: en source of truth).
  ru-RU name intentionally not added until Russian localization is requested.
- PR #359 lobby screen: fields adapted to fork schema `title`/`artist` (LocId) with new
  en-US `_Arcane/lobby/lobby-state-background.ftl`.
- PR #40 bluespace reaction: `CreateEntityReactionEffect` -> fork's `!type:SpawnEntity`
  (same `entity`/`number` fields).
- PR #31/#40/#162 damageContainer moved from Damageable to `Injurable` (Trauma split);
  `PowerCellDraw.useRate` -> `useCharge`; `BatterySelfRecharger.autoRecharge` dropped (no such
  field; rate only).
- PR #370: six borg subtype RSIs were missing from Textures (import gap); restored from Orion
  ref into `_Arcane/Mobs/Silicon/...` (CC-BY-SA-4.0, @boiWab).
