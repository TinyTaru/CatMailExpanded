# Print transform (local pos/rot/scale, world yaw) and mesh bounds for GameObjects by path_id in main scene bundle.
import UnityPy, sys, os, math
D=r"C:\Program Files (x86)\Steam\steamapps\common\CatMailCo\CatMailCo_Data"
AA=os.path.join(D,"StreamingAssets","aa","StandaloneWindows64")
p=[os.path.join(AA,f) for f in os.listdir(AA) if "scenes_scenes_all_6e7454c8" in f][0]
env=UnityPy.load(p)
ids=set(int(x) for x in sys.argv[1].split(","))
objs={o.path_id:o for o in env.objects if o.assets_file.name.startswith("CAB-3b4f") or True}
def yaw(q):
    x,y,z,w=q.x,q.y,q.z,q.w
    return math.degrees(math.atan2(2*(w*y+x*z),1-2*(y*y+x*x)))
for o in env.objects:
    if o.type.name=="GameObject" and o.path_id in ids:
        go=o.read()
        print("==",o.path_id,go.m_Name)
        stack=[(go,0)]
        while stack:
            g,dep=stack.pop()
            for c in g.m_Components:
                ptr=getattr(c,'component',None) or c
                co=ptr.deref()
                if co.type.name=="Transform":
                    t=co.read(); r=t.m_LocalRotation
                    print("  "*dep+" ",g.m_Name,"lp",[round(v,3) for v in (t.m_LocalPosition.x,t.m_LocalPosition.y,t.m_LocalPosition.z)],"lrot",[round(v,3) for v in (r.x,r.y,r.z,r.w)],"yaw",round(yaw(r),1),"ls",[round(v,2) for v in (t.m_LocalScale.x,t.m_LocalScale.y,t.m_LocalScale.z)])
                    if dep<3:
                        for ch in t.m_Children:
                            stack.append((ch.deref().read().m_GameObject.deref().read(),dep+1))
                elif co.type.name=="MeshFilter":
                    try:
                        m=co.read().m_Mesh.deref().read()
                        b=m.m_LocalAABB
                        print("  "*dep+"   mesh",m.m_Name,"center",[round(v,2) for v in (b.m_Center.x,b.m_Center.y,b.m_Center.z)],"ext",[round(v,2) for v in (b.m_Extent.x,b.m_Extent.y,b.m_Extent.z)])
                    except Exception as e: print("   mesh err",e)
                elif co.type.name=="BoxCollider":
                    b=co.read(); print("  "*dep+"   box c",[round(v,2) for v in (b.m_Center.x,b.m_Center.y,b.m_Center.z)],"s",[round(v,2) for v in (b.m_Size.x,b.m_Size.y,b.m_Size.z)])
