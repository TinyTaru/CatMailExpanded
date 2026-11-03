import UnityPy, sys, os
D=r"C:\Program Files (x86)\Steam\steamapps\common\CatMailCo\CatMailCo_Data"
AA=os.path.join(D,"StreamingAssets","aa","StandaloneWindows64")
for f in sorted(os.listdir(AA)):
    p=os.path.join(AA,f)
    print(f, os.path.getsize(p))
    if 'scenes' in f:
        env=UnityPy.load(p)
        for name,fl in env.files.items():
            print("  ",name)
            if hasattr(fl,'files'):
                for n2 in fl.files: print("     ",n2)
