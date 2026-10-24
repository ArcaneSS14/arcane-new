import json
data=json.load(open(".orion-work/ids/extract.json"))
base=json.load(open(".orion-work/ids/base_index.json"))
bclasses=set(base["classes"]); bprotos=set(base["protos"]); bpaths=set(base["paths"])

results={}
for pr,v in data.items():
    if "err" in v:
        results[pr]={"verdict":"UNCERTAIN","reason":"diff-error"}; continue
    classes=v.get("classes",[]); protos=v.get("protos",[])
    added_files=v.get("added_files",[])
    europa=[f for f in added_files if "_Europa" in f or "_europa" in f]
    total_new=len(classes)+len(protos)
    found_c=[c for c in classes if c in bclasses]
    found_p=[p for p in protos if p in bprotos]
    found=len(found_c)+len(found_p)
    # path signal: do added file paths already exist in base?
    path_hit=[f for f in added_files if f in bpaths]
    if total_new==0:
        if added_files and len(europa)==len(added_files):
            verdict,reason="KEEP","Orion-only (_Europa), no new ids"
        elif not added_files:
            verdict,reason="KEEP","mod-only (изменяет существующее)"
        else:
            verdict,reason="UNCERTAIN","нет новых ids, правит базовые файлы"
    else:
        ratio=found/total_new
        if ratio==0: verdict,reason="KEEP",f"0/{total_new} ids not in base"
        elif ratio>=0.6: verdict,reason="CUT",f"{found}/{total_new} ids уже в базе"
        else: verdict,reason="UNCERTAIN",f"{found}/{total_new} частично"
    results[pr]={"verdict":verdict,"reason":reason,"found_classes":found_c,
                 "found_protos":found_p,"n_added":len(added_files),"europa":len(europa),
                 "path_hit":len(path_hit),"classes":classes,"protos":protos,
                 "added_files":added_files}
json.dump(results,open(".orion-work/ids/classify.json","w"),ensure_ascii=False,indent=0)
from collections import Counter
c=Counter(r["verdict"] for r in results.values())
print("ИТОГ:",dict(c)); print()
for k in ["CUT","UNCERTAIN","KEEP"]:
    row=[(int(p),r) for p,r in results.items() if r["verdict"]==k]
    row.sort()
    print(f"===== {k} ({len(row)}) =====")
    for p,r in row:
        print(f"{p:>4} europa={r.get('europa',0)} add={r.get('n_added',0)} | {r['reason']}")
    print()
