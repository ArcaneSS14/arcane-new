import subprocess, re, json
# Base class names (C#)
r=subprocess.run(["git","grep","-h","-E",r"(class|struct|record)\s+([A-Za-z0-9_]+)","--","Content.Server","Content.Shared","Content.Client"],capture_output=True,text=True,errors="replace")
classes=set()
for line in r.stdout.splitlines():
    m=re.search(r"(?:class|struct|record)\s+([A-Za-z0-9_]+)",line)
    if m: classes.add(m.group(1))
# Base prototype ids (yml)
r2=subprocess.run(["git","grep","-h","-E",r"^\s*id:\s*([A-Za-z0-9_\-]+)","--","Resources/Prototypes"],capture_output=True,text=True,errors="replace")
protos=set()
for line in r2.stdout.splitlines():
    m=re.search(r"id:\s*([A-Za-z0-9_\-]+)",line)
    if m: protos.add(m.group(1))
# Base file paths (for path-collision signal)
r3=subprocess.run(["git","ls-files","Content.Server","Content.Shared","Content.Client","Resources/Prototypes","Resources/Textures","Resources/Locale"],capture_output=True,text=True,errors="replace")
paths=set(r3.stdout.splitlines())
json.dump({"classes":sorted(classes),"protos":sorted(protos),"paths":sorted(paths)},open(".orion-work/ids/base_index.json","w"))
print("base classes:",len(classes),"| base protos:",len(protos),"| base paths:",len(paths))
