import json
# Subagent semantic verdicts (batch 1-4) + identifier CUT + resolved UNCERTAIN
sub = {
17:"UNCERTAIN",29:"KEEP",57:"UNCERTAIN",85:"CUT",162:"KEEP",210:"KEEP",239:"KEEP",302:"KEEP",350:"KEEP",356:"KEEP",372:"KEEP",381:"KEEP",
23:"KEEP",31:"KEEP",61:"KEEP",93:"CUT",182:"CUT",211:"KEEP",248:"CUT",311:"CUT",351:"KEEP",359:"KEEP",373:"CUT",382:"KEEP",
25:"UNCERTAIN",38:"KEEP",63:"KEEP",143:"KEEP",183:"KEEP",216:"KEEP",283:"KEEP",341:"CUT",353:"KEEP",363:"KEEP",375:"KEEP",429:"UNCERTAIN",
26:"CUT",41:"CUT",76:"KEEP",157:"CUT",207:"CUT",232:"KEEP",298:"CUT",349:"KEEP",354:"KEEP",370:"KEEP",376:"KEEP",466:"CUT",
}
reasons = {
17:"_Europa-контент (мудрости/события), пустой дифф",
57:"обновление карты InteQ_Sizo (Maps/_Europa) — оріоновская карта",
25:"фикс спрайтов.modsuit — пустой дифф",
429:"аудио-фикс reptilian_hiss.ogg (бинарь)",
85:"чистка Goob-voice чата",
93:"YAML-фикс Orion-постеров/доступов UEG",
182:"возврат к апстриму (RND-Server, медпак)",
248:"косметический спрайт bleed-алерта",
311:"удаляет ChangelogOrion.yml (нет в базе)",
373:"Orion-баланс патч (TC/урон/броня)",
341:"Orion-баланс брони шляп",
26:"миграция DresserCaptainFilled→CaptainDrobe (ломает базу)",
41:"Floodlight уже наследует HandheldLight",
157:"PDA-время уже есть в базе",
207:"Trauma-языки уже аддитивные (Knowledge)",
298:"фикс UI рекрутинга без самой системы",
466:"fan airtight для широких шлюзов (нет в базе)",
}
# base name-collision overrides (KEEP confirmed)
for p in [350,210,239,356]: sub[p]="KEEP"
cls=json.load(open(".orion-work/ids/classify.json"))
# identifier CUT stays
for p,v in cls.items():
    if v["verdict"]=="CUT" and int(p) not in sub:
        sub[int(p)]="CUT"
        reasons[int(p)]=v["reason"]
# resolve remaining UNCERTAIN by empty-diff heuristics -> KEEP (real content, base lacks)
for p in [17,57,25,429]:
    if sub[p]=="UNCERTAIN":
        sub[p]="KEEP"; reasons.setdefault(p,"орионовский контент (карта/спрайт/аудио), в базе нет")
final={}
for p,v in sub.items():
    final[p]={"verdict":v,"reason":reasons.get(p,"")}
json.dump(final,open(".orion-work/final_verdicts.json","w"),ensure_ascii=False,indent=0)
from collections import Counter
c=Counter(v["verdict"] for v in final.values())
print("Всего проверено:",len(final),dict(c))
print("CUT:",sorted(p for p,v in final.items() if v["verdict"]=="CUT"))
print("UNCERTAIN остались:",sorted(p for p,v in final.items() if v["verdict"]=="UNCERTAIN"))
print("KEEP:",sorted(p for p,v in final.items() if v["verdict"]=="KEEP"))
