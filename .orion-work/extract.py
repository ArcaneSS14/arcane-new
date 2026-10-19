import subprocess, re, sys, os, json

def git(*a):
    return subprocess.run(["git",*a], capture_output=True, text=True, errors="replace").stdout

def sh(*a):
    return subprocess.run(a, capture_output=True, text=True, errors="replace").stdout

prs = [int(x) for x in open(".orion-work/pr_sorted.txt").read().split()]
out = {}
for pr in prs:
    head = f"refs/orion/pr/{pr}"
    mb = git("merge-base", "refs/orion/master", head).strip()
    if not mb:
        out[pr]={"err":"no-merge-base"}; continue
    diff = git("diff", "--unified=0", mb, head)
    files=[]; added_files=[]; mod_files=[]
    # added lines
    comps=set(); protos=set(); locale=set(); paths=set()
    cur=None
    for line in diff.splitlines():
        if line.startswith("diff --git"):
            m=re.search(r" b/(\S+)", line); cur=m.group(1) if m else None
            if cur: files.append(cur)
        elif line.startswith("+++ b/"):
            m=re.search(r"b/(\S+)", line); p=m.group(1) if m else None
            if p: added_files.append(p)
        # new C# class names
        m=re.search(r"^\+\s*(?:public\s+)?(?:sealed\s+)?(?:partial\s+)?(?:abstract\s+)?class\s+(\w+)", line)
        if m: comps.add(m.group(1))
        m=re.search(r"^\+\s*\[RegisterComponent", line)
        # prototype ids (yml) - added 'id:' lines
        m=re.search(r"^\+\s*id:\s*([A-Za-z0-9_\-]+)", line)
        if m: protos.add(m.group(1))
    # unique added (new) files: those in added_files
    added_set=set(added_files)
    mod_set=set(f for f in files if f not in added_set)
    out[pr]={
        "n_files":len(files),
        "n_added":len(added_set),
        "classes":sorted(comps),
        "protos":sorted(protos),
        "added_files":sorted(added_set),
        "all_files":sorted(set(files)),
    }
json.dump(out, open(".orion-work/ids/extract.json","w"), ensure_ascii=False, indent=0)
print("extracted:", len(out))
# quick stats
withc=sum(1 for v in out.values() if v.get("classes"))
withp=sum(1 for v in out.values() if v.get("protos"))
print("с классами C#:", withc, "| с прототипами:", withp)
