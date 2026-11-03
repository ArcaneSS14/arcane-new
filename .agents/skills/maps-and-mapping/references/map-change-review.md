
Review map diffs for:

- intended entity additions/removals;
- unexpected coordinate or rotation changes;
- all moved/copied layers: tiles, entities, decals and Trauma AreaGrid; verify area coverage at the new location;
- prototype availability;
- anchoring and parent relationships;
- container membership and intended parent/device-link targets after moves or copies;
- grid and map metadata;
- serializer-wide reorder noise;
- source-fork-only IDs;
- required module resources;
- intended routes for the relevant users and access state, including return or transport paths when required; connected floor tiles alone do not prove a usable route.

A small gameplay change should not silently rewrite thousands of unrelated map lines.

For lighting review, use client-rendered captures: an offline map overview does not reproduce game lighting.
