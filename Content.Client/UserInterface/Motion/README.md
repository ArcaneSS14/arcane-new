<!-- Arcane-Start -->
# Client UI motion

Options → Misc exposes independent groups, timings, reset buttons, presets and a preview of **saved** settings.
Apply changed settings before opening the preview. `ui.animations=false` disables all new effects.
All preferences are archived client CVars defined in `UiMotionSettings.cs`.

## Motion language

Timed movement and fades use the official Material 3 spring-to-curve conversions:

| Token | Cubic bezier | Default duration |
| --- | --- | --- |
| Expressive fast spatial | `0.42, 1.67, 0.21, 0.90` | 350 ms |
| Expressive default spatial | `0.38, 1.21, 0.22, 1.00` | 500 ms |
| Expressive slow spatial | `0.39, 1.29, 0.35, 0.98` | 650 ms |
| Fast effects | `0.31, 0.94, 0.34, 1.00` | 150 ms |
| Default effects | `0.34, 0.80, 0.34, 1.00` | 200 ms |
| Slow effects | `0.34, 0.88, 0.34, 1.00` | 300 ms |

Menus and vote surfaces composite a spatial curve with the matching shorter effects curve, so content becomes
readable before travel settles. Interruptible radial hover uses the expressive fast spatial spring directly
(`damping = 0.6`, `stiffness = 800`) and preserves velocity when its target reverses. Other feedback uses effects
curves and does not move input geometry. Sources: [Material 3 motion overview](https://m3.material.io/styles/motion/overview/how-it-works)
and [motion specifications](https://m3.material.io/styles/motion/overview/specs).

## Coverage

| Surface | Behaviour | Settings prefix |
| --- | --- | --- |
| Context menus | Entrance, local item sequence, inert exit image | `ui.motion_menu_*`, `ui.animation_closing` |
| Main window root | Expressive spatial entrance with a faster fade; buttons sequence within their own parent | `ui.motion_windows`, `ui.motion_window_enter_ms`, `ui.motion_window_travel`, `ui.motion_button*` |
| TabContainer pages | Fade on selection without moving content or changing hit areas | `ui.motion_tabs*` |
| Vote popups | Fade and horizontal entrance from the left; normal vote input begins at its final position | `ui.motion_votes*` |
| Popups and tooltips | Entrance fade; immediate close; no second fade on their children | `ui.motion_popups*` |
| SlotControl (inventory/hands/belt) | Icon fade/scale, transient outgoing icon, highlight fade | `ui.motion_slot*` |
| Storage grid | Icon entrance; outgoing visual copy except during dragging | `ui.motion_slot*` |
| Action hotbar | Local icon sequence and optional border pulse at cooldown boundaries | `ui.motion_actions*` |
| Alerts | Icon entrance/exit fade, optional single severity border pulse | `ui.motion_alerts*` |
| Chat | New unread message border indicator; no replay for coalesced repeats | `ui.motion_chat*` |
| AHelp and changelog | Channel/tab transition and notification feedback; message history keeps its layout | `ui.motion_tabs*`, `ui.motion_notifications*` |
| Power monitoring | Separate display interpolation; target value changes immediately | `ui.motion_progress*` |
| Radial menus | Sector sequence, hover colour/outer-radius interpolation, snapshot exit | `ui.motion_radial*` |
| Player lists and guide navigation | Bounded sequence of rows, without moving their hit areas | `ui.motion_lists*` |
| Speech bubbles | Entrance/exit, optional movement and text reveal | `ui.motion_bubble*`, `ui.motion_speech*` |

### XAML audit

The release audit covers all 501 tracked XAML documents: 240 window roots, 4 popup roots, 5 radial-menu roots,
and 252 reusable controls, pages, fragments and HUD widgets. Every audited window ultimately derives from
`BaseWindow`, including custom window root types, so the root controller covers their appearance without rewriting
individual XAML files. Popup and radial roots use their dedicated lifecycle paths. Changelog tabs, Guidebook tree
navigation, action/alert/storage HUD widgets and reusable controls are animated at their owning lifecycle boundary.

Reusable children deliberately do not each receive a wrapper. Animating both a window and every nested XAML control
would multiply opacity, replay effects during state refreshes and make input-heavy interfaces feel slower. New window
types inherit root motion automatically; a genuinely independent popup, list-row family or dynamic visual should use
the matching opt-in control described above.

`ui.motion_reduce=true` removes cascades, travel, overshoot, icon scale, new pulses and progress interpolation.
Fades are limited to 100 ms and playback uses 1× speed. Typing has its own independent toggle.
Setting a duration to zero makes that transition immediate. Window entrance can be disabled separately.
Subtle, Expressive and Reduced presets stage values in the normal options workflow; Off disables the master switch.

## Ownership and lifetime

- `UiMotionUIController` observes public tree/visibility events in WindowRoot, PopupRoot, ModalRoot and StateRoot.
  Only pending appearances and active fades require controller frame work; there is no per-frame tree scan.
- `UiMotionVisual` changes dedicated visual children and restores the original tint/scale, preserving owner changes.
  Slot entity references, action state, cooldown time, input filters and hit geometry stay independent of the transition.
- A slot retains at most one outgoing icon. Removing its entity, hiding/removing the slot or disabling its group clears it.
  Prototype previews use a separate EntityPrototypeView with its normal entity cleanup.
- `UiMotionContainer` captures through public render APIs. Exit images contain a texture, never live window controls.
  Captures are released on hide/removal, resize, disable or transfer; snapshots release their texture when orphaned.
  Captures above 4,194,304 pixels are skipped. Radial menus require an additional capture render while visible.
- Time-based cooldown rendering already updates from the actual clock; it is reused without delaying readiness.

## Deliberate limits

- No RobustToolbox or networking changes. Windows close immediately because `BaseWindow.Close()` removes the live
  control before notifying content. Delaying that close would retain focus and BUI state; continuously caching all
  windows would add unacceptable GPU cost. Existing inert snapshots remain limited to opt-in menus and radial surfaces.
- OutputPanel stores private RichTextEntry values, not row controls. Chat uses a border indicator, not per-line fades.
- Alerts and storage use a conservative fade rather than layout scale. Storage exit copies can simplify rotated/custom item placement.
- Text reveal uses sandbox-permitted Rune APIs. Combining marks, common emoji sequences, flags and CRLF stay together;
  decomposed Hangul and Indic consonant conjuncts can reveal in multiple steps. This is not full UAX #29 segmentation.
  Text outlines are temporarily disabled during reveal because engine outlines ignore transparent text colour.
- Headless and unit checks do not establish visual quality or actual GPU allocation behaviour.

## Focused verification

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore -v:q
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter 'FullyQualifiedName~UiMotionTest|FullyQualifiedName~ListContainerTest'
bin/Content.Client/Content.Trauma.Client.exe --headless
git diff --check
git diff --stat
```

The headless process is a startup check: look for `Content.Client: Verified IL` and sandbox errors, then stop it.
Use the existing local engine checkout; do not update the submodule as part of verification.

Manual checks: replay the options preview, rapidly replace/remove items, drag storage items, switch hotbar pages,
open/close/reopen windows, change alert severity and disable groups during transitions. Check input, scrolling,
text readability, snapshot alignment and texture lifetime with real rendering.
<!-- Arcane-End -->
