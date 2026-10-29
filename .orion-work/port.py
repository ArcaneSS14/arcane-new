#!/usr/bin/env python3
"""Port one Orion PR into Arcane paths.

usage: port.py <pr> [--dry] [--skip REGEX] [--only REGEX]

- Files added by the PR under _Orion/_Europa trees are written at their Arcane path
  (contents namespace/marker-transformed), unless the target already exists.
- Everything else (vanilla files, Orion files we already ported) gets the PR's diff,
  transformed the same way, applied with `patch -F2`; rejects stay as *.rej for manual work.
- ru-RU locales, ss14-ru prototype locales and repo tooling are skipped.
Prints a report: written, patched, rejected, missing targets, skipped.
"""
import os
import re
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from import_pack0 import path_transform, content_transform  # noqa: E402

BINARY = ('.png', '.ogg', '.wav', '.webp', '.ttf', '.otf', '.jpg')
SKIP_DEFAULT = [
    r'^Resources/Locale/ru-RU/',
    r'^Resources/Locale/en-US/ss14-ru/',
    r'^\.github/', r'^Tools/', r'^RobustToolbox', r'^Resources/Changelog/',
    r'^Content\.IntegrationTests/', r'^Content\.Benchmarks/', r'\.csproj$', r'^Resources/ServerInfo/.*_ru',
]


def git(*a, binary=False):
    r = subprocess.run(['git', *a], capture_output=True)
    return r.stdout if binary else r.stdout.decode('utf-8', 'replace')


def is_orion(p):
    return bool(re.search(r'/_(?:Orion|Europa|orion|europa)(/|\.)|^Content\.(Client|Server|Shared)/_(?:Orion|Europa)/', p))


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
    written, patched, rejected, missing, skipped, deleted = [], [], [], [], [], []
    patch_parts = []
    for row in rows:
        st, src = row[0], row[-1]
        if any(re.search(s, src) for s in skips) or (only and not re.search(only, src)):
            skipped.append(src)
            continue
        dst = path_transform(src) if is_orion(src) else src
        if st == 'D':
            deleted.append(dst)
            continue
        binary = src.lower().endswith(BINARY)
        if st == 'A' and not os.path.exists(dst):
            data = git('show', f'{ref}:{src}', binary=True)
            if not binary:
                data = content_transform(data.decode('utf-8', 'replace'), pr).encode()
            written.append(dst)
            if not dry:
                os.makedirs(os.path.dirname(dst) or '.', exist_ok=True)
                open(dst, 'wb').write(data)
            continue
        if not os.path.exists(dst):
            missing.append(f'{dst}  (from {src})')
            continue
        if binary:
            # changed binary of a file we have: replace only if ours equals Orion's base
            ours = open(dst, 'rb').read()
            base = git('show', f'{mb}:{src}', binary=True)
            if ours == base or st == 'A':
                written.append(dst + ' (binary)')
                if not dry:
                    open(dst, 'wb').write(git('show', f'{ref}:{src}', binary=True))
            else:
                missing.append(f'{dst}  (binary differs from Orion base, kept ours)')
            continue
        d = git('diff', mb, ref, '--', src)
        d = d.replace(f'a/{src}', f'a/{dst}').replace(f'b/{src}', f'b/{dst}')
        d = content_transform(d, pr) if is_orion(src) else re.sub(
            r'Orion-(Start|End|Edit)', r'Arcane-\1', d).replace('// Orion', '// Arcane').replace('# Orion', '# Arcane')
        patch_parts.append((dst, d))
    for dst, d in patch_parts:
        with tempfile.NamedTemporaryFile('w', delete=False, suffix='.patch') as f:
            f.write(d)
        cmd = ['patch', '-p1', '-F2', '--no-backup-if-mismatch', '-i', f.name]
        if dry:
            cmd.append('--dry-run')
        r = subprocess.run(cmd, capture_output=True, text=True)
        os.unlink(f.name)
        fails = re.findall(r'(\d+) out of (\d+) hunks? FAILED', r.stdout)
        if fails:
            rejected.append(f'{dst}  {fails[0][0]}/{fails[0][1]} hunks failed')
        else:
            patched.append(dst)
    for title, items in [('WRITTEN', written), ('PATCHED', patched), ('REJECTED', rejected),
                         ('MISSING', missing), ('DELETED in PR', deleted), ('SKIPPED', skipped)]:
        print(f'== {title} ({len(items)})')
        for i in items:
            print('  ', i)


if __name__ == '__main__':
    main()
