using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatMailExpanded.Dev
{
    /// <summary>
    /// Development helper: polls UserData/CME/cmd.txt for commands so the game can be
    /// driven and inspected from outside (scene dumps, screenshots, debug camera).
    /// Only active when the "DevMode" preference is on.
    /// </summary>
    internal static class DevConsole
    {
        static string _dir;
        static string _cmdFile;
        static string _outFile;
        static Camera _debugCam;
        public static Camera ActiveDebugCamera => _debugCam;
        static float _nextPoll;
        static readonly Dictionary<string, Action<string[]>> _commands = new();

        public static void Init()
        {
            _dir = System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "CME");
            Directory.CreateDirectory(_dir);
            _cmdFile = System.IO.Path.Combine(_dir, "cmd.txt");
            _outFile = System.IO.Path.Combine(_dir, "out.txt");
            Register("dump", a => DumpScene(a.Length > 1 ? a[1] : null, a.Length > 2 ? int.Parse(a[2]) : 99));
            Register("shot", a => Shot(a.Length > 1 ? a[1] : "shot"));
            Register("cam", Cam);
            Register("find", a => Find(string.Join(" ", a, 1, a.Length - 1)));
            Register("echo", a => Out(string.Join(" ", a, 1, a.Length - 1)));
            Register("ortho", a =>
            {
                // ortho cx cz size [y]: top-down orthographic view
                Cam(new[] { "cam", a[1], a.Length > 4 ? a[4] : "40", a[2], "0", "90", "60" });
                _debugCam.orthographic = true;
                _debugCam.orthographicSize = float.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture);
                _debugCam.farClipPlane = 200;
            });
            Register("orthocam", a =>
            {
                // orthocam x y z yaw pitch size
                float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
                Cam(new[] { "cam", a[1], a[2], a[3], a[4], a[5], "60" });
                _debugCam.orthographic = true;
                _debugCam.orthographicSize = F(6);
                _debugCam.nearClipPlane = 0.01f;
            });
            Register("persp", a => { if (_debugCam) _debugCam.orthographic = false; });
            Register("look", a =>
            {
                // look x y z dist yaw pitch [fov]: orbit camera aimed at a target point
                float F(int i, float d) => a.Length > i ? float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture) : d;
                var target = new Vector3(F(1, 0), F(2, 1), F(3, 0));
                var rot = Quaternion.Euler(F(6, 20), F(5, 0), 0);
                var pos = target - rot * Vector3.forward * F(4, 3);
                Cam(new[] { "cam", "0", "0", "0", "0", "0", F(7, 60).ToString(System.Globalization.CultureInfo.InvariantCulture) });
                _debugCam.orthographic = false;
                _debugCam.transform.position = pos;
                _debugCam.transform.rotation = rot;
                _debugCam.nearClipPlane = 0.05f;
            });
            Register("bounds", a => Bounds(string.Join(" ", a, 1, a.Length - 1)));
            Register("prop", a =>
            {
                // prop mesh tex x y z yaw
                float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
                var go = Core.ModAssets.Prop(a[1], a[2], null, new Vector3(F(3), F(4), F(5)), Quaternion.Euler(0, F(6), 0));
                Out("spawned " + go.name);
            });
            Register("shader", a => Out("Shader.Find " + a[1] + " -> " + (Shader.Find(a[1]) ? "ok" : "null")));
            Register("shaders", a =>
            {
                var sb = new StringBuilder();
                foreach (var s in Resources.FindObjectsOfTypeAll<Shader>())
                    if (a.Length < 2 || s.name.Contains(a[1], StringComparison.OrdinalIgnoreCase)) sb.AppendLine(s.name);
                Out(sb.ToString());
            });
            Register("inspect", a => Inspect(string.Join(" ", a, 1, a.Length - 1)));
            Register("stampmenu", a =>
            {
                // stampmenu: rebuild every sticker-menu category and list the stickers it shows
                var sb = new StringBuilder();
                foreach (var cat in Resources.FindObjectsOfTypeAll<Il2Cpp.StampSelectionCategoryInterface>())
                {
                    if (!cat.gameObject.scene.IsValid()) continue;
                    try { cat.Populate(); } catch (Exception e) { sb.AppendLine($"{cat.name} populate threw {e.Message}"); }
                    var names = new List<string>();
                    foreach (var item in cat.GetComponentsInChildren<Il2Cpp.StampSelectableItemInterface>(true))
                        names.Add(item.StampData ? item.StampData.name : "null");
                    sb.AppendLine($"{cat.name} [{cat.LinkedStampCategory}] {names.Count}: {string.Join(", ", names)}");
                }
                Out(sb.ToString());
            });
            Register("spawneropts", a =>
            {
                var sb = new StringBuilder();
                foreach (var si in Resources.FindObjectsOfTypeAll<Il2Cpp.SpawnerInterface>())
                {
                    if (!si.gameObject.scene.IsValid()) continue;
                    si.InitializeConstraintsDropdown();
                    var dd = si.ConstraintDropdown;
                    foreach (var o in dd.options) sb.Append(o.text).Append(" | ");
                    sb.AppendLine();
                    if (a.Length > 1) { dd.value = int.Parse(a[1]); dd.RefreshShownValue(); sb.AppendLine("selected " + dd.captionText.text); }
                }
                Out(sb.ToString());
            });
            Register("stickon", a =>
            {
                // stickon <strawberry|banana|restless>: applies our sticker to every parcel of that kind, like the stamp tool
                var kind = (Content.ModKind)Enum.Parse(typeof(Content.ModKind), a[1], true);
                if (!Content.ModStamps.TryGet(Content.ModStamps.NameFor(kind), out var data)) { Out("no sticker data"); return; }
                int n = 0;
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                {
                    if (Content.ParcelKinds.KindOf(p.Properties.StorageConstraint) != kind) continue;
                    var st = p.GetComponentInChildren<Il2Cpp.EntityInteractableStamp>(true);
                    if (st == null) continue;
                    var slot = p.transform.Find("Tech/ConstraintStampPositions/1") ?? p.transform;
                    var pos = slot.position; var rot = slot.rotation;
                    st.AddStamp(data, st._stampNextIdentifier, ref pos, ref rot, true);
                    n++;
                }
                Out($"stuck {n}");
            });
            Register("blockers", a =>
            {
                var sb = new StringBuilder();
                var root = Core.World.Find("LightBlocker", "PostOffice_Environment");
                var parent = root ? root.transform.parent : null;
                foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (!PathOf(r.transform).Contains("LightBlocker")) continue;
                    var b = r.bounds;
                    sb.AppendLine(FormattableString.Invariant($"{PathOf(r.transform)} mode={r.shadowCastingMode} rot={r.transform.eulerAngles} min=({b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00}) max=({b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00})"));
                    var mf = r.GetComponent<MeshFilter>();
                    if (a.Length > 1 && mf && mf.sharedMesh)
                    {
                        var mb = mf.sharedMesh.bounds;
                        sb.Append("  corners");
                        for (int i = 0; i < 8; i++)
                        {
                            var c = new Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z);
                            var w = r.transform.TransformPoint(c);
                            sb.Append(FormattableString.Invariant($" ({w.x:0.00},{w.y:0.00},{w.z:0.00})"));
                        }
                        sb.AppendLine();
                    }
                }
                Out(sb.ToString());
            });
            Register("parcelstamps", a =>
            {
                // parcelstamps: stamps on every parcel of the new types (or all parcels with "all")
                var sb = new StringBuilder();
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                {
                    var props = p.GetComponent<Il2Cpp.EntityProperties>();
                    var c = props ? props.StorageConstraint : Il2Cpp.StorageConstraint.None;
                    if ((a.Length < 2 || a[1] == "mats") && !Content.ParcelKinds.IsModValue(c)) continue;
                    var st = p.GetComponentInChildren<Il2Cpp.EntityInteractableStamp>(true);
                    var names = new List<string>();
                    if (st != null) foreach (var si in st._stampInstances) names.Add(si.StampData ? si.StampData.name : "null");
                    sb.AppendLine($"{p.name} c={(int)c} pos={p.transform.position} stamps=[{string.Join(", ", names)}]");
                    if (st != null && a.Length > 1 && a[1] == "mats")
                        foreach (var si in st._stampInstances)
                            if (si.StampData && si.StampData.name.Contains("CME") && si.Stamp)
                                foreach (var r in si.Stamp.GetComponentsInChildren<MeshRenderer>(true))
                                    sb.AppendLine($"   {si.StampData.name} renderer mat={(r.sharedMaterial ? r.sharedMaterial.name : "-")} tex={(r.sharedMaterial && r.sharedMaterial.GetTexture("_BaseMap") ? r.sharedMaterial.GetTexture("_BaseMap").name : "-")}");
                }
                Out(sb.ToString());
            });
            Register("stampinfo", a =>
            {
                // stampinfo: native constraint stamp data, prefab structure and mesh layout
                var sb = new StringBuilder();
                var epm = Il2Cpp.EntityPropertiesManager.Instance;
                foreach (var cs in epm.Constraints)
                {
                    if (cs == null) continue;
                    var sd = cs.Stamp;
                    sb.AppendLine($"constraint={(int)cs.Constraint} unlocked={cs.IsUnlocked} stamp={(sd ? sd.name : "null")} type={(sd ? sd.GetIl2CppType().Name : "-")} tilt={(sd ? sd.MaximumRandomizedTilt : 0)} sprite={(sd && sd.StampPreviewSprite ? sd.StampPreviewSprite.name + " " + sd.StampPreviewSprite.rect : "-")}");
                    if (!sd || !sd.StampPrefab) continue;
                    InspectRec(sb, sd.StampPrefab.transform, 1);
                    foreach (var mf in sd.StampPrefab.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var m = mf.sharedMesh;
                        if (!m) continue;
                        sb.AppendLine($"   mesh {m.name} verts={m.vertexCount} bounds={m.bounds.center}/{m.bounds.size} readable={m.isReadable}");
                        if (m.isReadable)
                        {
                            var uv = m.uv; var v = m.vertices; var n = m.normals;
                            for (int i = 0; i < Math.Min(8, v.Length); i++) sb.AppendLine($"     v{i} {v[i]} n={(n.Length > i ? n[i] : Vector3.zero)} uv={(uv.Length > i ? uv[i] : Vector2.zero)}");
                        }
                    }
                }
                Out(sb.ToString());
            });
            Register("colliders", a =>
            {
                var sb = new StringBuilder();
                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (!go.scene.IsValid() || go.name != string.Join(" ", a, 1, a.Length - 1)) continue;
                    foreach (var c in go.GetComponentsInChildren<Collider>(true))
                    {
                        var b = c.bounds;
                        sb.AppendLine(FormattableString.Invariant($"{c.gameObject.name} layer={c.gameObject.layer} trig={c.isTrigger} min=({b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00}) max=({b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00})"));
                    }
                }
                Out(sb.ToString());
            });
            Register("toggle", a =>
            {
                // toggle <exact name> [0|1]
                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (!go.scene.IsValid() || go.name != a[1]) continue;
                    bool on = a.Length > 2 ? a[2] == "1" : !go.activeSelf;
                    go.SetActive(on);
                    Out($"{PathOf(go.transform)} -> {on}");
                }
            });
            Register("meshinfo", a =>
            {
                foreach (var sh in Resources.FindObjectsOfTypeAll<Il2Cpp.StampHelper>())
                {
                    if (!sh.name.Contains(a[1])) continue;
                    var mf = sh.GetComponentInChildren<MeshFilter>(true);
                    var m = mf.sharedMesh;
                    var sb = new StringBuilder($"{sh.name} mesh={m.name} verts={m.vertexCount} bounds={m.bounds.center}/{m.bounds.size} childScale={mf.transform.localScale}\n");
                    sb.AppendLine($" readable={m.isReadable} childRot={mf.transform.localEulerAngles} childPos={mf.transform.localPosition}");
                    Out(sb.ToString());
                    break;
                }
            });
            Register("setkind", a =>
            {
                // setkind <strawberry|banana|restless|none> [count]: converts parcels nearest the camera
                var value = a[1].ToLowerInvariant() switch
                {
                    "strawberry" => Content.ParcelKinds.Strawberry,
                    "banana" => Content.ParcelKinds.Banana,
                    "restless" => Content.ParcelKinds.Restless,
                    "otherworldly" => Content.ParcelKinds.Otherworldly,
                    "cold" => Il2Cpp.StorageConstraint.Cold,
                    _ => Il2Cpp.StorageConstraint.None,
                };
                int count = a.Length > 2 ? int.Parse(a[2]) : 1;
                var cam = Camera.main ? Camera.main.transform.position : Vector3.zero;
                if (_debugCam) cam = _debugCam.transform.position;
                var parcels = new List<Il2Cpp.EntityParcel>();
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None)) parcels.Add(p);
                parcels.Sort((x, y) => (x.transform.position - cam).sqrMagnitude.CompareTo((y.transform.position - cam).sqrMagnitude));
                for (int i = 0; i < count && i < parcels.Count; i++)
                {
                    parcels[i].Properties._StorageConstraint_k__BackingField = value;
                    Content.ParcelTracker.Register(parcels[i]);
                    Out($"set {parcels[i].name} at {parcels[i].transform.position} -> {value}");
                }
            });
            Register("audio", a =>
            {
                var sb = new StringBuilder();
                foreach (var s in Resources.FindObjectsOfTypeAll<Il2Cpp.AudioAsset_ScriptableObject>())
                    if (a.Length < 2 || s.name.Contains(a[1], StringComparison.OrdinalIgnoreCase))
                    {
                        var clips = s.AudioClips;
                        sb.AppendLine($"{s.name} clips={(clips != null ? clips.Length : 0)} len={(clips != null && clips.Length > 0 && clips[0] ? clips[0].length : 0):0.0} loop={s.Loop} mixer={(s.MixerGroupType ? s.MixerGroupType.name : "-")}");
                    }
                Out(sb.ToString());
            });
            Register("lights", a =>
            {
                var sb = new StringBuilder();
                foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                    sb.AppendLine($"{PathOf(l.transform)} type={l.type} int={l.intensity:0.###} range={l.range:0.##} col={l.color} shadows={l.shadows} enabled={l.enabled && l.gameObject.activeInHierarchy} mask={l.cullingMask}");
                Out(sb.ToString());
            });
            Register("whatat", a =>
            {
                // whatat x y z r : renderers whose bounds come within r of the point
                float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
                var p = new Vector3(F(1), F(2), F(3));
                float r = F(4);
                var sb = new StringBuilder();
                foreach (var rend in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var b = rend.bounds;
                    var q = new Vector3(Mathf.Clamp(p.x, b.min.x, b.max.x), Mathf.Clamp(p.y, b.min.y, b.max.y), Mathf.Clamp(p.z, b.min.z, b.max.z));
                    if ((q - p).magnitude > r) continue;
                    sb.AppendLine(FormattableString.Invariant($"{PathOf(rend.transform)} mat={(rend.sharedMaterial ? rend.sharedMaterial.name : "-")} min=({b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00}) max=({b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00})"));
                }
                Out(sb.ToString());
            });
            // ---- player driving (exercise real gameplay code paths)
            float P(string[] a, int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
            Il2Cpp.PlayerEntity Player() => Il2Cpp.PlayerManager.Instance?.LocalPlayerEntity;
            Register("tp", a =>
            {
                Player().CharacterController.Movement_Teleport(new Vector3(P(a, 1), P(a, 2), P(a, 3)));
                Out("tp ok");
            });
            Register("aim", a =>
            {
                var pc = Player().PlayerCamera;
                pc._horizontalRotation = P(a, 1);
                pc._verticalRotation = P(a, 2);
                Out($"aim {pc._horizontalRotation} {pc._verticalRotation}");
            });
            Register("pos", a =>
            {
                var p = Player();
                var cam = p.PlayerCamera.Camera;
                Out($"player {p.transform.position} cam {cam.transform.position} fwd {cam.transform.forward} h={p.PlayerCamera._horizontalRotation} v={p.PlayerCamera._verticalRotation} holding={(p.PlayerInventory != null ? "?" : "")}");
            });
            Register("pickup", a =>
            {
                // pickup <namecontains> : nearest matching entity to the player
                var p = Player();
                Il2Cpp.Entity best = null;
                float bd = float.MaxValue;
                foreach (var e in UnityEngine.Object.FindObjectsByType<Il2Cpp.Entity>(FindObjectsSortMode.None))
                {
                    if (!e.name.Contains(a[1], StringComparison.OrdinalIgnoreCase)) continue;
                    if (e.Interactable == null || e.Interactable.Pickable == null) continue;
                    float d = (e.transform.position - p.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; best = e; }
                }
                if (best == null) { Out("nothing to pick up"); return; }
                p.PlayerInventory.PickUp(best);
                Out($"picked {best.name} at {best.transform.position} dist {Mathf.Sqrt(bd):0.0}");
            });
            Register("pickkind", a =>
            {
                var p = Player();
                var want = a[1].ToLowerInvariant();
                Il2Cpp.EntityParcel best = null;
                float bd = float.MaxValue;
                foreach (var e in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                {
                    var k = Content.ParcelKinds.KindOf(e.Properties.StorageConstraint).ToString().ToLowerInvariant();
                    if (k != want) continue;
                    float d = (e.transform.position - p.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; best = e; }
                }
                if (best == null) { Out("none"); return; }
                p.PlayerInventory.PickUp(best);
                Out($"picked {best.name} {want}");
            });
            Register("dropbtn", a => { var inv = Player().PlayerInventory; inv.InputManager_OnDropPerformed(); Scheduler_After(float.Parse(a.Length > 1 ? a[1] : "0.08", System.Globalization.CultureInfo.InvariantCulture), () => inv.InputManager_OnDropCanceled()); Out("drop button"); });
            Register("dropvalid", a => { var p = Player(); Il2Cpp.Entity held = null; foreach (var e in UnityEngine.Object.FindObjectsByType<Il2Cpp.Entity>(FindObjectsSortMode.None)) { var pk = e.Interactable?.Pickable; if (pk != null && pk.CarryingEntity != null) held = e; } Out(held == null ? "holding nothing" : $"holding {held.name} valid={p.PlayerInventory.IsEntityDropValid(held)} kinematic={held.GetComponent<Rigidbody>()?.isKinematic}"); });
            Register("drop", a => { Player().PlayerInventory.InstantlyDrop(null); Out("dropped"); });
            Register("throw", a => { var inv = Player().PlayerInventory; inv.StartDrop(); Scheduler_After(float.Parse(a.Length > 1 ? a[1] : "0.6", System.Globalization.CultureInfo.InvariantCulture), () => inv.EndDrop()); Out("throwing"); });
            Register("interact", a => { Player().PlayerInteraction.InputManager_OnInteractPerformed(); Out("interact"); });
            Register("target", a => { var t = Player().PlayerInteraction.CHEAT_GetTargetEntity(); Out("target " + (t != null ? t.name + " " + t.transform.position : "none")); });
            Register("notify", a =>
            {
                // notify <key|text>: show a toast (CME/ keys are localized), then report each toast's text layout
                string s = string.Join(" ", a, 1, a.Length - 1);
                Il2Cpp.InterfaceManager.Instance.NotificationInterface.ShowNotification(s.StartsWith("CME/") ? Content.Localization.Text(s) : s);
                var sb = new System.Text.StringBuilder();
                foreach (var n in UnityEngine.Object.FindObjectsByType<Il2Cpp.NotificationItemInterface>(FindObjectsSortMode.None))
                {
                    var t = n.LinkedText;
                    sb.AppendLine($"toast rect={t.rectTransform.rect.size} size={t.fontSize} max={t.fontSizeMax} min={t.fontSizeMin} auto={t.enableAutoSizing} wrap={t.textWrappingMode} overflow={t.overflowMode} margin={t.margin} pref={t.GetPreferredValues()} lines={t.textInfo.lineCount}");
                }
                Out(sb.ToString());
            });
            Register("save", a => { Il2Cpp.SaveManager.Instance.SaveGame(); Out("saved"); });
            Register("dialogue", a =>
            {
                // dialogue <kind> [mood]: run the game's request-dialogue builder for a parcel of that kind
                var want = a[1].ToLowerInvariant();
                Il2Cpp.EntityParcel parcel = null;
                foreach (var e in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                    if (Content.ParcelKinds.KindOf(e.Properties.StorageConstraint).ToString().ToLowerInvariant() == want) { parcel = e; break; }
                if (parcel == null) { Out("no parcel of kind " + want); return; }
                Il2Cpp.EntityCustomer customer = null;
                foreach (var c in Resources.FindObjectsOfTypeAll<Il2Cpp.EntityCustomer>()) if (c && c.gameObject.scene.IsValid()) { customer = c; break; }
                if (customer == null) { Out("no customer object"); return; }
                var mgr = Il2Cpp.CustomerDialogueManager.Instance;
                var sb = new StringBuilder();
                var moods = new[] { Il2Cpp.DialogueMood.Neutral, Il2Cpp.DialogueMood.Excited, Il2Cpp.DialogueMood.Grumpy, Il2Cpp.DialogueMood.Anxious };
                for (int i = 0; i < 6; i++)
                {
                    var mood = moods[i % moods.Length];
                    var lines = mgr.GetRequestDialogue(parcel, customer, mood);
                    sb.Append($"[{mood}] ");
                    foreach (var l in lines) sb.Append(Il2CppI2.Loc.LocalizationManager.GetTranslation(l.Key, true, 0, true, false, null, null, true)).Append(" | ");
                    sb.AppendLine();
                }
                Out(sb.ToString());
            });
            Register("rollstats", a =>
            {
                var epm = Il2Cpp.EntityPropertiesManager.Instance;
                var any = UnityEngine.Object.FindObjectOfType<Il2Cpp.EntityParcel>();
                var counts = new Dictionary<int, int>();
                for (int i = 0; i < 2000; i++)
                {
                    int v = (int)epm.GetRandomStorageConstraint(any.Properties);
                    counts[v] = counts.TryGetValue(v, out var c) ? c + 1 : 1;
                }
                var sb = new StringBuilder("2000 rolls: ");
                foreach (var kv in counts) sb.Append($"{(Il2Cpp.StorageConstraint)kv.Key}({kv.Key})={kv.Value} ");
                sb.Append(" available=");
                foreach (var c in epm.GetAvailableStorageConstraints()) sb.Append((int)c).Append(',');
                Out(sb.ToString());
            });
            Register("ray", a =>
            {
                float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
                var o = new Vector3(F(1), F(2), F(3));
                var d = new Vector3(F(4), F(5), F(6)).normalized;
                var hits = Physics.RaycastAll(o, d, 20f, ~0, QueryTriggerInteraction.Ignore);
                var sb = new StringBuilder();
                foreach (var h in hits) sb.AppendLine($"{h.distance:0.000} {h.point} {PathOf(h.collider.transform)} layer={h.collider.gameObject.layer}");
                Out(sb.ToString());
            });
            Register("postnext", a =>
            {
                var pm = UnityEngine.Object.FindObjectOfType<Il2Cpp.EntityOldPostman>();
                if (pm == null) { Out("no postman"); return; }
                pm.NextDialogStep();
                Out("postman next");
            });
            Register("creditsdone", a =>
            {
                var pm = UnityEngine.Object.FindObjectOfType<Il2Cpp.EntityOldPostman>();
                if (pm == null) { Out("no postman"); return; }
                pm.CreditsInterface_OnFinishedScrolling();
                Out("credits finished");
            });
            Register("postdespawn", a =>
            {
                var pm = UnityEngine.Object.FindObjectOfType<Il2Cpp.EntityOldPostman>();
                if (pm == null) { Out("no postman"); return; }
                pm.Signal_Despawn();
                Out("despawn signalled");
            });
            Register("poststate", a =>
            {
                var pm = UnityEngine.Object.FindObjectOfType<Il2Cpp.EntityOldPostman>();
                if (pm == null) { Out("no postman"); return; }
                Out($"started={pm._endingStarted} finished={pm._clientFinished} players={Il2Cpp.PlayerManager.Instance.PlayerCount} sessionEnding={Content.ModProgress.EndingSeenThisSession}");
            });
            Register("postend", a =>
            {
                var pm = UnityEngine.Object.FindObjectOfType<Il2Cpp.EntityOldPostman>();
                if (pm == null) { Out("no postman"); return; }
                pm.EndCinematic();
                Out("EndCinematic called");
            });
            Register("flag", a => Out($"FinishedEnding={Il2Cpp.SaveManager.Instance.GetBool(Il2Cpp.TableType.GameProgression, "FinishedEnding", false, null)}"));
            Register("uv2", a =>
            {
                // uv2 <meshname> <u> <v> | uv2 <meshname> copy
                var m = Core.ModAssets.Mesh(a[1]);
                var n = m.vertexCount;
                var arr = new Vector2[n];
                if (a[2] == "copy") { var uv = m.uv; for (int i = 0; i < n; i++) arr[i] = uv[i]; }
                else { var c = new Vector2(float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture)); for (int i = 0; i < n; i++) arr[i] = c; }
                m.uv2 = arr;
                Out("uv2 set on " + a[1]);
            });
            Register("plainmat", a =>
            {
                Shader sh = null;
                foreach (var s in Resources.FindObjectsOfTypeAll<Shader>()) if (s.name == "Universal Render Pipeline/Lit") { sh = s; break; }
                foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (r.gameObject.name != "CME_" + a[1]) continue;
                    var m = new Material(sh);
                    m.SetColor("_BaseColor", Color.magenta);
                    if (a.Length > 2) m.SetFloat("_Cull", float.Parse(a[2]));
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = m;
                    r.sharedMaterials = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Material>(mats);
                    Out("plain material on " + r.name + " submeshes=" + mats.Length);
                }
            });
            Register("clearcans", a => { int n = Content.TunaCans.DisposeAll(); Content.SpeakerRoom.ForceRefresh(); Out("disposed " + n); });
            Register("compartments", a =>
            {
                var sb = new StringBuilder();
                foreach (var (area, en) in Content.SpeakerRoom.DebugCompartments()) sb.AppendLine($"{area.Center} enabled={en}");
                Out(sb.ToString());
            });
            Register("cycle", a => { Il2Cpp.GameTimeManager.Instance.CHEAT_CycleTimePeriod(); Out("period now " + Il2Cpp.GameTimeManager.Instance.CurrentTimePeriod); });
            Register("period", a => Out("period " + Il2Cpp.GameTimeManager.Instance.CurrentTimePeriod + " shift% " + Il2Cpp.GameTimeManager.Instance.CurrentShiftTimePercentage));
            Register("customers", a =>
            {
                var sb = new StringBuilder();
                foreach (var c in Resources.FindObjectsOfTypeAll<Il2Cpp.EntityCustomer>())
                    if (c && c.gameObject.scene.IsValid()) sb.AppendLine($"{c.name} active={c.gameObject.activeInHierarchy} pos={c.transform.position}");
                Out(sb.ToString());
            });
            Register("callcustomer", a =>
            {
                // callcustomer <kind> [counter]: queue a request for a parcel of that kind and ring the bell
                var want = a[1].ToLowerInvariant();
                int counter = a.Length > 2 ? int.Parse(a[2]) : 0;
                Il2Cpp.EntityParcel parcel = null;
                foreach (var e in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                    if (Content.ParcelKinds.KindOf(e.Properties.StorageConstraint).ToString().ToLowerInvariant() == want) { parcel = e; break; }
                if (parcel == null) { Out("no parcel " + want); return; }
                var cm = Il2Cpp.CustomerManager.Instance;
                cm.SetCustomerDepositProbability(0f);
                cm.EnqueueParcelToRequest(0, parcel);
                var counters = Il2Cpp.WorldManager.Instance.Counters;
                counters[counter].CallNextCustomerBell_OnPrimaryInteracted(Player());
                Out($"called customer at counter {counter} for {parcel.name}");
            });
            Register("speech", a =>
            {
                var sb = new StringBuilder();
                foreach (var t in UnityEngine.Object.FindObjectsByType<Il2CppTMPro.TMP_Text>(FindObjectsSortMode.None))
                {
                    var p = PathOf(t.transform);
                    if (p.Contains("p_EntityCustomer") && t.gameObject.name == "text_Speech")
                        sb.AppendLine(p + " :: " + t.text.Replace('\n', '|'));
                }
                Out(sb.ToString());
            });
            Register("setlevel", a => { Il2Cpp.ProgressionManager.Instance.SetLevel(int.Parse(a[1]), false); Out("level set " + a[1]); });
            // ---- placement tools
            GameObject FindMod(string term)
            {
                GameObject best = null;
                foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (!go.scene.IsValid()) continue;
                    var p = PathOf(go.transform);
                    if (!p.StartsWith("CME_")) continue;
                    if (go.name.Equals(term, StringComparison.OrdinalIgnoreCase)) return go;
                    if (best == null && p.Contains(term, StringComparison.OrdinalIgnoreCase)) best = go;
                }
                return best;
            }
            Register("objs", a =>
            {
                var sb = new StringBuilder();
                foreach (var root in new[] { "CME_Yard", "CME_SpeakerRoom", "CME_Planetarium", "CME_FrontPath" })
                {
                    var r = GameObject.Find(root);
                    if (!r) continue;
                    for (int i = 0; i < r.transform.childCount; i++)
                    {
                        var c = r.transform.GetChild(i);
                        sb.AppendLine(FormattableString.Invariant($"{root}/{c.name} pos=({c.position.x:0.00},{c.position.y:0.00},{c.position.z:0.00}) yaw={c.eulerAngles.y:0}"));
                    }
                }
                Out(sb.ToString());
            });
            Register("move", a =>
            {
                // move <name> x y z [yaw] [scale]
                var go = FindMod(a[1]);
                if (!go) { Out("not found " + a[1]); return; }
                float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
                go.transform.position = new Vector3(F(2), F(3), F(4));
                if (a.Length > 5) go.transform.rotation = Quaternion.Euler(0, F(5), 0);
                if (a.Length > 6) go.transform.localScale = Vector3.one * F(6);
                Physics.SyncTransforms();
                Out(Core.Placement.Describe(go));
            });
            Register("clip", a => { var go = FindMod(a[1]); Physics.SyncTransforms(); Out(go ? Core.Placement.Describe(go) : "not found"); });
            Register("settle", a => { var go = FindMod(a[1]); if (!go) { Out("not found"); return; } Core.Placement.Settle(go); Out(Core.Placement.Describe(go)); });
            Register("park", a => { Player().CharacterController.Movement_Teleport(new Vector3(3f, 0.05f, 3f)); Out("player parked"); });
            Register("orbit", a =>
            {
                // orbit name x y z dist pitch fov : four shots name_0.._3 around a point, player parked first
                Player().CharacterController.Movement_Teleport(new Vector3(3f, 0.05f, 3f));
                float F(int i, float d) => a.Length > i ? float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture) : d;
                var target = new Vector3(F(2, 0), F(3, 1), F(4, 0));
                float dist = F(5, 2.5f), pitch = F(6, 20), fov = F(7, 60);
                MelonCoroutines.Start(OrbitRoutine(a[1], target, dist, pitch, fov));
            });
            Register("volume", a =>
            {
                // volume : list active volume components; volume <i> <0|1> : toggle one
                var sb = new StringBuilder();
                int idx = 0;
                foreach (var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None))
                {
                    var prof = v.sharedProfile;
                    sb.AppendLine($"{PathOf(v.transform)} global={v.isGlobal} weight={v.weight} prio={v.priority} profile={(prof ? prof.name : "-")}");
                    if (!prof) continue;
                    foreach (var c in prof.components)
                    {
                        if (a.Length > 2 && idx == int.Parse(a[1])) c.active = a[2] == "1";
                        sb.AppendLine($"  [{idx}] {c.GetIl2CppType().Name} active={c.active}");
                        idx++;
                    }
                }
                Out(sb.ToString());
            });
            Register("copyvis", a =>
            {
                // copyvis <exact name> x y z [yaw] : visual-only clone placed under CME_DevCopies
                float F(int i, float d) => a.Length > i ? float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture) : d;
                var src = Core.World.Find(a[1]);
                var holder = GameObject.Find("CME_Yard") ?? new GameObject("CME_Yard");
                var c = Core.World.CopyVisual(src, holder.transform, new Vector3(F(2, 0), F(3, 0), F(4, 0)), F(5, 0), 1f, false);
                Out(c ? Core.Placement.Describe(c) : "not found " + a[1]);
            });
            Register("meshtops", a =>
            {
                // meshtops <exact object name> : highest vertices per 0.25m cell (garland attach points)
                var go = Core.World.Find(a[1]);
                var mf = go ? go.GetComponentInChildren<MeshFilter>() : null;
                if (!mf) { Out("no mesh"); return; }
                var m = mf.sharedMesh;
                if (!m.isReadable) { Out($"{m.name} not readable, bounds {m.bounds.center} {m.bounds.size}"); return; }
                var v = m.vertices;
                var sb = new StringBuilder($"{m.name} verts={v.Length} bounds {m.bounds.center} {m.bounds.size}\n");
                var best = new Dictionary<(int, int), Vector3>();
                foreach (var p in (Vector3[])v)
                {
                    var k = ((int)Mathf.Floor(p.x / 0.25f), (int)Mathf.Floor(p.z / 0.25f));
                    if (!best.TryGetValue(k, out var b) || p.y > b.y) best[k] = p;
                }
                float top = m.bounds.max.y;
                foreach (var kv in best) if (kv.Value.y > top - 0.08f) sb.AppendLine($"top {kv.Value}");
                Out(sb.ToString());
            });
            Register("fog", a => Out(Content.OutdoorAtmosphere.Describe()));
            Register("spherecast", a =>
            {
                var cam = Player().PlayerCamera.Camera;
                var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                var sb = new StringBuilder();
                foreach (var r in new[] { 0.02f, 0.05f, 0.1f, 0.15f, 0.2f, 0.3f })
                {
                    if (Physics.SphereCast(ray, r, out var hit, 4f, Il2Cpp.Constants.InteractableLayerMask | Il2Cpp.Constants.InteractableObstacleLayerMask, QueryTriggerInteraction.Ignore))
                        sb.AppendLine($"r={r} d={hit.distance:0.00} {PathOf(hit.collider.transform)} layer={hit.collider.gameObject.layer}");
                    else sb.AppendLine($"r={r} none");
                }
                Out(sb.ToString());
            });
            Register("masks", a => Out($"Entity={Il2Cpp.Constants.EntityLayer} EntityMask={Convert.ToString(Il2Cpp.Constants.EntityLayerMask,2)} Interactable={Convert.ToString(Il2Cpp.Constants.InteractableLayerMask,2)} ObstacleAllowingActions={Convert.ToString(Il2Cpp.Constants.ObstacleAllowingActionsLayerMask,2)} WallObstacle={Convert.ToString(Il2Cpp.Constants.WallObstacleLayerMask,2)} InteractableObstacle={Convert.ToString(Il2Cpp.Constants.InteractableObstacleLayerMask,2)} layers: {string.Join(",", System.Linq.Enumerable.Range(0,32).Select(i => i + "=" + LayerMask.LayerToName(i)).Where(s => !s.EndsWith("=")))}"));
            Register("tracked", a => Out(Content.ParcelTracker.Describe()));
            Register("progress", a => Out($"yard={Content.ModProgress.YardUnlocked} speaker={Content.ModProgress.SpeakerRoomUnlocked} tracked={Content.ParcelTracker.Count} level={Il2Cpp.ProgressionManager.Instance?.CurrentLevel}"));
            Register("player", a =>
            {
                var cam = Camera.main;
                Out(cam ? $"camera {cam.transform.position} {cam.transform.eulerAngles}" : "no camera");
            });
            Register("buttons", a => ListButtons());
            Register("click", a => Click(string.Join(" ", a, 1, a.Length - 1)));
            Register("savefiles", a =>
            {
                var sm = Il2Cpp.SaveManager.Instance;
                Out(string.Join(" | ", (string[])sm.GetGameSaveNameFiles()));
            });
            Register("wiggletest", a => Content.ParcelTracker.DebugWiggle(r => Plugin.Log.Msg("wiggletest: " + r.Replace(System.Environment.NewLine, " | "))));
            Register("deletetestsave", a =>
            {
                // deletetestsave [go]: deletes the mod's CME-Test save copy through the game (so Steam Cloud syncs it)
                const string stem = "GameSave_20261022_200000_CME-Test";
                var sm = Il2Cpp.SaveManager.Instance;
                var path = sm.GetGameSaveFullPath(stem);
                if (path == null || !System.IO.File.Exists(path)) { Out("not found: " + path); return; }
                if (a.Length > 1 && a[1] == "go") { sm.DeleteGameSaveFile(stem); Out($"deleted {path} exists={System.IO.File.Exists(path)}"); }
                else Out("would delete " + path);
            });
            Register("deletesave", a =>
            {
                // deletesave <save name>: presses the delete button on that save slot in the save menu
                var want = string.Join(" ", a, 1, a.Length - 1);
                foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                {
                    if (!b.gameObject.activeInHierarchy || !b.name.Contains("DeleteSave")) continue;
                    var slot = b.transform.parent;
                    var text = new StringBuilder();
                    foreach (var t in slot.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true)) text.Append(t.text).Append(' ');
                    if (!text.ToString().Contains(want)) continue;
                    b.onClick.Invoke();
                    Out("pressed delete on " + text);
                    return;
                }
                Out("no slot " + want);
            });
            Register("timescale", a => Time.timeScale = float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture));
            DevPlanetarium.Register();
        }

        public static void Register(string name, Action<string[]> act) => _commands[name] = act;

        static void Scheduler_After(float s, Action a) => Core.Scheduler.After(s, a);

        public static void Out(string s)
        {
            File.AppendAllText(_outFile, s + "\n");
        }

        public static void Update()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 0.25f;
            if (!File.Exists(_cmdFile)) return;
            string[] lines;
            try { lines = File.ReadAllLines(_cmdFile); File.Delete(_cmdFile); }
            catch { return; }
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    if (_commands.TryGetValue(parts[0], out var act)) act(parts);
                    else Out("unknown command: " + parts[0]);
                }
                catch (Exception e) { Out("ERR " + line + ": " + e); }
            }
            Out("#done " + DateTime.Now.ToString("HH:mm:ss.fff"));
        }

        static void Shot(string name)
        {
            var path = System.IO.Path.Combine(_dir, name + ".png");
            MelonCoroutines.Start(ShotRoutine(path));
        }

        static System.Collections.IEnumerator OrbitRoutine(string name, Vector3 target, float dist, float pitch, float fov)
        {
            for (int i = 0; i < 4; i++)
            {
                Cam(new[] { "cam", "0", "0", "0", "0", "0", fov.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                _debugCam.orthographic = false;
                _debugCam.nearClipPlane = 0.05f;
                var rot = Quaternion.Euler(pitch, i * 90f, 0);
                _debugCam.transform.SetPositionAndRotation(target - rot * Vector3.forward * dist, rot);
                for (int f = 0; f < 3; f++) yield return null;
                yield return ShotRoutine(System.IO.Path.Combine(_dir, $"{name}_{i}.png"));
            }
            Out("orbit done " + name);
        }

        static System.Collections.IEnumerator ShotRoutine(string path)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            // Downscale large captures to keep files small.
            int w = tex.width, h = tex.height;
            Texture2D outTex = tex;
            if (w > 1600)
            {
                int nw = 1600, nh = h * 1600 / w;
                var rt = RenderTexture.GetTemporary(nw, nh);
                Graphics.Blit(tex, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                outTex = new Texture2D(nw, nh, TextureFormat.RGB24, false);
                outTex.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
                outTex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.Destroy(tex);
            }
            var bytes = ImageConversion.EncodeToPNG(outTex);
            File.WriteAllBytes(path, bytes);
            UnityEngine.Object.Destroy(outTex);
            Out("shot " + path);
        }

        static void Cam(string[] a)
        {
            if (a.Length > 1 && a[1] == "off")
            {
                if (_debugCam) UnityEngine.Object.Destroy(_debugCam.gameObject);
                _debugCam = null;
                return;
            }
            if (!_debugCam)
            {
                var go = new GameObject("CME_DebugCam");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _debugCam = go.AddComponent<Camera>();
                var main = Camera.main;
                if (main) { _debugCam.CopyFrom(main); }
                _debugCam.depth = 100;
            }
            float F(int i, float d) => a.Length > i ? float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture) : d;
            _debugCam.transform.position = new Vector3(F(1, 0), F(2, 2), F(3, 0));
            _debugCam.transform.rotation = Quaternion.Euler(F(5, 20), F(4, 0), 0);
            _debugCam.fieldOfView = F(6, 60);
        }

        static void Find(string term)
        {
            var sb = new StringBuilder();
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go.name.Contains(term, StringComparison.OrdinalIgnoreCase)) continue;
                if (!go.scene.IsValid()) continue;
                sb.AppendLine($"{PathOf(go.transform)} active={go.activeInHierarchy} pos={go.transform.position} {Comps(go)}");
            }
            Out(sb.ToString());
        }

        /// <summary>Dumps the first GameObject whose name contains term: hierarchy, renderers and material properties.</summary>
        static void Inspect(string term)
        {
            GameObject target = null;
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go.scene.IsValid() && go.activeInHierarchy && go.name.Contains(term, StringComparison.OrdinalIgnoreCase)) { target = go; break; }
            if (!target) { Out("not found " + term); return; }
            var sb = new StringBuilder();
            InspectRec(sb, target.transform, 0);
            Out(sb.ToString());
        }

        static void InspectRec(StringBuilder sb, Transform t, int d)
        {
            var go = t.gameObject;
            sb.AppendLine($"{new string(' ', d * 2)}{go.name} {(go.activeSelf ? "" : "(off)")} lp={t.localPosition} lr={t.localEulerAngles} ls={t.localScale} layer={go.layer} {Comps(go)}");
            var r = go.GetComponent<Renderer>();
            if (r)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (!m) continue;
                    var sh = m.shader;
                    sb.Append($"{new string(' ', d * 2 + 2)}mat {m.name} shader={sh.name} rq={m.renderQueue} kw=[{string.Join(",", (string[])m.shaderKeywords)}] props:");
                    int n = sh.GetPropertyCount();
                    for (int i = 0; i < n; i++)
                    {
                        var pn = sh.GetPropertyName(i);
                        var pt = sh.GetPropertyType(i);
                        string v = pt switch
                        {
                            UnityEngine.Rendering.ShaderPropertyType.Texture => m.GetTexture(pn) ? m.GetTexture(pn).name : "-",
                            UnityEngine.Rendering.ShaderPropertyType.Color => m.GetColor(pn).ToString(),
                            UnityEngine.Rendering.ShaderPropertyType.Vector => m.GetVector(pn).ToString(),
                            _ => m.GetFloat(pn).ToString("0.###"),
                        };
                        sb.Append($" {pn}={v}");
                    }
                    sb.AppendLine();
                }
            }
            for (int i = 0; i < t.childCount; i++) InspectRec(sb, t.GetChild(i), d + 1);
        }

        static void Bounds(string term)
        {
            var sb = new StringBuilder();
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go.scene.IsValid() || !go.name.Equals(term, StringComparison.OrdinalIgnoreCase)) continue;
                for (int i = 0; i < go.transform.childCount; i++)
                    AppendBounds(sb, go.transform.GetChild(i).gameObject);
                AppendBounds(sb, go);
            }
            Out(sb.ToString());
        }

        static void AppendBounds(StringBuilder sb, GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            sb.AppendLine(FormattableString.Invariant($"{PathOf(go.transform)} min=({b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00}) max=({b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00})"));
        }

        static string ButtonLabel(UnityEngine.UI.Button b)
        {
            var sb = new StringBuilder();
            foreach (var t in b.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true)) sb.Append(t.text).Append(' ');
            return sb.ToString().Trim();
        }

        static void ListButtons()
        {
            var sb = new StringBuilder();
            foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                if (!b.gameObject.activeInHierarchy) continue;
                sb.AppendLine($"{PathOf(b.transform)} interactable={b.interactable} label='{ButtonLabel(b)}'");
            }
            Out(sb.ToString());
        }

        static void Click(string term)
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                if (!b.gameObject.activeInHierarchy || !b.interactable) continue;
                if (PathOf(b.transform).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    ButtonLabel(b).Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    b.onClick.Invoke();
                    Out("clicked " + PathOf(b.transform));
                    return;
                }
            }
            Out("no button matching " + term);
        }

        public static string PathOf(Transform t)
        {
            var s = t.name;
            while (t.parent) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        static string Comps(GameObject go)
        {
            var sb = new StringBuilder("[");
            foreach (var c in go.GetComponents<Component>())
            {
                if (!c) continue;
                sb.Append(c.GetIl2CppType().Name).Append(',');
            }
            return sb.Append(']').ToString();
        }

        static void DumpScene(string rootFilter, int maxDepth)
        {
            var path = System.IO.Path.Combine(_dir, "scene_dump.txt");
            using var w = new StreamWriter(path);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                w.WriteLine($"=== Scene {sc.name} loaded={sc.isLoaded}");
                foreach (var root in sc.GetRootGameObjects())
                {
                    if (rootFilter != null && rootFilter != "*" && !root.name.Contains(rootFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    DumpRec(w, root.transform, 0, maxDepth);
                }
            }
            Out("dumped " + path);
        }

        static void DumpRec(StreamWriter w, Transform t, int depth, int maxDepth)
        {
            var go = t.gameObject;
            w.WriteLine($"{new string(' ', depth * 2)}{go.name} {(go.activeSelf ? "" : "(off) ")}p={Fmt(t.position)} r={Fmt(t.eulerAngles)} s={Fmt(t.lossyScale)} {Comps(go)}");
            if (depth >= maxDepth) return;
            for (int i = 0; i < t.childCount; i++) DumpRec(w, t.GetChild(i), depth + 1, maxDepth);
        }

        static string Fmt(Vector3 v) => FormattableString.Invariant($"({v.x:0.##},{v.y:0.##},{v.z:0.##})");
    }
}
