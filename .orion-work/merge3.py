#!/usr/bin/env python3
"""Three-way merge of one Orion PR into Arcane paths.

usage: merge3.py <pr> [--dry] [--skip REGEX] [--only REGEX]

For every file the PR changes: ours = our file, base = PR merge-base version, theirs = PR head version
(Orion paths and markers transformed to Arcane). `git merge-file` writes conflict markers instead of
rejecting hunks, so nothing lands in the wrong place silently. New files are written when absent.
"""
import os
import re
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from import_pack0 import path_transform, content_transform  # noqa: E402
from port import BINARY, SKIP_DEFAULT, git, is_orion  # noqa: E402


def transform(text, src, pr):
    if is_orion(src):
        return content_transform(text, pr)
    return re.sub(r'Orion-(Start|End|Edit)', r'Arcane-\1', text).replace('// Orion', '// Arcane').replace('# Orion', '# Arcane')


# Files moved out of Content.Arcane.* because vanilla partial classes or core code use them (Orion #320).
REMAP = [
    (r'^Content\.Arcane\.Server/Research/Systems/(ResearchSystem\.\w+)\.cs$', r'Content.Server/Research/Systems/\1.Arcane.cs', False),
    (r'^Content\.Arcane\.Shared/Research/(.+)\.cs$', r'Content.Shared/Research/\1.Arcane.cs', True),
    (r'^Content\.Arcane\.Client/Research/UI/(FancyResearchConsoleLogWindow\.xaml(?:\.cs)?)$', r'Content.Goobstation.Client/Research/UI/\1', True),
    # Machine parts (Orion #385): vanilla machines use these types.
    (r'^Content\.Arcane\.Shared/Construction/(.+)\.cs$', r'Content.Shared/Construction/\1.Arcane.cs', False),
    (r'^Content\.Arcane\.Server/Construction/Systems/(ConstructionSystem\.Machine\.Upgrades)\.cs$', r'Content.Server/Construction/\1.Arcane.cs', False),
]


def remap(dst):
    for pat, rep, need_exists in REMAP:
        if re.match(pat, dst):
            new = re.sub(pat, rep, dst)
            if not need_exists or os.path.exists(new):
                return new
    return dst


def main():
    args = sys.argv[1:]
    pr = int(args[0])
    dry = '--dry' in args
    skips = list(SKIP_DEFAULT)
    only = None
    for i, a in enumerate(args):
        if a == '--skip':
            skips.append(args[i + 1])
        if a == '--only':
            only = args[i + 1]
    ref = f'refs/orion/pr/{pr}'
    mb = git('merge-base', ref, 'refs/orion/master').strip()
    rows = [l.split('\t') for l in git('diff', '--name-status', '--no-renames', mb, ref).splitlines() if l]
    res = {k: [] for k in ('written', 'merged', 'conflicts', 'deleted', 'skipped', 'binary-kept')}
    for row in rows:
        st, src = row[0], row[-1]
        if any(re.search(s, src) for s in skips) or (only and not re.search(only, src)):
            res['skipped'].append(src)
            continue
        dst = remap(path_transform(src) if is_orion(src) else src)
        if st == 'D':
            res['deleted'].append(dst)
            continue
        binary = src.lower().endswith(BINARY)
        head = git('show', f'{ref}:{src}', binary=True)
        if not os.path.exists(dst):
            if st != 'A':
                res['conflicts'].append(f'{dst} (MISSING in fork)')
                continue
            res['written'].append(dst)
            if not dry:
                os.makedirs(os.path.dirname(dst) or '.', exist_ok=True)
                open(dst, 'wb').write(head if binary else transform(head.decode('utf-8', 'replace'), src, pr).encode())
            continue
        if binary:
            base = git('show', f'{mb}:{src}', binary=True) if st != 'A' else b''
            if open(dst, 'rb').read() == base:
                res['written'].append(dst + ' (binary)')
                if not dry:
                    open(dst, 'wb').write(head)
            else:
                res['binary-kept'].append(dst)
            continue
        base = git('show', f'{mb}:{src}') if st != 'A' else ''
        with tempfile.TemporaryDirectory() as tmp:
            b, t = os.path.join(tmp, 'base'), os.path.join(tmp, 'theirs')
            open(b, 'w').write(transform(base, src, pr))
            open(t, 'w').write(transform(head.decode('utf-8', 'replace'), src, pr))
            r = subprocess.run(['git', 'merge-file', '-p', '--diff3', '-L', 'ours', '-L', 'orion-base', '-L', f'orion-{pr}', dst, b, t],
                               capture_output=True)
        out = r.stdout
        n = r.returncode
        if n < 0:
            res['conflicts'].append(f'{dst} (merge-file error)')
            continue
        if not dry:
            open(dst, 'wb').write(out)
        (res['conflicts'] if n > 0 else res['merged']).append(f'{dst}' + (f'  {n} conflicts' if n else ''))
    for k, v in res.items():
        print(f'== {k.upper()} ({len(v)})')
        for i in v:
            print('  ', i)


main()
