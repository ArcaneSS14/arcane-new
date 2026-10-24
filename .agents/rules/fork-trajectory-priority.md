# Ownership on the Trauma sync trajectory

Arcane receives TraumaStation changes. Confirm the actual source refs/remotes for sync work; a remote name alone does not establish provenance. This policy chooses placement, not the user's Git strategy.

| Path class | Examples | Placement and edits |
| --- | --- | --- |
| Arcane-owned | root Content.Arcane.*, existing Resources/**/_Arcane/** including locale roots | Default for Arcane-only behavior; no Arcane marker |
| Trauma sync-sensitive | Content.Trauma.*, Resources/**/_Trauma/**, *.Trauma.cs | Supported Trauma-compatible changes may belong here; mark our changes with Arcane markers |
| Vanilla base | Root Content.* and resources without fork ownership | Use when supported owner extensions cannot express the behavior; keep inherited edits minimal and marked |
| Other fork | Content.Goobstation.*, Content.Lavaland.*, _Goobstation, _Lavaland, Content.Medical.*, _Shitmed and other foreign owners | Preserve the owner's behavior/conventions; do not place Arcane-only work here. Necessary inherited adaptations still mark our changes with Arcane markers |

Ownership is confirmed by current metadata and history, not just namespace. Other-fork paths are not Arcane-owned even if outside the usual sync path. See [port-destination](port-destination.md) for Trauma additions and restoration exceptions.

## Keep inherited changes reconcilable

- Try an existing Arcane system, supported partial or prototype extension first, only when it preserves behavior and dependencies.
- Append list additions where order permits. Group adjacent compatible additions, not unrelated changes across unchanged content.
- Prefer additive changes/partial overrides or supported !Remove when they express the requested behavior. Do not preserve the wrong behavior just to avoid a base edit.
- Keep the necessary hunk narrow; leave neighbours, formatting and order alone.
- Apply [marker syntax](arcane-edit-markers.md) to our added/changed lines, preserve untouched upstream markers, and keep added usings last in the using group. Do not nest or interleave foreign/Arcane blocks.
- Explain necessary inherited edits and their ownership reason in the handoff.

## Partial files and resources

A fork-suffixed partial can avoid a base edit only within the same assembly and when the declaration/hook supports it. Match existing naming and ownership: an Arcane-authored partial is not made Trauma-owned by copying a .Trauma.cs suffix. Confirm an established appropriate Arcane suffix/path before creating one. Adding partial or a hook to a base declaration is itself a marked inherited change.

Arcane-owned resource directories and supported partial prototypes can keep an override separate. Verify loader merge/remove behavior, effective defaults and references; directory placement alone does not prove compatibility. Follow the existing migration owner for compatibility-sensitive data.

## Actual conflicts

Use [merge-conflict-resolution](merge-conflict-resolution.md), which preserves confirmed Arcane intent while accepting compatible incoming changes. A marker locates history, not correctness or whole-file priority. Sprite/map conflicts require the user. Do not silently drop either side or choose merge/rebase strategy to make conflicts disappear.
