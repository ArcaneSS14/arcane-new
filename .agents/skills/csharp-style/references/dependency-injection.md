
Reference tables for the systems and namespaces that appear most often in this repository. Use them to pick an existing owner and a name that matches its neighbors. They are not a substitute for verifying a declaration before calling it.

Counts are `[Dependency]` declaration sites across `Content.Server`, `Content.Shared`, and `Content.Client`. A declaration count is not an importance score: `_entManager` has 131 declarations against 864 textual mentions, while `_popup` has 229 declarations against 987 mentions, so a frequently *mentioned* field is not necessarily a frequently *injected* one.

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

| Namespace | Type | Field | Declarations | Mentions |
|---|---|---|---|---|
| `Robust.Shared.Timing` | `IGameTiming` | `_timing` | 331 | 1133 |
| `Robust.Shared.Audio.Systems` | `SharedAudioSystem` | `_audio` | 227 | 715 |
| `Robust.Shared.Random` | `IRobustRandom` | `_random` | 236 | 715 |
| `Robust.Shared.GameObjects` | `SharedTransformSystem` | `_transform` | 183 | 714 |
| `Robust.Shared.GameObjects` | `SharedAppearanceSystem` | `_appearance` | 187 | 619 |
| `Robust.Shared.GameObjects` | `IEntityManager` | `_entManager` | 131 | 864 |
| `Robust.Shared.Configuration` | `IConfigurationManager` | `_cfg` | 127 | 574 |
| `Robust.Shared.Network` | `INetManager` | `_net` | 95 | 252 |
| `Robust.Shared.GameObjects` | `EntityLookupSystem` | `_lookup` | 67 | 203 |
| `Robust.Client.GameObjects` | `SpriteSystem` | `_sprite` | 101 | 682 |
| `Robust.Shared.Player` | `IPlayerManager` | `_playerManager` | 127 | 457 |
| `Robust.Shared.Physics.Systems` | `SharedPhysicsSystem` | `_physics` | 68 | 252 |
| `Robust.Shared.Prototypes` | `IPrototypeManager` | `_prototypeManager` | 90 | 343 |
| `Robust.Shared.GameObjects` | `SharedMapSystem` | `_map` | 73 | 259 |
| `Robust.Shared.GameObjects` | `SharedUserInterfaceSystem` | `_ui` | 56 | 151 |

`IGameTiming` is injected under three names in active use: `_timing` 331, `_gameTiming` 104, and `_timingManager` elsewhere. Match the file you are editing.

### Content-owned

| Namespace | Type | Field | Declarations | Mentions |
|---|---|---|---|---|
| `Content.Shared.Popups` | `SharedPopupSystem` | `_popup` | 229 | 987 |
| `Content.Shared.Administration.Logs` | `ISharedAdminLogManager` | `_adminLogger` | 144 | 544 |
| `Content.Shared.Chat` | `SharedChatSystem` | `_chat` | 65 | 190 |
| `Content.Shared.Hands.EntitySystems` | `SharedHandsSystem` | `_hands` | 75 | 250 |
| `Content.Shared.Actions` | `SharedActionsSystem` | `_actions` | 39 | 201 |
| `Content.Shared.DoAfter` | `SharedDoAfterSystem` | `_doAfter` | 61 | 139 |
| `Content.Shared.Whitelist` | `EntityWhitelistSystem` | `_whitelist` | 55 | 130 |
| `Content.Shared.Damage.Systems` | `DamageableSystem` | `_damageable` | 58 | 169 |
| `Content.Shared.Mobs.Systems` | `MobStateSystem` | `_mobState` | 51 | 130 |
| `Content.Shared.Mind` | `SharedMindSystem` | `_mind` | 53 | 172 |
| `Content.Shared.EntityEffects` | `SharedEntityEffectsSystem` | `_effects` | 6 | 20 |
| `Content.Shared.Inventory` | `InventorySystem` | `_inventory` | 46 | 130 |
| `Content.Shared.Body` | `BodySystem` | `_body` | 18 | 68 |
| `Content.Shared.StatusEffectNew` | `StatusEffectsSystem` | `_status` | 14 | 50 |
| `Content.Shared.Station.Systems` | `StationSystem` | `_station` | 36 | 163 |

Client-only replacements exist for some shared types. `Content.Client.Popups.PopupSystem` is injected in 5 client files where the client needs a popup surface, against 78 declarations of `SharedPopupSystem _popupSystem`. Prefer the shared type unless the client-only one is what the surrounding file already uses.

## Field name variants

These are all in active use. Match the file you are editing rather than normalizing.

Numbers are `[Dependency]` declarations, then total textual mentions in the same three projects. A declaration count of zero with a nonzero mention count means the field is passed around or used as a local, not injected under that name.

| Type | Common fields | Declarations | Mentions |
|---|---|---|---|
| `IGameTiming` | `_timing`, `_gameTiming` | 331, 104 | 1133, 399 |
| `IEntityManager` | `_entManager`, `_entMan` | 131, 22 | 864, 114 |
| `IPrototypeManager` | `_prototypeManager`, `_proto` | 90, 28 | 343, 80 |
| `IPlayerManager` | `_playerManager`, `_player` | 127, 92 | 457, 311 |
| `SharedPopupSystem` | `_popup`, `_popupSystem` | 229, 78 | 987, 310 |
| `SharedDoAfterSystem` | `_doAfter` | 61 | 139 |

`_entMan`, `_proto`, and `_gameTiming` are the short forms. `_entManager`, `_prototypeManager`, and `_timing` are the long forms. Neither style is deprecated.

Do not follow the `_doAfters` spelling. It has 5 textual mentions and zero `[Dependency]` declarations: it is a local or parameter name, not a field. Same for `_timingManager`.

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
