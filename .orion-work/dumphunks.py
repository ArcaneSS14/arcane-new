import subprocess, re, json, os
def git(*a): return subprocess.run(["git",*a],capture_output=True,text=True,errors="replace").stdout
prs=[int(x) for x in open(".orion-work/pr_sorted.txt").read().split()]
cls=json.load(open(".orion-work/ids/classify.json"))
os.makedirs(".orion-work/hunks",exist_ok=True)
titles={}
for pr in prs:
    t=git("log","-1","--format=%s",f"refs/orion/pr/{pr}").strip()
    titles[pr]=t
json.dump(titles,open(".orion-work/hunks/titles.json","w"),ensure_ascii=False,indent=0)
for pr in prs:
    head=f"refs/orion/pr/{pr}"
    mb=git("merge-base","refs/orion/master",head).strip()
    if not mb: continue
    diff=git("diff","--unified=2",mb,head)
    # keep only code/proto hunks, cap size
    lines=diff.splitlines()
    out=[];keep=False;size=0
    for ln in lines:
        if ln.startswith("diff --git"):
            fn=ln.split(" b/")[-1]
            keep= any(fn.endswith(ext) for ext in (".cs",".yml",".yaml")) and "_europa" not in fn.lower()
            if keep: out.append("\n### "+fn)
        if keep and (ln.startswith("+") or ln.startswith("-") or ln.startswith("@")):
            if ln.startswith("+++") or ln.startswith("---"): continue
            out.append(ln); size+=1
        if size>400: keep=False
    txt="\n".join(out)
    open(f".orion-work/hunks/pr{pr}.txt","w").write((t if False else titles[pr])+"\n"+txt)
print("dumped",len(prs),"hunks; avg size", round(sum(os.path.getsize(f'.orion-work/hunks/pr{p}.txt') for p in prs)/len(prs)))
