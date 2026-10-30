import re,sys
f=sys.argv[1]; s=open(f).read()
for i,m in enumerate(re.finditer(r'^<<<<<<< ours\n(.*?)^\|\|\|\|\|\|\| orion-base\n(.*?)^=======\n(.*?)^>>>>>>> orion-\d+\n',s,re.S|re.M)):
    ln=s[:m.start()].count('\n')+1
    def strip(x): return '\n'.join(l for l in x.split('\n') if 'SPDX' not in l and l.strip()!='//')
    print(f'##### conflict {i+1} at line {ln}')
    print('--- ours'); print(strip(m.group(1)))
    print('--- base'); print(strip(m.group(2)))
    print('--- theirs'); print(strip(m.group(3)))
