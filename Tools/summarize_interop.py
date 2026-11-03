import os,re,sys
src,dst=sys.argv[1],sys.argv[2]
os.makedirs(dst,exist_ok=True)
for root,_,files in os.walk(src):
    for fn in files:
        if not fn.endswith('.cs'): continue
        t=open(os.path.join(root,fn),encoding='utf-8').read()
        out=[]
        for m in re.finditer(r'^(public|internal)[^\n]*(class|struct|enum|interface)\s+[^\n]+',t,re.M): out.append(m.group(0))
        if ' enum ' in t:
            out+= [l.strip() for l in t.splitlines() if re.match(r'^\s+[A-Za-z_0-9]+( = [-0-9x]+)?,?$',l)]
        out+=['F '+x for x in re.findall(r'NativeFieldInfoPtr_(\w+);',t)]
        out+=['M '+x for x in re.findall(r'NativeMethodInfoPtr_(\w+);',t)]
        # public property types
        for m in re.finditer(r'^\tpublic (?:unsafe )?(?:static )?([\w<>\[\]., ]+) (\w+)\n\t\{',t,re.M): out.append('P %s %s'%(m.group(1),m.group(2)))
        rel=os.path.relpath(os.path.join(root,fn),src).replace(os.sep,'__')
        open(os.path.join(dst,rel[:-3]+'.txt'),'w',encoding='utf-8').write('\n'.join(out))
