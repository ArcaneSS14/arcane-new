import json, subprocess, re, os

data=json.load(open(".orion-work/ids/extract.json"))

# pre-load all base identifiers once for speed
def sh(*a): return subprocess.run(a,capture_output=True,text=True,errors="replace").stdout

# collect class -> file mapping from base (approximate via class name grep, cached)
import functools
cache={}
def grep_base(tok):
    if tok in cache: return cache[tok]
    # search class/proto tokens in code+prototypes
    r=subprocess.run(["git","grep","-l","-F",tok,"--","Content.Server","Content.Shared","Content.Client","Resources/Prototypes","Resources/Locale"],capture_output=True,text=True,errors="replace")
    files=[l for l in r.stdout.splitlines() if l]
    cache[tok]=files
    return files

results={}
for pr,v in data.items():
    if "err" in v:
        results[pr]={"verdict":"UNCERTAIN","reason":"diff-error"}; continue
    classes=v.get("classes",[]); protos=v.get("protos",[])
    added_files=v.get("added_files",[])
    europa=[f for f in added_files if "_Europa" in f or "_europa" in f]
    non_europa_added=[f for f in added_files if "_Europa" not in f and "_europa" not in f]

    found_classes=[c for c in classes if grep_base(c)]
    found_protos=[p for p in protos if grep_base(p)]

    total_new_ids=len(classes)+len(protos)
    total_found=len(found_classes)+len(found_protos)

    # Heuristic verdict:
    # - If PR only touches _Europa (Orion-own) AND none of its new ids in base -> KEEP (Orion-specific)
    # - If most new ids already in base -> CUT (already present)
    # - mixed -> UNCERTAIN
    if total_new_ids==0:
        # no extractable ids (tweak/fix to existing) -> likely KEEP unless purely europa
        if added_files and len(europa)==len(added_files):
            verdict="KEEP"; reason="Orion-only (_Europa), no new ids"
        else:
            verdict="UNCERTAIN"; reason="no new ids, touches existing"
    else:
        ratio=total_found/total_new_ids
        if ratio==0:
            verdict="KEEP"; reason=f"0/{total_new_ids} ids in base"
        elif ratio>=0.6:
            verdict="CUT"; reason=f"{total_found}/{total_new_ids} ids already in base"
        else:
            verdict="UNCERTAIN"; reason=f"{total_found}/{total_new_ids} partial"

    results[pr]={"verdict":verdict,"reason":reason,
                 "found_classes":found_classes,"found_protos":found_protos,
                 "n_added":v["n_added"],"europa":len(europa),
                 "added_files":added_files[:8]}

json.dump(results, open(".orion-work/ids/classify.json","w"), ensure_ascii=False, indent=0)
from collections import Counter
c=Counter(r["verdict"] for r in results.values())
print("ИТОГ:", dict(c))
for pr,r in sorted(results.items(), key=lambda x:int(x[0])):
    print(f"{pr:>4} {r['verdict']:<9} {r['reason']}")
