#!/usr/bin/env python3
"""Import Orion PR pack0 files into Arcane-normalized paths.

Usage: import_pack0.py <pr> [<pr> ...]
Writes files, logs every (pr, src, dst) to .orion-work/import_log.txt.
Does NOT touch vanilla/Goob files present in our tree (manual edits done separately).
"""
import json
import os
import re
import subprocess
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOG = os.path.join(REPO, '.orion-work', 'import_log.txt')

BINARY_EXT = ('.png', '.ogg', '.webp', '.ogg', '.ogg')

# per-PR configuration
# skip  : deferred (missing introducing PR in this pack) or replaced elsewhere
# manual: file exists in our tree, edited by hand with Arcane markers
# delta : file absent in our tree; write ONLY the lines this PR adds
# fulldir: import entire directory at the PR ref (self-contained assets)
CFG = {
    17: {
        'skip': [],
        'manual': [],
        'delta': {'Resources/Prototypes/_Europa/GameRules/events.yml'},
    },
    25: {'skip': [], 'manual': [], 'delta': set()},
    31: {
        'skip': ['Resources/Prototypes/_Europa/Entities/Clothing/Belt/belts.yml'],
        'manual': ['Resources/Prototypes/Recipes/Lathes/Packs/security.yml'],
        'delta': {
            'Resources/Prototypes/_Europa/Catalog/uplink_catalog.yml',
            'Resources/Prototypes/_Europa/Damage/modifier_sets.yml',
        },
    },
    38: {'skip': [], 'manual': [], 'delta': set()},
    40: {
        'skip': ['Resources/Locale/en-US/_europa/stacks/stacks.ftl'],  # key already in tree (_Goobstation/xenobio/stacks.ftl)
        'manual': [
            'Resources/Prototypes/Recipes/Lathes/Packs/science.yml',
            'Resources/Prototypes/Research/civilianservices.yml',
            'Resources/Prototypes/_Europa/Recipes/Construction/structures.yml',  # appends into our pre-existing _Arcane file
        ],
        'delta': set(),
    },
    57: {
        'skip': [
            'Resources/Maps/_Europa/Lavaland/InteQ_Sizo.yml',       # introduces: Orion #16
            'Resources/Prototypes/_Europa/Entities/Markers/Spawners/hermit.yml',  # introduces: Orion #20 (CUT)
            'Resources/Prototypes/_Europa/Entities/Markers/Spawners/inteq.yml',   # introduces: Orion #16
        ],
        'manual': [],
        'delta': set(),
    },
    64: {
        'skip': [],
        'manual': ['Resources/Prototypes/NPCs/mob.yml'],
        'delta': set(),
    },
    162: {'skip': 'ALL', 'manual': [], 'delta': set()},  # handled manually at its own commit
    283: {
        'skip': [
            'Resources/Prototypes/_Orion/Catalog/Fills/Backpacks/backpack.yml',  # introduces: Orion #16
            'Resources/Prototypes/_Orion/Entities/Markers/Spawners/inteq.yml',   # introduces: Orion #16
        ],
        'manual': [],
        'delta': set(),
    },
    328: {'skip': 'ALL', 'manual': [], 'delta': set()},  # manual Goob edit
    354: {'skip': [], 'manual': [], 'delta': set()},
    359: {'skip': [], 'manual': [], 'delta': set()},
    370: {
        'skip': [],
        'manual': ['Resources/Prototypes/AppearanceCustomization/station_ai.yml'],
        'delta': {
            'Resources/Locale/en-US/_Orion/silicons/station-ai.ftl',   # 15 keys collide with _Trauma/_Goobstation
            'Resources/Locale/ru-RU/_Orion/silicons/station-ai.ftl',
            'Resources/Prototypes/_Orion/AppearanceCustomization/station_ai.yml',
        },
        'fulldir': ['Resources/Textures/_Orion/Mobs/Silicon/holograms.rsi'],
    },
    375: {
        'skip': [],
        'manual': ['Resources/Prototypes/GameRules/events.yml'],
        'delta': set(),
    },
    382: {'skip': [], 'manual': [], 'delta': set()},
    429: {'skip': [], 'manual': [], 'delta': set()},
    445: {'skip': 'ALL', 'manual': [], 'delta': set()},  # manual Goob edit
}

BASENAME_MAP = {
    'orion-station.webp': 'arcane-station.webp',
    'orion-station.webp.yml': 'arcane-station.webp.yml',
}

CONTENT_EXTRA = {
    40: [('re', r'\bFloorTileItemBluespace\b', 'FloorTileItemArcaneBluespace')],
    354: [('str', 'parent: DrinkGlassBase', 'parent: [DrinkBaseMaterialGlass, SolutionSmall, DrinkBaseCup, DrinkVisualsFill]')],
}


def git(*args, binary=False):
    r = subprocess.run(['git', *args], cwd=REPO, capture_output=True)
    if r.returncode != 0:
        raise SystemExit(f'git {args} failed: {r.stderr.decode()[:400]}')
    return r.stdout if binary else r.stdout.decode('utf-8', 'replace')


def path_transform(p: str) -> str:
    m = re.match(r'^Resources/Locale/ru-RU/ss14-ru/prototypes/_(?:europa|orion)/(.*)$', p, re.I)
    if m:
        p = f'Resources/Locale/ru-RU/_Arcane/prototypes/{m.group(1)}'
    else:
        p = re.sub(r'^Resources/Locale/(en-US|ru-RU)/_(?:europa|orion)/', r'Resources/Locale/\1/_Arcane/', p, flags=re.I)
        p = re.sub(r'^(Resources/(?:Prototypes|Textures|Audio|Maps))/_(?:europa|orion)/', r'\1/_Arcane/', p, flags=re.I)
    m = re.match(r'^Content\.Server/_(?:europa|orion)/(.*)$', p, re.I)
    if m:
        p = 'Content.Arcane.Server/' + m.group(1)
    m = re.match(r'^Content\.Shared/_(?:europa|orion)/(.*)$', p, re.I)
    if m:
        p = 'Content.Arcane.Shared/' + m.group(1)
    m = re.match(r'^Content\.Client/_(?:europa|orion)/(.*)$', p, re.I)
    if m:
        p = 'Content.Arcane.Client/' + m.group(1)
    base = os.path.basename(p)
    if base in BASENAME_MAP:
        p = p[: -len(base)] + BASENAME_MAP[base]
    return p


def content_transform(t: str, pr: int) -> str:
    # namespaces first (before bare _Orion/_Europa sweeps)
    for old, new in (
        ('Content.Server._Europa.', 'Content.Arcane.Server.'),
        ('Content.Server._Orion.', 'Content.Arcane.Server.'),
        ('Content.Shared._Europa.', 'Content.Arcane.Shared.'),
        ('Content.Shared._Orion.', 'Content.Arcane.Shared.'),
        ('Content.Client._Europa.', 'Content.Arcane.Client.'),
        ('Content.Client._Orion.', 'Content.Arcane.Client.'),
        ('namespace Content.Server.EnergyDome;', 'namespace Content.Arcane.Server.EnergyDome;'),
        ('using Content.Server.EnergyDome', 'using Content.Arcane.Server.EnergyDome'),
    ):
        t = t.replace(old, new)
    # path-like references
    for old, new in (
        ('_Europa/', '_Arcane/'), ('_Orion/', '_Arcane/'),
        ('_europa/', '_Arcane/'), ('_orion/', '_Arcane/'),
        ('_Europa', '_Arcane'), ('_Orion', '_Arcane'),
        ('_europa', '_Arcane'), ('_orion', '_Arcane'),
    ):
        t = t.replace(old, new)
    # fork markers / brand names -> Arcane
    t = t.replace('Europa-', 'Arcane-').replace('Orion-', 'Arcane-')
    t = re.sub(r'\bOrion\b', 'Arcane', t)
    t = re.sub(r'\bEuropa\b', 'Arcane', t)
    t = re.sub(r'\borion\b', 'arcane', t)
    t = re.sub(r'\beuropa\b', 'arcane', t)
    # per-PR extras
    for kind, a, b in CONTENT_EXTRA.get(pr, []):
        if kind == 'str':
            t = t.replace(a, b)
        else:
            t = re.sub(a, b, t)
    return t


def merge_base(pr: int) -> str:
    return git('merge-base', f'refs/orion/pr/{pr}', 'refs/orion/master').strip()


def namestatus(pr: int):
    out = git('diff', '--name-status', merge_base(pr), f'refs/orion/pr/{pr}')
    rows = []
    for line in out.splitlines():
        parts = line.split('\t')
        rows.append((parts[0], parts[-1]))
    return rows


def added_lines(pr: int, git_path: str) -> str:
    diff = git('diff', merge_base(pr), f'refs/orion/pr/{pr}', '--', git_path)
    adds = []
    for line in diff.splitlines():
        if line.startswith('+++') or line.startswith('---'):
            continue
        if line.startswith('+'):
            adds.append(line[1:])
        elif line.startswith('@@') or line.startswith('diff ') or line.startswith('index '):
            continue
    return '\n'.join(adds) + '\n'


LOGGED = set()
if os.path.exists(LOG):
    for line in open(LOG):
        parts = line.rstrip('\n').split('\t')
        if len(parts) == 3:
            LOGGED.add((int(parts[0][2:]), parts[2]))


def write_file(pr: int, src: str, dest: str, data: bytes, text: bool):
    if (pr, dest) in LOGGED:
        return  # already imported by an earlier partial run
    full = os.path.join(REPO, dest)
    if os.path.exists(full):
        raise SystemExit(f'pr{pr}: REFUSING to overwrite existing {dest}')
    os.makedirs(os.path.dirname(full), exist_ok=True)
    if text:
        t = data.decode('utf-8', 'replace')
        t = content_transform(t, pr)
        data = t.encode('utf-8')
    with open(full, 'wb') as f:
        f.write(data)
    with open(LOG, 'a') as f:
        f.write(f'pr{pr}\t{src}\t{dest}\n')


def is_text(path: str) -> bool:
    return not path.endswith(BINARY_EXT)


def main(prs):
    for pr in prs:
        cfg = CFG.get(int(pr))
        if cfg is None:
            raise SystemExit(f'no config for PR {pr}')
        if cfg['skip'] == 'ALL':
            print(f'== PR {pr}: fully manual/special, skipped by script')
            continue
        n = 0
        for status, git_path in namestatus(int(pr)):
            if git_path in cfg['skip'] or git_path in cfg['manual']:
                continue
            if any(git_path.startswith(d + '/') for d in cfg.get('fulldir', [])):
                continue  # handled by fulldir below
            dest = path_transform(git_path)
            if status.startswith('A') or status.startswith('M'):
                if git_path in cfg['delta']:
                    data = added_lines(int(pr), git_path).encode('utf-8')
                    if not data.strip():
                        raise SystemExit(f'pr{pr}: empty delta for {git_path}')
                else:
                    data = git('show', f'refs/orion/pr/{pr}:{git_path}', binary=True)
                write_file(int(pr), git_path, dest, data, is_text(git_path))
                n += 1
            elif status.startswith('D'):
                raise SystemExit(f'pr{pr}: unexpected delete {git_path}')
            else:
                raise SystemExit(f'pr{pr}: unknown status {status} {git_path}')
        for d in cfg.get('fulldir', []):
            files = git('ls-tree', '-r', '--name-only', f'refs/orion/pr/{pr}', '--', d).splitlines()
            for git_path in files:
                dest = path_transform(git_path)
                data = git('show', f'refs/orion/pr/{pr}:{git_path}', binary=True)
                write_file(int(pr), git_path, dest, data, is_text(git_path))
                n += 1
        print(f'== PR {pr}: wrote {n} files')


if __name__ == '__main__':
    main(sys.argv[1:])
