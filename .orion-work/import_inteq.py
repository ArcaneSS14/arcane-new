#!/usr/bin/env python3
"""Import Orion InteQ-group PRs into Arcane-normalized paths (inteq branch).

Extends the pack0 importer with union-merge support for shared files.
Usage: import_inteq.py <pr> [<pr> ...]
"""
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import import_pack0 as ip

LOG = os.path.join(ip.REPO, '.orion-work', 'import_inteq_log.txt')

CFG = {
    16: {
        'skip': [],
        'manual': [
            'Content.Client/Administration/AdminNameOverlay.cs',
            'Resources/Locale/ru-RU/prototypes/entities/clothing/head/base_clothinghead.ftl',
            'Resources/Prototypes/Access/misc.yml',
            'Resources/Prototypes/Catalog/Fills/Lockers/misc.yml',
            'Resources/Prototypes/Entities/Clothing/Masks/masks.yml',
            'Resources/Prototypes/Entities/Markers/Spawners/Random/bedsheet.yml',
            'Resources/Prototypes/Entities/Markers/Spawners/Random/posters.yml',
            'Resources/Prototypes/Entities/Markers/Spawners/Random/pottedplants.yml',
            'Resources/Prototypes/Entities/Markers/Spawners/Random/trash.yml',
            'Resources/Prototypes/_Goobstation/_Pirates/faction.yml',
            'Resources/Prototypes/_Goobstation/ai_factions.yml',
            'Resources/Prototypes/_Lavaland/Procedural/lavaland_grid_ruins.yml',
            'Resources/Prototypes/_Lavaland/Procedural/ruin_pools.yml',
            'Resources/Prototypes/ai_factions.yml',
            'Resources/Textures/Objects/Misc/bureaucracy.rsi/meta.json',
            'Resources/Textures/_Goobstation/Objects/Weapons/Throwable/boomerangIncomplete.rsi/meta.json',
            'Resources/migration.yml',
        ],
        'merge': {
            'Resources/Locale/en-US/_europa/datasets/localizedDataset.ftl',
            'Resources/Locale/en-US/_europa/ghost/roles/ghost-role-component.ftl',
            'Resources/Locale/en-US/_europa/mind/role-types.ftl',
            'Resources/Locale/en-US/_europa/station-events/events/events.ftl',
            'Resources/Locale/en-US/_europa/tiles/tiles.ftl',
            'Resources/Locale/ru-RU/_europa/datasets/localizedDataset.ftl',
            'Resources/Locale/ru-RU/_europa/ghost/roles/ghost-role-component.ftl',
            'Resources/Locale/ru-RU/_europa/mind/role-types.ftl',
            'Resources/Locale/ru-RU/_europa/station-events/events/events.ftl',
            'Resources/Locale/ru-RU/_europa/tiles/tiles.ftl',
            'Resources/Prototypes/_Europa/GameRules/events.yml',
            'Resources/Prototypes/_Europa/Roles/role_types.yml',
            'Resources/Prototypes/_Europa/Tiles/floors.yml',
        },
        'delta': set(),
    },
    101: {'skip': [], 'manual': [], 'delta': set()},
    123: {'skip': [], 'manual': [], 'delta': set()},
    228: {'skip': [], 'manual': [], 'delta': set()},
    254: {
        'skip': [],
        'manual': [
            'Resources/Prototypes/Loadouts/loadout_groups.yml',
            'Resources/Prototypes/Loadouts/role_loadouts.yml',
        ],
        'delta': set(),
    },
}

LOGGED = set()
if os.path.exists(LOG):
    for line in open(LOG):
        parts = line.rstrip('\n').split('\t')
        if len(parts) >= 3:
            LOGGED.add((int(parts[0][2:]), parts[2]))


def merge_file(pr, git_path, dest):
    if (pr, dest) in LOGGED:
        print(f'pr{pr}: merge already done {dest}')
        return
    theirs = ip.git('show', f'refs/orion/pr/{pr}:{git_path}')
    theirs = ip.content_transform(theirs, pr)
    full = os.path.join(ip.REPO, dest)
    if not os.path.exists(full):
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, 'w', encoding='utf-8') as f:
            f.write(theirs)
        with open(LOG, 'a') as f:
            f.write(f'pr{pr}\t{git_path}\t{dest}\tmerge-create\n')
        print(f'pr{pr}: merge-create {dest}')
        return
    ours = open(full, encoding='utf-8', errors='replace').read()
    theirs_lines = [l for l in theirs.splitlines() if l.strip()]
    missing = [l for l in theirs_lines if l not in ours]
    if not missing:
        print(f'pr{pr}: merge skip (ours already contains theirs) {dest}')
        return
    with open(full, 'a', encoding='utf-8') as f:
        f.write('\n')
        f.write('\n'.join(missing))
        f.write('\n')
    with open(LOG, 'a') as f:
        f.write(f'pr{pr}\t{git_path}\t{dest}\tmerge+{len(missing)}\n')
    print(f'pr{pr}: merge +{len(missing)} lines -> {dest}')


def main(prs):
    for pr in prs:
        pr = int(pr)
        cfg = CFG[pr]
        n = 0
        for status, git_path in ip.namestatus(pr):
            if git_path in cfg['skip'] or git_path in cfg['manual']:
                continue
            dest = ip.path_transform(git_path)
            if status.startswith('A') or status.startswith('M'):
                if git_path in cfg['merge']:
                    merge_file(pr, git_path, dest)
                    n += 1
                    continue
                data = ip.git('show', f'refs/orion/pr/{pr}:{git_path}', binary=True)
                ip.write_file(pr, git_path, dest, data, ip.is_text(git_path))
                n += 1
            elif status.startswith('D'):
                raise SystemExit(f'pr{pr}: unexpected delete {git_path}')
            else:
                raise SystemExit(f'pr{pr}: unknown status {status} {git_path}')
        print(f'== PR {pr}: wrote/merged {n} files')


if __name__ == '__main__':
    main(sys.argv[1:])
