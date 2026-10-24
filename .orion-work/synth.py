import json, re
from collections import Counter
cls=json.load(open(".orion-work/ids/classify.json"))
modf=json.load(open(".orion-work/ids/modfiles.json"))

def touches_only(path_pats, files):
    return files and all(any(re.search(p,f["file"]) for p in path_pats) for f in files)

final={}
for pr,v in cls.items():
    p=str(pr); files=modf.get(p,[])
    nfiles=len(files)
    europa_files=[f for f in files if "_Europa" in f["file"] or "_europa" in f["file"]]
    base_files=[f for f in files if "_Europa" not in f["file"] and "_europa" not in f["file"]]
    total_add=sum(f["add"] for f in files)
    ver=v["verdict"]
    if ver in ("CUT","KEEP"):
        final[p]=v["verdict"]; continue
    # UNCERTAIN: mod-only
    # categorize by what base dirs are touched
    dirs=Counter()
    for f in base_files:
        m=re.match(r"(Content\.(?:Server|Shared|Client|Goobstation\.\w+|Trauma\.\w+|Arcane\.\w+|Medical\.\w+|Lavaland\.\w+|Factory\.\w+|SIS\.\w+)|Resources/(?:Prototypes|Locale|Textures|Audio))", f["file"])
        dirs[m.group(1) if m else f["file"].split("/")[0]]+=1
    # locale-only?
    if base_files and all(f["file"].startswith("Resources/Locale") for f in base_files):
        final[p]="KEEP"; v["cat"]="локализация"
    elif base_files and all(f["file"].startswith("Resources/Textures") for f in base_files):
        final[p]="KEEP"; v["cat"]="спрайты"
    else:
        # touches real gameplay code/protos -> still needed (base lacks the change)
        final[p]="KEEP"; v["cat"]="правка базы"
    v["base_dirs"]=dict(dirs)
    v["nfiles"]=nfiles; v["total_add"]=total_add
    v["n_europa"]=len(europa_files); v["n_base"]=len(base_files)
json.dump(cls,open(".orion-work/ids/classify.json","w"),ensure_ascii=False,indent=0)

c=Counter(final.values()); print("ИТОГ:",dict(c)); print()
# Among KEEP, split europa-flavor vs base-tweak
keep_europa=[];keep_base=[];keep_other=[]
for p,ver in final.items():
    if ver!="KEEP": continue
    v=cls[p]; cat=v.get("cat","")
    if v.get("n_europa",0)>0 and v.get("n_base",0)==0: keep_europa.append(int(p))
    elif cat in ("правка базы","локализация","спрайты"): keep_base.append((int(p),cat))
    else: keep_other.append(int(p))
print(f"KEEP Orion-flavour (_Europa, чисто новое): {len(keep_europa)} -> {sorted(keep_europa)}")
print(f"KEEP правки базы/локализация/спрайты: {len(keep_base)}")
for p,cat in sorted(keep_base): print(f"   {p:>4} [{cat}] {cls[str(p)]['reason']}")
print(f"KEEP прочее/смешанное: {len(keep_other)} -> {sorted(keep_other)}")
print()
cut=sorted([int(p) for p,v in final.items() if v=="CUT"])
print(f"CUT (уже в базе): {len(cut)} -> {cut}")
for p in cut: print(f"   {p:>4} {cls[str(p)]['reason']}")
