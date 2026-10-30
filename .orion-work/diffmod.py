import subprocess, re, json
def git(*a): return subprocess.run(["git",*a],capture_output=True,text=True,errors="replace").stdout
prs=[int(x) for x in open(".orion-work/pr_sorted.txt").read().split()]
out={}
for pr in prs:
    head=f"refs/orion/pr/{pr}"
    mb=git("merge-base","refs/orion/master",head).strip()
    if not mb: continue
    # numstat: added deleted file
    ns=git("diff","--numstat",mb,head)
    files=[]
    for line in ns.splitlines():
        parts=line.split("\t")
        if len(parts)==3:
            a,d,f=parts
            files.append({"file":f,"add":0 if a=="-" else int(a),"del":0 if d=="-" else int(d)})
    out[pr]=files
json.dump(out,open(".orion-work/ids/modfiles.json","w"),ensure_ascii=False)
print("done",len(out))
