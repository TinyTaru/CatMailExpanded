# Dump scene hierarchy with components (MonoBehaviour script names) and world-ish positions.
import UnityPy, sys, os, json
D=r"C:\Program Files (x86)\Steam\steamapps\common\CatMailCo\CatMailCo_Data"
AA=os.path.join(D,"StreamingAssets","aa","StandaloneWindows64")
files=[os.path.join(AA,f) for f in os.listdir(AA) if any(h in f for h in ("764d8770","6e7454c8","bbbf90de","02ec9b26","500a080d","unitybuiltin"))]
out=sys.argv[1]
env=UnityPy.load(*files)
# monoscript lookup
scripts={}
for o in env.objects:
    if o.type.name=="MonoScript":
        try:
            d=o.read()
            scripts[(o.assets_file.name,o.path_id)]=d.m_ClassName
        except Exception as e: pass
print("scripts",len(scripts))
def script_name(mb_reader):
    try:
        d=mb_reader.read(check_read=False) if False else None
    except Exception: pass
for sf_name, sf in env.files.items():
    pass
res=[]
for o in env.objects:
    if o.type.name!="GameObject": continue
    af=o.assets_file
    try: go=o.read()
    except Exception as e: continue
    comps=[]
    for c in go.m_Components:
        ptr=c.component if hasattr(c,'component') else c[1] if isinstance(c,tuple) else c
        try:
            co=ptr.deref()
            tn=co.type.name
            if tn=="MonoBehaviour":
                mb=co.read(check_read=False) if 'check_read' in co.read.__code__.co_varnames else co.read()
                try:
                    s=mb.m_Script.deref().read().m_ClassName
                except Exception as e: s="?"
                tn="MB:"+s
            comps.append((tn,co.path_id))
        except Exception as e:
            comps.append(("ERR",0))
    res.append({"file":af.name,"pid":o.path_id,"name":go.m_Name,"active":go.m_IsActive,"comps":comps})
json.dump(res,open(out,"w"))
print(len(res))
