# Dump scene hierarchy (path, world pos, components, MB typetree for selected scripts) to JSON.
import UnityPy, sys, os, json, math
D=r"C:\Program Files (x86)\Steam\steamapps\common\CatMailCo\CatMailCo_Data"
AA=os.path.join(D,"StreamingAssets","aa","StandaloneWindows64")
files=[os.path.join(AA,f) for f in os.listdir(AA) if any(h in f for h in ("764d8770","6e7454c8","bbbf90de","02ec9b26","500a080d","unitybuiltin"))]
out=sys.argv[1]
env=UnityPy.load(*files)
objs={}
for o in env.objects:
    objs[(o.assets_file.name,o.path_id)]=o
tr={}  # transform key -> data
gos={}
def key(o): return (o.assets_file.name,o.path_id)
def qmul(a,b):
    ax,ay,az,aw=a; bx,by,bz,bw=b
    return (aw*bx+ax*bw+ay*bz-az*by, aw*by-ax*bz+ay*bw+az*bx, aw*bz+ax*by-ay*bx+az*bw, aw*bw-ax*bx-ay*by-az*bz)
def qrot(q,v):
    x,y,z,w=q; vx,vy,vz=v
    # v' = q v q*
    ix=w*vx+y*vz-z*vy; iy=w*vy+z*vx-x*vz; iz=w*vz+x*vy-y*vx; iw=-x*vx-y*vy-z*vz
    return (ix*w+iw*-x+iy*-z-iz*-y, iy*w+iw*-y+iz*-x-ix*-z, iz*w+iw*-z+ix*-y-iy*-x)
for k,o in objs.items():
    if o.type.name in ("Transform","RectTransform"):
        try: t=o.read()
        except Exception: continue
        p=t.m_LocalPosition; r=t.m_LocalRotation; s=t.m_LocalScale
        par=None
        try:
            if t.m_Father.path_id!=0: par=key(t.m_Father.deref())
        except Exception: pass
        gk=None
        try: gk=key(t.m_GameObject.deref())
        except Exception: pass
        tr[k]={"lp":(p.x,p.y,p.z),"lr":(r.x,r.y,r.z,r.w),"ls":(s.x,s.y,s.z),"par":par,"go":gk}
want=set(sys.argv[2].split(",")) if len(sys.argv)>2 else set()
for k,o in objs.items():
    if o.type.name!="GameObject": continue
    try: go=o.read()
    except Exception: continue
    comps=[];tk=None;mbs={}
    for c in go.m_Components:
        ptr=getattr(c,'component',None) or (c[1] if isinstance(c,(tuple,list)) else c)
        try:
            co=ptr.deref(); tn=co.type.name
            if tn in ("Transform","RectTransform"): tk=key(co)
            if tn=="MonoBehaviour":
                mb=co.read()
                try: s=mb.m_Script.deref().read().m_ClassName
                except Exception: s="?"
                tn="MB:"+s
                if s in want or "*" in want:
                    try:
                        tt=co.read_typetree()
                        mbs[s]=tt
                    except Exception as e: mbs[s]="ERR "+str(e)
            comps.append(tn+"#"+str(co.path_id))
        except Exception as e:
            comps.append("ERR")
    gos[k]={"name":go.m_Name,"active":go.m_IsActive,"comps":comps,"t":tk,"mb":mbs}
# world
cache={}
def world(tk):
    if tk in cache: return cache[tk]
    t=tr.get(tk)
    if t is None: return ((0,0,0),(0,0,0,1),(1,1,1))
    if t["par"] is None or t["par"] not in tr:
        r=(t["lp"],t["lr"],t["ls"])
    else:
        pp,pr,ps=world(t["par"])
        lp=t["lp"]; sp=(lp[0]*ps[0],lp[1]*ps[1],lp[2]*ps[2])
        rp=qrot(pr,sp)
        r=((pp[0]+rp[0],pp[1]+rp[1],pp[2]+rp[2]),qmul(pr,t["lr"]),(ps[0]*t["ls"][0],ps[1]*t["ls"][1],ps[2]*t["ls"][2]))
    cache[tk]=r; return r
sys.setrecursionlimit(10000)
def path(tk):
    names=[]
    while tk in tr:
        g=tr[tk]["go"]; names.append(gos[g]["name"] if g in gos else "?")
        tk=tr[tk]["par"]
    return "/".join(reversed(names))
res=[]
def ser(x):
    try: json.dumps(x); return x
    except Exception: return str(x)
for k,g in gos.items():
    tk=g["t"]
    w=world(tk) if tk else None
    res.append({"file":k[0],"pid":k[1],"path":path(tk) if tk else g["name"],"active":g["active"],"comps":g["comps"],
                "wpos":[round(v,3) for v in w[0]] if w else None,"lpos":[round(v,3) for v in tr[tk]["lp"]] if tk in tr else None,"mb":{a:ser(b) for a,b in g["mb"].items()}})
json.dump(res,open(out,"w"),default=str)
print(len(res))
