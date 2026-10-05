
Reference tables for the systems and namespaces that appear most often in this repository. Use them to pick an existing owner and a name that matches its neighbors. They are not a substitute for verifying a declaration before calling it.

Counts are declaration-site occurrences across `Content.Server`, `Content.Shared`, and `Content.Client`, so a system used by many files appears many times.

## The injection shape

```csharp
[Dependency] private IGameTiming _timing = default!;
```

Fields are `private` and **not** `readonly` in current code. `[Dependency] private readonly` appears zero times in the content projects; do not introduce it. `readonly` is acceptable only in a file where every neighbouring dependency already uses it.

Never resolve inside a method body:

```csharp
// Forbidden
var random = IoCManager.Resolve<IRobustRandom>();
```

## Frequently injected dependencies

### Engine-owned

| Namespace | Type | Field | Count |
|---|---|---|---|
| `Robust.Shared.Timing` | `IGameTiming` | `_timing` | 709 |
| `Robust.Shared.Audio.Systems` | `SharedAudioSystem` | `_audio` | 446 |
| `Robust.Shared.Random` | `IRobustRandom` | `_random` | 367 |
| `Robust.Shared.GameObjects` | `SharedTransformSystem` | `_transform` | 320 |
| `Robust.Shared.GameObjects` | `SharedAppearanceSystem` | `_appearance` | 234 |
| `Robust.Shared.Network` | `INetManager` | `_net` | 232 |
| `Robust.Shared.GameObjects` | `EntityLookupSystem` | `_lookup` | 156 |
| `Robust.Shared.Configuration` | `IConfigurationManager` | `_cfg` | 160 |
| `Robust.Shared.Containers` | `SharedContainerSystem` | `_container` | 160 |
| `Robust.Client.GameObjects` | `SpriteSystem` | `_sprite` | 150 |
| `Robust.Shared.GameObjects` | `IEntityManager` | `_entManager` | 139 |
| `Robust.Shared.Player` | `IPlayerManager` | `_player` | 138 |
| `Robust.Shared.Physics.Systems` | `SharedPhysicsSystem` | `_physics` | 108 |
| `Robust.Shared.Prototypes` | `IPrototypeManager` | `_proto` | 101 |
| `Robust.Shared.GameObjects` | `SharedMapSystem` | `_map` | 104 |
| `Robust.Shared.GameObjects` | `SharedUserInterfaceSystem` | `_ui` | 119 |

### Content-owned

| Namespace | Type | Field | Count |
|---|---|---|---|
| `Content.Shared.Popups` | `SharedPopupSystem` | `_popup` | 483 |
| `Content.Shared.Chat` | `SharedChatSystem` | `_chat` | 132 |
| `Content.Shared.Administration.Logs` | `ISharedAdminLogManager` | `_adminLogger` | 101 |
| `Content.Shared.Actions` | `SharedActionsSystem` | `_actions` | 149 |
| `Content.Shared.Hands.EntitySystems` | `SharedHandsSystem` | `_hands` | 148 |
| `Content.Shared.DoAfter` | `SharedDoAfterSystem` | `_doAfter` | 147 |
| `Content.Shared.Whitelist` | `EntityWhitelistSystem` | `_whitelist` | 153 |
| `Content.Shared.Damage.Systems` | `DamageableSystem` | `_damageable` | 115 |
| `Content.Shared.Minds.Systems` | `MobStateSystem` | `_mobState` | 114 |
| `Content.Shared.Mind` | `SharedMindSystem` | `_mind` | 100 |
| `Content.Shared.EntityEffects` | `SharedEntityEffectsSystem` | `_effects` | 91 |
| `Content.Shared.Inventory` | `InventorySystem` | `_inventory` | 90 |
| `Content.Shared.Body` | `BodySystem` | `_body` | 87 |
| `Content.Shared.StatusEffectNew` | `StatusEffectsSystem` | `_status` | 75 |
| `Content.Shared.Station.Systems` | `StationSystem` | `_station` | 70 |

Client-only replacements exist for some shared types. `Content.Client.Popups.PopupSystem` is used 29 times where the client needs a popup surface, against 59 uses of `SharedPopupSystem _popupSystem`. Prefer the shared type unless the client-only one is what the surrounding file already uses.

## Field name variants

These are all in active use. Match the file you are editing rather than normalizing.

| Type | Common fields | Counts |
|---|---|---|
| `IGameTiming` | `_timing`, `_gameTiming` | 709, 108 |
| `IEntityManager` | `_entManager`, `_entMan` | 139, 69 |
| `IPrototypeManager` | `_proto`, `_prototypeManager` | 101, 93 |
| `IPlayerManager` | `_player`, `_playerManager` | 138, 122 |
| `SharedPopupSystem` | `_popup`, `_popupSystem` | 483, 59 |
| `SharedDoAfterSystem` | `_doAfter`, `_doAfters` | 354, 5 |

`_entMan`, `_proto`, and `_gameTiming` are the short forms. `_entManager`, `_prototypeManager`, and `_timing` are the long forms. Neither is deprecated.

## Common usings in systems

Ordered by frequency across all `*System.cs` files:

```
Robust.Shared.Timing                 769
Content.Shared.Popups                624
Robust.Shared.Audio.Systems          490
System.Linq                          461
Robust.Shared.Random                 444
Robust.Shared.Prototypes             379
Robust.Shared.Player                 360
Content.Shared.Interaction           338
Robust.Shared.Containers             312
Content.Shared.Examine               300
Robust.Shared.Map                    291
Content.Shared.Damage.Systems        279
Content.Shared.Database              249
Robust.Shared.Utility                219
Content.Shared.Whitelist             211
Content.Shared.DoAfter               209
Content.Shared.Inventory             206
Content.Shared.Actions               202
Content.Shared.Mobs.Systems          188
Robust.Client.GameObjects            187
Content.Shared.IdentityManagement    181
Content.Shared.Verbs                 178
JetBrains.Annotations               174
Content.Shared.Mind                  165
Content.Shared.Hands.EntitySystems   162
Robust.Shared.Audio                  161
Content.Shared.Administration.Logs   159
Content.Shared.Mobs.Components       157
Robust.Shared.Configuration          156
Robust.Server.GameObjects            156
```

`JetBrains.Annotations` supplies `[PublicAPI]`, imported by 550 files and applied 507 times. Use it on public system methods rather than prose saying they are public API.

## Common usings in components

Ordered by frequency across all `*Component.cs` files:

```
Robust.Shared.GameStates                                     1277
Robust.Shared.Prototypes                                      559
Robust.Shared.Audio                                           541
Robust.Shared.Serialization.TypeSerializers.Implementations.Custom  332
Robust.Shared.Serialization                                    219
Content.Shared.Whitelist                                      184
Content.Shared.FixedPoint                                     157
Content.Shared.Damage                                         149
Robust.Shared.Containers                                       68
Content.Shared.Actions                                         66
Content.Shared.EntityEffects                                   61
System.Numerics                                               60
Content.Shared.DeviceLinking                                  55
Robust.Shared.Utility                                         53
Content.Shared.Atmos                                          52
Content.Shared.Tag                                            46
Content.Shared.DoAfter                                        45
Robust.Shared.Map                                             42
Content.Shared.Chemistry.Reagent                              42
Content.Shared.Alert                                          42
Content.Shared.Inventory                                       37
Content.Shared.StatusIcon                                      33
```

`Robust.Shared.GameStates` leads because networked components need `[NetworkedComponent]` and `[AutoNetworkedField]`. `Robust.Shared.Serialization.TypeSerializers.Implementations.Custom` supplies custom `[DataField(customTypeSerializer: ...)]` types.

## Reading the table correctly

These names are frequent, not guaranteed. Verify the declaration before use:

- a type may be renamed, and this file will not notice;
- a `Shared*` prefix may be dropped when a counterpart no longer exists;
- several types share a namespace, so the namespace alone does not identify the type;
- a system may have both a shared and a client or server variant, as popups and entity management do.

Namespaces here were resolved from current declarations in both the content projects and the engine. Where a type was not found under the expected name, it is listed under the name that exists, for example `MobStateSystem` rather than an assumed `SharedMobStateSystem`, and `StatusEffectsSystem` in `Content.Shared.StatusEffectNew` rather than `Content.Shared.StatusEffects`.

Reading engine sources to verify a signature is allowed. Modifying the engine is not. See `.agents/rules/engine-boundaries.md`.
