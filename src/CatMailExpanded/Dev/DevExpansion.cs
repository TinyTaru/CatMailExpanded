using System;
using System.Text;
using UnityEngine;
using static CatMailExpanded.Dev.DevConsole;

namespace CatMailExpanded.Dev
{
    /// <summary>Developer commands for the extended pile corridor, the mirror room and the Catazon returns room.</summary>
    internal static class DevExpansion
    {
        static string V(Vector3 v) => FormattableString.Invariant($"({v.x:0.00},{v.y:0.00},{v.z:0.00})");
        static string B(Bounds b) => V(b.min) + ".." + V(b.max);

        static Bounds? BoundsOf(GameObject go)
        {
            Bounds? b = null;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (b == null) b = r.bounds;
                else { var bb = b.Value; bb.Encapsulate(r.bounds); b = bb; }
            }
            return b;
        }

        public static void Register()
        {
            DevConsole.Register("piles", a =>
            {
                var sb = new StringBuilder();
                foreach (var p in Resources.FindObjectsOfTypeAll<Il2Cpp.EntityPile>())
                {
                    if (!p || !p.gameObject.scene.IsValid()) continue;
                    sb.AppendLine($"{PathOf(p.transform)} type={p.PileType} idx={p.CurrentPileSizeIndex} inPile={p.ParcelAmountInCurrentPile} max={p.MaximumPackageOnPileCount} perCust={p.ParcelPerCustomerAmount} outbound={p.OutboundChance} delay={p.DelayBetweenSpawn} pos={V(p.transform.position)}");
                    var per = p.PackagesPerSize;
                    sb.AppendLine("  perSize=" + (per == null ? "-" : string.Join(",", per)));
                    var ld = p.LinkedDoor;
                    sb.AppendLine($"  linkedDoor={(ld == null ? "-" : (ld.RoomDoor ? PathOf(ld.RoomDoor.transform) : "null") + " step=" + ld.PileStep)} initialWall={(p.InitialWall ? PathOf(p.InitialWall.transform) : "-")} invisWall={(p.InvisibleWall ? PathOf(p.InvisibleWall.transform) + " " + (BoundsOf(p.InvisibleWall) is Bounds iw ? B(iw) : "") : "-")}");
                    var sizes = p.PileSizes;
                    if (sizes != null)
                        for (int i = 0; i < sizes.Length; i++)
                        {
                            var s = sizes[i];
                            var go = s.ParentGameObject;
                            var anchors = s.PackageAnchorPoints;
                            Bounds ab = default; bool first = true;
                            if (anchors != null) foreach (var t in anchors) { if (!t) continue; if (first) { ab = new Bounds(t.position, Vector3.zero); first = false; } else ab.Encapsulate(t.position); }
                            sb.AppendLine($"  size[{i}] {(go ? PathOf(go.transform) + (go.activeInHierarchy ? " on" : " off") : "-")} anchors={(anchors == null ? 0 : anchors.Length)} anchorBox={B(ab)} vis={(go && BoundsOf(go) is Bounds vb ? B(vb) : "-")}");
                            if (a.Length > 1 && go)
                                for (int c = 0; c < go.transform.childCount; c++)
                                {
                                    var ch = go.transform.GetChild(c);
                                    sb.AppendLine($"     {ch.name} {(ch.gameObject.activeSelf ? "" : "(off)")} {V(ch.position)} {(BoundsOf(ch.gameObject) is Bounds cb ? B(cb) : "")}");
                                }
                        }
                }
                Out(sb.ToString());
            });
            DevConsole.Register("doors", a =>
            {
                var sb = new StringBuilder();
                foreach (var d in Resources.FindObjectsOfTypeAll<Il2Cpp.StorageRoomDoor>())
                {
                    if (!d || !d.gameObject.scene.IsValid()) continue;
                    sb.AppendLine($"{PathOf(d.transform)} active={d.gameObject.activeInHierarchy} pos={V(d.transform.position)} {(BoundsOf(d.gameObject) is Bounds b ? B(b) : "")}");
                }
                foreach (var u in Resources.FindObjectsOfTypeAll<Il2Cpp.EntityUnlockable>())
                {
                    if (!u || !u.gameObject.scene.IsValid()) continue;
                    sb.AppendLine($"unlockable {PathOf(u.transform)} locked={u.IsLocked} pos={V(u.transform.position)}");
                }
                Out(sb.ToString());
            });
            DevConsole.Register("custinfo", a =>
            {
                var cm = Il2Cpp.CustomerManager.Instance;
                var sb = new StringBuilder();
                sb.AppendLine($"toSatisfy={cm.CurrentTotalCustomersToSatisfy} satisfied={cm.CurrentSatisfiedCustomerCount} depositP={cm.DepositProbability} canSpawn={cm.CanSpawnCustomers} requestable={cm._requestablePackages?.Count}");
                var dep = cm.DepositableParcels;
                sb.AppendLine("depositables=" + (dep == null ? 0 : dep.Length));
                if (dep != null) foreach (var s in dep) sb.AppendLine("  " + (s ? s.name : "null"));
                foreach (var c in UnityEngine.Object.FindObjectsByType<Il2Cpp.CustomerCounter>(FindObjectsSortMode.None))
                    sb.AppendLine($"counter {PathOf(c.transform)} {V(c.transform.position)}");
                foreach (var c in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityCustomer>(FindObjectsSortMode.None))
                    sb.AppendLine($"customer {c.name} state={c._currentState} {V(c.transform.position)}");
                Out(sb.ToString());
            });
            DevConsole.Register("prefabflags", a =>
            {
                // does each boat / customer-deposit store roll its parcels' constraints, or read them from its text?
                var sb = new StringBuilder();
                void Store(string tag, Il2Cpp.EntityInteractableStore st)
                {
                    if (!st) { sb.AppendLine(tag + " null"); return; }
                    var txt = st.PrefilledStorageTextAsset;
                    string body = txt ? txt.text : "";
                    int constraintMentions = 0;
                    foreach (var key in new[] { "\"storageConstraint\":1", "\"storageConstraint\":2", "\"storageConstraint\":4", "\"storageConstraint\":8", "\"storageConstraint\":16" })
                        constraintMentions += body.Split(key).Length - 1;
                    sb.AppendLine($"{tag} {st.name} procedural={st.AreLoadedConstraintsProcedural} stamps={st.ConstraintStamps} text={(txt ? txt.name + " len=" + body.Length : "-")} constrained={constraintMentions} type={st.StorageType}");
                    if (a.Length > 1 && txt) sb.AppendLine("   " + body.Substring(0, Math.Min(body.Length, 600)));
                }
                var cm = Il2Cpp.CustomerManager.Instance;
                if (cm != null && cm.DepositableParcels != null) foreach (var st in cm.DepositableParcels) Store("deposit", st);
                var pd = Il2Cpp.ParcelDeliveryManager.Instance;
                if (pd != null)
                {
                    var per = pd.DelivererStoresPerProgressionLevelsPerPlayerCount;
                    if (per != null)
                        for (int i = 0; i < per.Length; i++)
                        {
                            var lv = per[i]?.DelivererStoreDataPerProgressionLevels;
                            if (lv == null) continue;
                            foreach (var d in lv)
                            {
                                if (d?.DelivererStores == null) continue;
                                foreach (var st in d.DelivererStores) Store($"boat p{i} lvl{d.StartingProgressionLevel}", st);
                            }
                        }
                    if (pd.DelivererStoresFallback?.DelivererStores != null) foreach (var st in pd.DelivererStoresFallback.DelivererStores) Store("boat fallback", st);
                }
                Out(sb.ToString());
            });
            DevConsole.Register("wing", a => Out(Content.PileWing.Describe() + "\n" + Content.MirrorRoom.Describe() + Content.GiftRoom.Describe()
                + $"\npile stage={Content.PileExtension.Stage()} extended={Content.PileExtension.Extended} gifts={Content.ModProgress.GiftRoomUnlocked} mirror={Content.ModProgress.MirrorUnlocked}"));
            DevConsole.Register("pilenext", a =>
            {
                // pilenext [n]: advance the main pile n stages (as if emptied)
                int n = a.Length > 1 ? int.Parse(a[1]) : 1;
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityPile>(FindObjectsSortMode.None))
                {
                    if (p.PileType != Il2Cpp.PileType.MainPile) continue;
                    for (int i = 0; i < n; i++) p.SwitchToNextPileSize();
                    Out($"main pile now at stage {p.CurrentPileSizeIndex}");
                }
            });
            DevConsole.Register("giftnext", a => { Content.GiftRoom.ForceNext(); Out("next customer brings a gift"); });
            DevConsole.Register("makegift", a =>
            {
                // makegift [paper] [ribbon]: turns the parcel nearest the camera into a gift (wrapped when given, -1 = none)
                var cam = Core.Cameras.View ? Core.Cameras.View.transform.position : Vector3.zero;
                Il2Cpp.EntityParcel best = null; float bd = 1e9f;
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                {
                    if (p.Properties == null || Content.ParcelKinds.IsGift(p.Properties.StorageConstraint)) continue;
                    float d = (p.transform.position - cam).sqrMagnitude;
                    if (d < bd) { bd = d; best = p; }
                }
                if (best == null) { Out("no parcel"); return; }
                int paper = a.Length > 1 ? int.Parse(a[1]) : -1, ribbon = a.Length > 2 ? int.Parse(a[2]) : -1;
                Content.GiftRoom.DevMakeGift(best, paper, ribbon);
                var w = Content.GiftRoom.WishOf(best);
                Out($"{best.name} at {best.transform.position} is now a gift wanting {Content.GiftRoom.Papers[w.Paper]}/{Content.GiftRoom.RibbonNames[w.Ribbon]}");
            });
            DevConsole.Register("giftcam", a =>
            {
                // giftcam [i] [yaw] [pitch] [dist]: debug camera on the i-th gift
                float F(int k, float d) => a.Length > k ? float.Parse(a[k], System.Globalization.CultureInfo.InvariantCulture) : d;
                var gifts = System.Linq.Enumerable.ToList(Content.ParcelTracker.Gifts());
                int i = (int)F(1, 0);
                if (i >= gifts.Count) { Out("no gift " + i); return; }
                var p = gifts[i].transform.position;
                DevConsole.Run($"look {p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {(p.y + 0.15f).ToString(System.Globalization.CultureInfo.InvariantCulture)} {p.z.ToString(System.Globalization.CultureInfo.InvariantCulture)} {F(4, 1.1f)} {F(2, 90)} {F(3, 40)}");
                Out($"gift {i} {gifts[i].name} at {p}");
            });
            DevConsole.Register("validate", a =>
            {
                int i = a.Length > 1 ? int.Parse(a[1]) : 0;
                var c = Il2Cpp.WorldManager.Instance.Counters[i];
                Out("request=" + (c.CurrentRequest != null ? c.CurrentRequest.name : "none"));
                c.CHEAT_ValidateRequest();
            });
            DevConsole.Register("giftclick", a => Out("click used=" + Content.GiftRoom.TryClick()));
            DevConsole.Register("bell", a =>
            {
                int counter = a.Length > 1 ? int.Parse(a[1]) : 0;
                var player = Il2Cpp.PlayerManager.Instance?.LocalPlayerEntity;
                Il2Cpp.WorldManager.Instance.Counters[counter].CallNextCustomerBell_OnPrimaryInteracted(player);
                Out("rang counter " + counter);
            });
            DevConsole.Register("storein", a =>
            {
                // storein <box|table|shelf name> <entity name part> [x z]: stores the nearest matching entity at a point
                Il2Cpp.EntityInteractableStore store = null;
                string want = a[1] == "bench" ? "CME_GiftBenchStore" : a[1];
                foreach (var st in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityInteractableStore>(FindObjectsSortMode.None))
                    if (st.name.Contains(want)) { store = st; break; }
                if (store == null) { Out("no store " + want); return; }
                Il2Cpp.Entity ent = null; float bd = 1e9f;
                bool wantReturn = a[2] == "gift";
                var wantKind = Content.ModKind.None;
                if (!wantReturn) Enum.TryParse(a[2], true, out wantKind);
                foreach (var e in UnityEngine.Object.FindObjectsByType<Il2Cpp.Entity>(FindObjectsSortMode.None))
                {
                    if (e.Interactable == null || e.Interactable.Pickable == null) continue;
                    if (wantKind != Content.ModKind.None)
                    {
                        if (e.Properties == null || Content.ParcelKinds.KindOf(e.Properties.StorageConstraint) != wantKind) continue;
                        if ((e.Interactable.Store?.GetRootStore(true)?.name ?? "").Contains(want)) continue;
                    }
                    else if (wantReturn ? !(e.Properties != null && Content.ParcelKinds.IsGift(e.Properties.StorageConstraint)) : !e.name.Contains(a[2], StringComparison.OrdinalIgnoreCase)) continue;
                    if (wantReturn && (e.Interactable.Store?.GetRootStore(true)?.name ?? "").Contains(want)) continue;   // already there
                    float d = (e.transform.position - store.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; ent = e; }
                }
                if (ent == null) { Out("no entity " + a[2]); return; }
                var target = store.transform.position;
                if (a.Length > 4) target = new Vector3(float.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture), target.y + 1f, float.Parse(a[4], System.Globalization.CultureInfo.InvariantCulture));
                // out of any hand or store first; the entity gets its physics back a moment later
                try { var inv = Il2Cpp.PlayerManager.Instance.LocalPlayerEntity.PlayerInventory; if (ent.Interactable.Pickable.CarryingEntity != null) inv.InstantlyDrop(null); } catch { }
                try { var parent = ent.Interactable.Store?.ParentStore; if (parent != null) parent.RemoveEntity(ent); } catch (Exception e) { Out("remove: " + e.Message); }
                var entity = ent; var st2 = store; var tgt = target;
                Core.Scheduler.After(0.4f, () =>
                {
                    var res = st2.CanStoreEntity(entity, tgt, out var cell, out var foot, out var yaw);
                    if (res == Il2Cpp.StoreResult.Success) { st2.StoreEntity(entity, cell, foot, yaw, false); Plugin.Log.Msg($"dev: stored {entity.name} in {st2.name} at cell {cell}"); }
                    else Plugin.Log.Msg($"dev: storing {entity.name}: {res}");
                });
                Out($"storing {ent.name} in {store.name}...");
            });
            DevConsole.Register("storeinfo", a =>
            {
                var sb = new StringBuilder();
                foreach (var st in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityInteractableStore>(FindObjectsSortMode.None))
                {
                    if (!st.name.Contains(a[1])) continue;
                    var all = st.GetAllChildren();
                    var direct = st.GetChildren();
                    sb.AppendLine($"{st.name} pos={V(st.transform.position)} all={(all == null ? -1 : all.Count)} direct={(direct == null ? -1 : direct.Count)} hasChild={st.HasAnyChild} maxH={st.StorageMaximumHeight} max={st.MaximumParcelAmount}");
                    if (all != null) for (int i = 0; i < all.Count; i++) sb.AppendLine($"   {all[i].name} {V(all[i].transform.position)} c={(int)(all[i].Properties?.StorageConstraint ?? 0)}");
                }
                Out(sb.ToString());
            });
            DevConsole.Register("census", a =>
            {
                // where are the mod-kind parcels? pile / boat / customer counter / other stores / loose
                var counts = new System.Collections.Generic.SortedDictionary<string, int>();
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                {
                    var props = p.Properties;
                    if (props == null) continue;
                    var kind = Content.ParcelKinds.KindOf(props.StorageConstraint);
                    string where;
                    if (p.LinkedPileType != Il2Cpp.PileType.None) where = "pile";
                    else
                    {
                        var root = p.Interactable?.Store?.ParentStore?.GetRootStore(true);
                        where = root == null ? "loose" : root.StorageType == Il2Cpp.StorageType.Boat ? "boat" : root.StorageType == Il2Cpp.StorageType.Customer ? "counter" : "stored";
                    }
                    var key = $"{kind,-14} {where}";
                    counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
                }
                var sb = new StringBuilder();
                foreach (var kv in counts) sb.AppendLine($"{kv.Key}: {kv.Value}");
                Out(sb.ToString());
            });
            DevConsole.Register("deliverer", a =>
            {
                var pd = Il2Cpp.ParcelDeliveryManager.Instance;
                var sb = new StringBuilder();
                sb.AppendLine($"atDock={pd._isAtDock} store={(pd.CurrentDelivererStore ? pd.CurrentDelivererStore.name : "-")} fallback={(pd.DelivererStoresFallback != null)}");
                var per = pd.DelivererStoresPerProgressionLevelsPerPlayerCount;
                sb.AppendLine("perLevel=" + (per == null ? 0 : per.Length));
                Out(sb.ToString());
            });
        }
    }
}
