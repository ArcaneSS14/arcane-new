# Directional appearance layering

Adapted from ArcaneSS14/arcane#360 and final old Arcane
404b7564e40e3c6f7913c124af7a9bbd956c0a40, AGPL-3.0-or-later. The source's stable
anchor, alias preservation and direction calculations are retained. Current
VisualBody/organs replace HumanoidAppearance/MarkingSet; marking lists are cached
until organ/marking/equipment updates. Current Trauma moth child layers remain
owned by their existing system. Tail overlays and HairSpecial use current layers.

The source's opt-in ears-above-hair preference is saved per character, replicated
on VisualBody and copied by its existing appearance-copy API. Existing Vulpkanin
and Tajaran ear markings receive the source flag, retaining current assets and
group restrictions. Preview ordering uses the actual SpriteView direction/eye;
no engine modifications or new species are included. A client-local drawing event
keeps the preview hook in the base client without a reverse module dependency.
