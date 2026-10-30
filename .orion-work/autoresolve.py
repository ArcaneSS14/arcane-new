"""Resolve merge3 conflicts that only differ from base by known fork-wide renames.

ours == R(base)  -> take R(theirs) if theirs carries an Arcane marker, else keep ours (cosmetic Orion change)
otherwise         -> leave the conflict for manual work
"""
import re, sys

RENAMES = [
    (r'\bPrototypeManager\.', 'ProtoMan.'),
    (r'\[Dependency\] private readonly ', '[Dependency] private '),
    (r'\[Dependency\] protected readonly ', '[Dependency] protected '),
    (r'public sealed class (\w+System)\b', r'public sealed partial class \1'),
]
CONF = re.compile(r'^<<<<<<< ours\n(.*?)^\|\|\|\|\|\|\| orion-base\n(.*?)^=======\n(.*?)^>>>>>>> orion-\d+\n', re.S | re.M)


def norm(x):
    lines = [l for l in x.split('\n') if 'SPDX-' not in l]
    x = '\n'.join(lines)
    for a, b in RENAMES:
        x = re.sub(a, b, x)
    return re.sub(r'[ \t]+\n', '\n', x).strip()


def apply(x):
    lines = [l for l in x.split('\n') if 'SPDX-' not in l]
    x = '\n'.join(lines)
    for a, b in RENAMES:
        x = re.sub(a, b, x)
    # drop a dangling "//" left by removed SPDX blocks at the top
    return re.sub(r'\A(\s*//\s*\n)+', '', x)


for f in sys.argv[1:]:
    s = open(f).read()
    left = 0

    def rep(m):
        global left
        ours, base, theirs = m.group(1), m.group(2), m.group(3)
        if norm(ours) == norm(base):
            if 'Arcane' in theirs:
                return apply(theirs)
            if norm(theirs).replace(' ', '').replace('\n', '') == norm(base).replace(' ', '').replace('\n', ''):
                return ours
            # unmarked Orion change: keep ours but flag
            left += 1
            return m.group(0)
        if norm(ours) == norm(theirs):
            return ours
        left += 1
        return m.group(0)

    n = CONF.sub(rep, s)
    if n != s:
        open(f, 'w').write(n)
    print(f'{f}: {left} left')
