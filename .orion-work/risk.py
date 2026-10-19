import json,re
modf=json.load(open(".orion-work/ids/modfiles.json"))
V=json.load(open(".orion-work/FINAL.json"))
keep=sorted(p for p in V if V[p]["verdict"]=="KEEP")
def risk_of(files):
    inh=eu=arc=new=0
    kinds=set()
    for f in files:
        fn=f["file"]
        if "_Europa" in fn or "_Orion" in fn or "_europa" in fn: eu+=1
        elif fn.startswith("Content.Arcane.") or "/_Arcane/" in fn or "_Arcane/" in fn: arc+=1
        elif fn.startswith(("Content.Server","Content.Shared","Content.Client")) or fn.startswith("Resources/Prototypes/Entities") or fn.startswith("Resources/Prototypes/Catalog") or fn.startswith("Resources/Locale/en-US") or fn.startswith("Resources/Maps"):
            inh+=1; kinds.add(fn.split("/")[0]+"/"+(fn.split("/")[1] if "/" in fn else ""))
        else: new+=1
    return inh,eu,arc,new,kinds
rows=[]
for p in keep:
    files=modf.get(str(p),[])
    inh,eu,arc,new,kinds=risk_of(files)
    lvl="низкий" if inh==0 else ("средний" if inh<=2 else "высокий")
    rows.append((p,inh,eu,arc,new,lvl,sorted(kinds)[:4]))
rows.sort(key=lambda r:(-r[1],r[0]))
print(f"{'PR':>4} {'насл':<5} {'Europa':<6} {'Arcane':<6} {'новые':<5} {'риск':<8} затронутые унаследованные")
for p,inh,eu,arc,new,lvl,kinds in rows:
    print(f"{p:>4} {inh:<5} {eu:<6} {arc:<6} {new:<5} {lvl:<8} {','.join(kinds)}")
from collections import Counter
print("\nУровни риска:",dict(Counter(r[5] for r in rows)))
# save
json.dump([{"pr":r[0],"inh":r[1],"eu":r[2],"arc":r[3],"new":r[4],"risk":r[5],"kinds":r[6]} for r in rows],open(".orion-work/risk.json","w"),ensure_ascii=False,indent=0)
