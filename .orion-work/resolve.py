"""resolve.py FILE STRATEGY[,STRATEGY...]   strategies per remaining conflict, in order:
ours | theirs | both (ours then theirs) | usings (header conflict: add the new usings after the file's usings)
"""
import re, sys
CONF = re.compile(r'^<<<<<<< ours\n(.*?)^\|\|\|\|\|\|\| orion-base\n(.*?)^=======\n(.*?)^>>>>>>> orion-\d+\n', re.S | re.M)
f, strategies = sys.argv[1], sys.argv[2].split(',')
s = open(f).read()
pending_usings = []
it = iter(strategies)


def rep(m):
    st = next(it)
    ours, base, theirs = m.group(1), m.group(2), m.group(3)
    clean = lambda x: '\n'.join(l for l in x.split('\n') if 'SPDX-' not in l and l.strip() != '//')
    if st == 'ours':
        return ours
    if st == 'theirs':
        return clean(theirs).lstrip('\n')
    if st == 'both':
        return ours + clean(theirs).lstrip('\n')
    if st == 'usings':
        base_usings = set(re.findall(r'^using [\w.]+;', base, re.M))
        pending_usings.extend(u for u in re.findall(r'^using [\w.]+;', theirs, re.M) if u not in base_usings)
        return ours
    raise SystemExit('bad strategy ' + st)


s = CONF.sub(rep, s)
if pending_usings:
    ns = re.search(r'^namespace ([\w.]+)', s, re.M)
    ns = ns.group(1) if ns else ''
    existing = set(re.findall(r'^using [\w.]+;', s, re.M))
    add = []
    for u in pending_usings:
        name = u[6:-1]
        if u in existing or u in add or ns == name or ns.startswith(name + '.'):
            continue
        add.append(u)
    if add:
        last = list(re.finditer(r'^using [^\n]*\n', s, re.M))
        pos = last[-1].end() if last else 0
        block = (add[0] + ' // Arcane\n') if len(add) == 1 else ('// Arcane-Start\n' + '\n'.join(add) + '\n// Arcane-End\n')
        s = s[:pos] + block + s[pos:]
open(f, 'w').write(s)
print(f, 'remaining', len(CONF.findall(s)))
