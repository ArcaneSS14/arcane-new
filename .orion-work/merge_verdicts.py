import json
V={
# batch 1
17:"UNCERTAIN",29:"KEEP",57:"UNCERTAIN",85:"CUT",162:"KEEP",210:"KEEP",239:"KEEP",302:"KEEP",350:"KEEP",356:"KEEP",372:"KEEP",381:"KEEP",
23:"KEEP",31:"KEEP",61:"KEEP",93:"CUT",182:"CUT",211:"KEEP",248:"CUT",311:"CUT",351:"KEEP",359:"KEEP",373:"CUT",382:"KEEP",
25:"UNCERTAIN",38:"KEEP",63:"KEEP",143:"KEEP",183:"KEEP",216:"KEEP",283:"KEEP",341:"CUT",353:"KEEP",363:"KEEP",375:"KEEP",429:"UNCERTAIN",
26:"CUT",41:"CUT",76:"KEEP",157:"CUT",207:"CUT",232:"KEEP",298:"CUT",349:"KEEP",354:"KEEP",370:"KEEP",376:"KEEP",466:"CUT",
# U1
14:"CUT",27:"KEEP",39:"CUT",55:"KEEP",74:"KEEP",99:"CUT",113:"CUT",138:"CUT",153:"CUT",178:"CUT",188:"CUT",233:"KEEP",259:"CUT",275:"KEEP",284:"UNCERTAIN",312:"UNCERTAIN",325:"CUT",343:"CUT",366:"CUT",402:"KEEP",420:"KEEP",438:"KEEP",447:"CUT",
# U2
19:"CUT",34:"CUT",40:"KEEP",60:"CUT",79:"CUT",100:"CUT",116:"CUT",144:"CUT",160:"CUT",181:"KEEP",208:"KEEP",245:"KEEP",265:"CUT",276:"CUT",287:"UNCERTAIN",313:"CUT",327:"CUT",345:"CUT",367:"KEEP",406:"CUT",427:"CUT",439:"UNCERTAIN",448:"CUT",
# U3
20:"CUT",35:"UNCERTAIN",42:"CUT",64:"KEEP",87:"CUT",106:"CUT",117:"KEEP",148:"CUT",166:"CUT",186:"KEEP",220:"KEEP",256:"KEEP",267:"CUT",279:"CUT",305:"CUT",322:"CUT",328:"KEEP",357:"CUT",380:"CUT",412:"CUT",434:"KEEP",444:"CUT",450:"KEEP",
# U4
21:"CUT",37:"CUT",52:"CUT",66:"UNCERTAIN",97:"CUT",110:"KEEP",118:"CUT",152:"CUT",177:"CUT",187:"KEEP",227:"CUT",258:"KEEP",271:"CUT",282:"CUT",307:"CUT",324:"CUT",331:"UNCERTAIN",365:"CUT",383:"KEEP",417:"CUT",436:"CUT",445:"KEEP",458:"KEEP",
}
# resolve remaining UNCERTAIN via empty-diff / name-collision evidence
overrides={17:("KEEP","Orion-контент (мудрости/события), в базе нет"),
57:("KEEP","обновление карты InteQ_Sizo (_Europa)"),
25:("KEEP","фикс спрайтов modsuit (_Europa)"),
429:("KEEP","аудио reptilian_hiss (бинарь)"),
284:("UNCERTAIN","пустой дифф (TG Banners)"),
312:("UNCERTAIN","пустой дифф (Lobby Server Info)"),
287:("UNCERTAIN","пустой дифф"),
439:("UNCERTAIN","пустой дифф (start)"),
35:("UNCERTAIN","пустой дифф (+4 hair)"),
66:("UNCERTAIN","пустой дифф"),
331:("UNCERTAIN","пустой дифф (fixsecretdoor)"),
}
R=json.load(open(".orion-work/final_verdicts.json")) if __import__("os").path.exists(".orion-work/final_verdicts.json") else {}
out={}
for p,v in V.items():
    out[p]={"verdict":v,"reason":""}
for p,(v,r) in overrides.items():
    out[p]={"verdict":v,"reason":r}
# verify full coverage
allpr=set(int(x) for x in open(".orion-work/pr_sorted.txt").read().split())
have=set(out.keys())
print("покрытие:",len(have),"/",len(allpr),"| недостаёт:",sorted(allpr-have))
from collections import Counter
c=Counter(x["verdict"] for x in out.values())
print("ИТОГ:",dict(c))
json.dump(out,open(".orion-work/FINAL.json","w"),ensure_ascii=False,indent=0)
print("CUT:",sorted(p for p,x in out.items() if x["verdict"]=="CUT"))
print("UNCERTAIN:",sorted(p for p,x in out.items() if x["verdict"]=="UNCERTAIN"))
print("KEEP count:",c["KEEP"])
