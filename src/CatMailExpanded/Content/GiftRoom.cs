using System;
using System.Collections.Generic;
using System.Linq;
using CatMailExpanded.Core;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The gift wrapping room, off the extended pile corridor across from the mirror room. Once it unlocks, a few
    /// extra customers each shift (on top of the shift's normal customers: they never count towards it) bring a gift
    /// to be wrapped. They describe the paper and ribbon they want in a riddle, which is also written on the gift's
    /// tag. The player puts the gift on the wrapping bench (or holds it) and clicks a paper roll and a ribbon spool on
    /// the pegboard. The gift then waits on a shelf like any parcel until the person it is for asks for it; the
    /// wrapping is scored when it is handed over, and shows as its own line on the shift recap.
    ///
    /// State lives in the parcel's own saved fields: the paper in the storage constraint (ParcelKinds.Gift, or one
    /// value per paper), the ribbon in the game's own ribbon colour (three new colours join the game's three), and
    /// the wish is worked out from the recipient's name, so it never changes.
    /// </summary>
    internal static class GiftRoom
    {
        // ---------------------------------------------------------------- layout (keep in sync with gifts.py)
        // the room's inner walls (PileWing's RX/RZ bounds are the outer ones). The west and south walls' visible
        // surfaces stand a little in front of their colliders (x 7.69, z 25.84), as the baseboards show.
        const float WallE = 11.99f, WallN = 33.41f, WallS = 25.93f, WallW = 7.89f;
        static readonly Vector3 BenchPos = new(WallE - 0.59f, PileWing.FloorY, 29.4f);
        const float BenchTop = 0.85f, BenchW = 1.0f, BenchL = 2.0f;
        const float RollX = 0.36f, DowelX = 0.38f, DowelY = 2.1f;
        static readonly float[] RollY = { 1.42f, 1.72f };
        static readonly float[] RollZ = { -0.75f, -0.25f, 0.25f, 0.75f };
        const int StoreIdBase = 700, LabelIdBase = 800;
        const float RugX = 9.7f, RugZ = 29.9f, RugW = 2.27f, RugL = 3.4f;
        const float PalletZ = 27.6f;   // the floor pallet for big boxes, under the poster
        const float ClickRange = 3.2f;

        public static readonly string[] Papers = { "stars", "candy", "dots", "flowers", "fish", "plaid", "paws", "gold" };
        public const int RibbonCount = 6;
        public static readonly string[] RibbonNames = { "red", "yellow", "blue", "green", "purple", "pink" };
        // the game has red, yellow and blue (ParcelRibbonColor 1-3); ours are 4-6
        static readonly Color[] NewRibbonColors = { new(0.36f, 0.70f, 0.38f), new(0.60f, 0.42f, 0.84f), new(1.0f, 0.5f, 0.66f) };
        const float PaperTile = 0.5f;

        public struct Wish { public int Paper, Ribbon, PaperVariant, RibbonVariant; }

        sealed class Selector
        {
            public bool IsRibbon;
            public int Index;
            public Transform Visual;
            public Vector3 RestScale;
            public float Pop;
        }

        static GameObject _root;
        static EntityInteractableStore _bench;
        static readonly Dictionary<IntPtr, Selector> _selectors = new();
        static readonly List<Selector> _selectorList = new();
        static Selector _hovered;
        static readonly List<string> _log = new();
        static bool _unlocked;
        static int _pendingGifts;
        static bool _nextIsGift, _forceNext;
        static int _skipSatisfied;
        static float _nextHover;
        // shift tallies, and the last shift's for the recap row
        static int _shiftCount, _shiftScore;
        public static int LastCount { get; private set; }
        public static int LastScore { get; private set; }
        static readonly HashSet<IntPtr> _scored = new();
        // the reaction the customer who just received a gift will say goodbye with
        static IntPtr _reactCustomer;
        static string _reactTier;

        // materials and meshes
        static readonly Dictionary<int, Material> _paperMats = new(), _paperDamagedMats = new();
        static readonly Dictionary<Vector3Int, Mesh> _paperBoxes = new();
        static Material _tagMat;
        static TMP_FontAsset _font;
        static int _nativeRibbonCount = -1;

        public static bool Built => _root;
        public static bool Unlocked => _unlocked;

        public static void Reset()
        {
            _root = null;
            _bench = null;
            _selectors.Clear();
            _selectorList.Clear();
            _hovered = null;
            _log.Clear();
            _unlocked = false;
            _pendingGifts = 0;
            _nextIsGift = _forceNext = false;
            _skipSatisfied = 0;
            _shiftCount = _shiftScore = 0;
            LastCount = LastScore = 0;
            _scored.Clear();
            _reactCustomer = IntPtr.Zero;
            _paperMats.Clear();
            _paperDamagedMats.Clear();
            _tagMat = null;
            _font = null;
        }

        static bool IsServer => NetworkManager.Instance == null || NetworkManager.Instance.IsServer;

        public static bool Contains(Vector3 p) => PileWing.InGiftRoom(p);

        // ---------------------------------------------------------------- wishes

        /// <summary>What the sender asked for, worked out from the recipient's name (saved with the parcel).</summary>
        public static Wish WishOf(EntityParcel p)
        {
            uint h = 2166136261;
            try
            {
                var props = p.Properties;
                h = (h ^ (uint)props._CustomerNameIndex_k__BackingField) * 16777619;
                h = (h ^ (uint)props._CustomerSurnameIndex_k__BackingField) * 16777619;
                h = (h ^ props._CustomerLastNameInitial_k__BackingField) * 16777619;
            }
            catch { }
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return new Wish
            {
                Paper = (int)(h % (uint)Papers.Length),
                Ribbon = (int)(h / 8 % RibbonCount),
                PaperVariant = (int)(h / 48 % Localization.GiftRiddleVariants),
                RibbonVariant = (int)(h / 96 % Localization.GiftRiddleVariants),
            };
        }

        static string PaperAskKey(Wish w) => $"CME/GiftPaperAsk_{w.Paper}_{w.PaperVariant}";
        static string RibbonAskKey(Wish w) => $"CME/GiftRibbonAsk_{w.Ribbon}_{w.RibbonVariant}";

        /// <summary>The gift customer's whole request: an intro and the two riddles.</summary>
        public static string AskLineKey(EntityParcel p)
        {
            var w = WishOf(p);
            int intro = Random.Range(0, Localization.GiftIntroCount);
            return Localization.Composite($"CME/GiftAsk_{intro}_{w.Paper}_{w.PaperVariant}_{w.Ribbon}_{w.RibbonVariant}",
                $"CME/GiftIntro_{intro}", PaperAskKey(w), RibbonAskKey(w));
        }

        /// <summary>What the person picking the gift up was told to look for.</summary>
        public static string WantLineKey(EntityParcel p)
        {
            var w = WishOf(p);
            return Localization.Composite($"CME/GiftWant_{w.Paper}_{w.Ribbon}", $"CME/GiftPaperWant_{w.Paper}", $"CME/GiftRibbonWant_{w.Ribbon}");
        }

        public static int RibbonOf(EntityParcel p)
        {
            try { return (int)p.Properties._RibbonColor_k__BackingField - 1; }
            catch { return -1; }
        }

        // ---------------------------------------------------------------- ribbons (the game's own ribbon, more colours)

        /// <summary>Adds our ribbon colours after the game's, so saves, network sync and the ribbon model all work.</summary>
        public static void ExtendRibbons(EntityPropertiesManager epm)
        {
            try
            {
                var arr = epm._RibbonMaterials_k__BackingField;
                if (arr == null || arr.Length == 0) return;
                if (_nativeRibbonCount < 0) _nativeRibbonCount = Math.Min(arr.Length, 4);
                if (arr.Length >= 4 + NewRibbonColors.Length) return;
                Material template = null;
                for (int i = arr.Length - 1; i >= 0 && !template; i--) if (arr[i]) template = arr[i];
                if (!template) return;
                var list = new List<Material>();
                for (int i = 0; i < arr.Length; i++) list.Add(arr[i]);
                // the game indexes the array by colour: [0] is never shown (None) and holds the blue material, Red and
                // Yellow are 1 and 2, and Blue (3) has no entry because the game never rolls it. Give it the blue one.
                Material blue = null;
                for (int i = 0; i < arr.Length; i++) if (arr[i] && arr[i].name.Contains("Blue")) blue = arr[i];
                while (list.Count < 4) list.Add(blue ?? template);
                for (int i = 0; i < NewRibbonColors.Length; i++)
                {
                    var m = new Material(template) { name = "CME_Ribbon_" + RibbonNames[3 + i] };
                    if (m.HasProperty("_DiffuseColor")) m.SetColor("_DiffuseColor", NewRibbonColors[i]);
                    m.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    list.Add(m);
                }
                epm._RibbonMaterials_k__BackingField = new Il2CppReferenceArray<Material>(list.ToArray());
                Plugin.Log.Msg($"Ribbon colours: {arr.Length} native ({string.Join(", ", Enumerable.Range(0, arr.Length).Select(i => arr[i] ? arr[i].name : "-"))}), now {list.Count}");
            }
            catch (Exception e) { Plugin.Log.Warning("Ribbon colours: " + e.Message); }
        }

        static Material RibbonMaterial(int ribbon)
        {
            try
            {
                var arr = EntityPropertiesManager.Instance?._RibbonMaterials_k__BackingField;
                if (arr != null && ribbon + 1 < arr.Length) return arr[ribbon + 1];
            }
            catch { }
            return null;
        }

        /// <summary>Ordinary parcels keep rolling only the game's own colours.</summary>
        [HarmonyPatch(typeof(EntityPropertiesManager), nameof(EntityPropertiesManager.GetRandomRibbonMaterial))]
        static class EntityPropertiesManager_RandomRibbon
        {
            static void Postfix(EntityPropertiesManager __instance, ref ParcelRibbonColor __result, ref Material material)
            {
                try
                {
                    if (_nativeRibbonCount <= 0 || (int)__result < _nativeRibbonCount) return;
                    int i = Random.Range(0, _nativeRibbonCount);
                    __result = (ParcelRibbonColor)i;
                    material = __instance._RibbonMaterials_k__BackingField[i];
                }
                catch { }
            }
        }

        static void ShowRibbon(EntityParcel p, int ribbon)
        {
            var props = p.Properties;
            var go = props._RibbonGameObject_k__BackingField;
            if (!go) return;
            bool on = ribbon >= 0;
            if (go.activeSelf != on) go.SetActive(on);
            if (!on) return;
            var r = go.GetComponent<MeshRenderer>();
            var m = RibbonMaterial(ribbon);
            if (r && m && r.sharedMaterial != m) r.sharedMaterial = m;
        }

        // ---------------------------------------------------------------- paper

        static Material PaperMaterial(int paper, bool damaged)
        {
            var cache = damaged ? _paperDamagedMats : _paperMats;
            if (cache.TryGetValue(paper, out var m) && m) return m;
            var baseMat = ModAssets.PropMaterial("gift_paper_" + Papers[paper]);
            if (!damaged) return cache[paper] = baseMat;
            m = new Material(baseMat) { name = baseMat.name + "_damaged", hideFlags = HideFlags.DontUnloadUnusedAsset };
            if (m.HasProperty("_DiffuseColor")) m.SetColor("_DiffuseColor", new Color(0.62f, 0.58f, 0.55f));
            return cache[paper] = m;
        }

        /// <summary>The parcel's box model (not its ribbon or selection outline).</summary>
        static MeshRenderer BoxRenderer(EntityParcel p)
        {
            var visuals = p.transform.Find("Visuals");
            if (!visuals) return null;
            for (int i = 0; i < visuals.childCount; i++)
            {
                var c = visuals.GetChild(i);
                var n = c.name;
                if (!n.StartsWith("sm_Parcel") || n.EndsWith("_Ribbon") || n.EndsWith("_Outline")) continue;
                var r = c.GetComponent<MeshRenderer>();
                if (r) return r;
            }
            return null;
        }

        /// <summary>A box of the given size with box-projected UVs, so the paper's pattern has the same size on every gift.</summary>
        static Mesh PaperBox(Vector3 size)
        {
            var key = new Vector3Int(Mathf.RoundToInt(size.x * 200), Mathf.RoundToInt(size.y * 200), Mathf.RoundToInt(size.z * 200));
            if (_paperBoxes.TryGetValue(key, out var m) && m) return m;
            var src = Shapes.Cube();
            m = UnityEngine.Object.Instantiate(src);
            m.name = $"CME_PaperBox_{key.x}_{key.y}_{key.z}";
            var v = src.vertices;
            var n = src.normals;
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = Vector3.Scale(v[i], size);
                float ax = Mathf.Abs(n[i].x), ay = Mathf.Abs(n[i].y);
                var q = ay > 0.5f ? new Vector2(v[i].x, v[i].z) : ax > 0.5f ? new Vector2(v[i].z, v[i].y) : new Vector2(v[i].x, v[i].y);
                uv[i] = q / PaperTile;
            }
            m.vertices = v;
            m.uv = uv;
            m.uv2 = uv;
            m.RecalculateBounds();
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return _paperBoxes[key] = m;
        }

        /// <summary>
        /// Wraps the gift: a paper box the size of its collider replaces the cardboard model (the game's ribbon, stamps
        /// and name label sit just outside the box, so they stay on top of the paper). -1 unwraps.
        /// </summary>
        static void ApplyPaper(EntityParcel p, int paper)
        {
            var r = BoxRenderer(p);
            if (!r) return;
            var shell = r.transform.parent.Find("CME_GiftPaper");
            if (paper < 0)
            {
                if (shell) UnityEngine.Object.Destroy(shell.gameObject);
                if (!r.enabled) r.enabled = true;
                return;
            }
            var col = p.transform.Find("Colliders/MainCollider")?.GetComponent<BoxCollider>();
            if (!col) return;
            bool damaged = false;
            try { damaged = p.Properties.IsDamaged; } catch { }
            var mat = PaperMaterial(paper, damaged);
            if (!shell)
            {
                var go = Shapes.Make("CME_GiftPaper", PaperBox(col.size + new Vector3(0.004f, 0.004f, 0.004f)), mat, r.transform.parent,
                    r.transform.parent.InverseTransformPoint(col.transform.TransformPoint(col.center)), Quaternion.Inverse(r.transform.parent.rotation) * col.transform.rotation, Vector3.one);
                go.layer = r.gameObject.layer;
                shell = go.transform;
            }
            var sr = shell.GetComponent<MeshRenderer>();
            if (sr.sharedMaterial != mat) sr.sharedMaterial = mat;
            if (r.enabled) r.enabled = false;
            Bands(shell, col.size, RibbonOf(p));
        }

        /// <summary>
        /// A wrapped gift's ribbon: two bands crossing over the top and running down all four sides, in the ribbon's
        /// material (the game's own ribbon model is hidden then: it is made for the cardboard box, not the paper).
        /// </summary>
        static void Bands(Transform shell, Vector3 size, int ribbon)
        {
            var bands = shell.Find("CME_GiftRibbon");
            var mat = ribbon >= 0 ? RibbonMaterial(ribbon) : null;
            if (!mat)
            {
                if (bands) UnityEngine.Object.Destroy(bands.gameObject);
                return;
            }
            if (!bands)
            {
                bands = new GameObject("CME_GiftRibbon").transform;
                bands.SetParent(shell, false);
                bands.gameObject.layer = shell.gameObject.layer;
                float w = Mathf.Clamp(Mathf.Min(size.x, size.z) * 0.14f, 0.025f, 0.06f);
                var sz = size + new Vector3(0.012f, 0.012f, 0.012f);
                foreach (var scale in new[] { new Vector3(w, sz.y, sz.z), new Vector3(sz.x, sz.y, w) })
                {
                    var b = Shapes.Make("band", Shapes.Cube(), mat, bands, Vector3.zero, Quaternion.identity, scale);
                    b.layer = shell.gameObject.layer;
                }
            }
            foreach (var mr in bands.GetComponentsInChildren<MeshRenderer>())
                if (mr.sharedMaterial != mat) mr.sharedMaterial = mat;
        }

        // ---------------------------------------------------------------- gift tag

        static TMP_FontAsset Font()
        {
            if (_font) return _font;
            foreach (var t in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
                if (t && t.name == "text_CustomerName" && t.font) { _font = t.font; break; }
            if (!_font)
                foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                    if (t && t.font) { _font = t.font; break; }
            return _font;
        }

        /// <summary>A card on the gift's top face with the sender's two riddles written on it.</summary>
        static void EnsureTag(EntityParcel p)
        {
            var col = p.transform.Find("Colliders/MainCollider")?.GetComponent<BoxCollider>();
            if (!col || col.transform.Find("CME_GiftTag")) return;
            var w = WishOf(p);
            var s = col.size;
            float cw = Mathf.Clamp(Mathf.Min(s.x, s.z) * 0.9f, 0.12f, 0.3f);
            float ch = cw * 0.625f;
            var root = new GameObject("CME_GiftTag").transform;
            root.SetParent(col.transform, false);
            // flat on the top face, pushed into a corner off the ribbon cross, lying on top of the bands
            float ox = Mathf.Max(0, s.x / 2 - cw / 2 - 0.015f), oz = Mathf.Max(0, s.z / 2 - ch / 2 - 0.015f);
            root.localPosition = col.center + new Vector3(-ox, s.y / 2 + 0.012f, -oz);
            root.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _tagMat ??= ModAssets.PropMaterial("gift_tag");
            if (_tagMat.HasProperty("_AlphaClip")) { _tagMat.SetFloat("_AlphaClip", 1f); _tagMat.EnableKeyword("_ALPHATEST_ON"); }
            var card = Shapes.Make("card", Shapes.Quad(), _tagMat, root, Vector3.zero, Quaternion.identity, new Vector3(cw, ch, 1f));
            card.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var font = Font();
            if (!font) return;
            var tgo = new GameObject("text");
            tgo.transform.SetParent(root, false);
            tgo.transform.localPosition = new Vector3(0.006f * cw / 0.25f, 0, -0.002f);
            var tmp = tgo.AddComponent<TextMeshPro>();
            tmp.font = font;
            tmp.text = Localization.Text(PaperAskKey(w)) + "\n" + Localization.Text(RibbonAskKey(w));
            tmp.color = new Color(0.22f, 0.18f, 0.24f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.05f;
            tmp.fontSizeMax = 2f;
            tmp.rectTransform.sizeDelta = new Vector2(cw * 0.76f, ch * 0.72f);
            var mr = tgo.GetComponent<MeshRenderer>();
            if (mr) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- per parcel

        /// <summary>Every logic tick for each gift: tag, paper and ribbon.</summary>
        public static void ItemLogic(EntityParcel p, ModKind kind)
        {
            if (p == null || p.Properties == null) return;
            EnsureTag(p);
            int paper = ParcelKinds.PaperOf(p.Properties.StorageConstraint);
            ApplyPaper(p, paper);
            // once wrapped, the bands on the paper are the ribbon
            ShowRibbon(p, paper >= 0 ? -1 : RibbonOf(p));
        }

        public static void ReleaseItem(EntityParcel p)
        {
            if (p == null) return;
            var tag = p.transform.Find("Colliders/MainCollider/CME_GiftTag");
            if (tag) UnityEngine.Object.Destroy(tag.gameObject);
            ApplyPaper(p, -1);
        }

        /// <summary>Catazon return items from an earlier build become ordinary parcels again.</summary>
        public static void MigrateReturn(EntityParcel p)
        {
            if (p == null) return;
            try
            {
                var col = p.transform.Find("Colliders/MainCollider");
                var art = col ? col.Find("CME_ReturnArt") : null;
                if (art) UnityEngine.Object.Destroy(art.gameObject);
                var props = p.Properties;
                if (props.PropertiesCanvas) props.PropertiesCanvas.gameObject.SetActive(true);
                if (IsServer) props.SetStorageConstraint(StorageConstraint.None);
            }
            catch (Exception e) { Plugin.Log.Warning("Return migration: " + e.Message); }
        }

        /// <summary>Turns a freshly deposited customer parcel into an unwrapped gift.</summary>
        static void MakeGift(EntityParcel p)
        {
            var props = p.Properties;
            props.SetStorageConstraint(ParcelKinds.Gift);
            props._RibbonColor_k__BackingField = ParcelRibbonColor.None;
            ShowRibbon(p, -1);
            ParcelTracker.Register(p);
        }

        // ---------------------------------------------------------------- build

        public static void Build()
        {
            if (_root) return;
            try { BuildInternal(); }
            catch (Exception e) { Plugin.Log.Error("Gift room build failed: " + e); }
        }

        static void BuildInternal()
        {
            _root = World.Root("CME_GiftRoom");
            var t = _root.transform;
            int id = StoreIdBase;

            // the wrapping bench (a 4x8 grid on its top) and its pegboard
            World.Mesh("gift_bench", "tex_gift_bench", t, BenchPos, 0).name = "CME_GiftBench";
            _bench = StoreFactory.Create("p_Entity_Storage_4x8", t, BenchPos + new Vector3(0, BenchTop, 0), 0f, id++);
            if (_bench) _bench.name = "CME_GiftBenchStore";
            id++;   // 701 was the old wrapping table: the shelves keep their ids
            World.Box(t, "bench", BenchPos + new Vector3(0, BenchTop / 2 - 0.02f, 0), new Vector3(BenchW, BenchTop - 0.04f, BenchL), 0);
            World.Box(t, "pegboard", BenchPos + new Vector3(0.52f, 1.6f, 0), new Vector3(0.06f, 1.5f, BenchL + 0.2f), 0);
            BuildSelectors(t);

            // shelves along the north wall for gifts waiting to be picked up
            var shelfM = World.Find("p_Shelves_M", "PostOffice_Environment") ?? World.Find("p_Shelves_M");
            var tagM = World.Find("prop_StorageTag_Left (1)", "p_DarkRoom") ?? World.Find("prop_StorageTag_Left", "p_DarkRoom");
            // the mirrored tag for a unit's right side: a left tag turned round would show its back; a right tag
            // at the left tag's yaw + 180 sticks out to the front of the unit just as the left one does
            var tagR = World.Find("prop_StorageTag_Right (1)", "p_DarkRoom") ?? World.Find("prop_StorageTag_Right") ?? tagM;
            int label = LabelIdBase;
            foreach (var x in new[] { 8.8f, WallE - 0.91f })
            {
                var pos = new Vector3(x, PileWing.FloorY, WallN - 0.36f);
                var unit = Yard.BuildShelf(shelfM, t, pos, 0f, ref id, keepLightmap: true);
                if (!unit) { _log.Add("no shelf template"); continue; }
                unit.name = "CME_GiftShelf";
                // the sign on the side facing the gap between the two shelves (the outer sides are against walls)
                bool west = x > 10f;
                Planetarium.CloneLabel(west ? tagM : tagR, t, pos + new Vector3(west ? -0.79f : 0.79f, 1.60f, -0.22f), west ? 90f : 270f, label++);
            }

            // a cubby unit against the south wall for small gifts (its own store ids, after everything above)
            var cubby = World.Find("p_Shelves_S", "p_BigRoom");
            if (cubby)
            {
                int cid = StoreIdBase + 60;
                var cpos = new Vector3(WallE - 1.25f, PileWing.FloorY, WallS + 0.42f);
                var unit = Yard.BuildShelf(cubby, t, cpos, 180f, ref cid, keepLightmap: true, nestedStores: true);
                if (unit)
                {
                    unit.name = "CME_GiftCubby";
                    // on its west side (the east side is against the wall)
                    Planetarium.CloneLabel(tagR, t, cpos + Quaternion.Euler(0, 180f, 0) * new Vector3(1.22f, 1.75f, -0.3f), 90f, label++);
                }
            }
            else _log.Add("no cubby template");

            // a floor pallet against the west wall for big boxes (after the shelves: their ids stay as saved)
            var pallet = World.Find("p_Floor_Storage_XL", "PostOffice_Environment") ?? World.Find("p_Floor_Storage_Dark_XL", "p_DarkRoom");
            var pu = Yard.BuildShelf(pallet, t, new Vector3(WallW + 0.75f, PileWing.FloorY, PalletZ), 90f, ref id, keepLightmap: true);
            if (pu)
            {
                pu.name = "CME_GiftPallet";
                Planetarium.PalletLabel(pu, t, Vector3.right, label++);
            }
            else _log.Add("no pallet template");

            // sign over the pegboard, how-to poster, corridor plaque, decor
            Flat(t, "CME_GiftSign", "gift_sign", new Vector3(WallE - 0.015f, 2.78f, BenchPos.z), 90f, new Vector2(1.5f, 0.66f));
            Flat(t, "CME_GiftPoster", "gift_poster", new Vector3(WallW + 0.06f, 1.7f, PalletZ), 270f, new Vector2(0.75f, 1.0f));
            Flat(t, "CME_GiftPlaque", "gift_logo", new Vector3(PileWing.CX1 - 0.012f, 1.85f, 29.95f), 90f, new Vector2(0.5f, 0.5f));
            World.Mesh("gift_decor", "tex_gift_decor", t, Vector3.zero, 0).name = "CME_GiftDecor";
            World.Box(t, "display", new Vector3(8.35f, 0.4f, 26.5f), new Vector3(0.95f, 0.8f, 0.95f), 0);
            World.Box(t, "bin", new Vector3(11.6f, 0.25f, 30.95f), new Vector3(0.36f, 0.5f, 0.36f), 0);
            // the game's own potted plant, clear of the door; it only blocks at the pot, not across its leaves
            var plant = World.Find("p_Plant", "p_CustomerRoom");
            var pv = plant ? World.CopyVisual(plant, t, new Vector3(WallW + 0.5f, PileWing.FloorY, 29.5f), 30f, collider: false) : null;
            if (pv)
            {
                pv.name = "CME_GiftPlant";
                World.Box(t, "plant", pv.transform.position + new Vector3(0, 0.3f, 0), new Vector3(0.4f, 0.6f, 0.4f), 0);
            }
            // a big rug over the middle of the floor
            var rug = Shapes.Make("CME_GiftRug", Shapes.Quad(), ModAssets.PropMaterial("gift_rug"), t, new Vector3(RugX, PileWing.FloorY + 0.005f, RugZ),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(RugW, RugL, 1f));
            rug.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            BuildDisplayGifts(t);

            Plugin.Log.Msg("Gift room built" + (_log.Count > 0 ? ": " + string.Join("; ", _log) : ""));
        }

        static void Flat(Transform t, string name, string tex, Vector3 pos, float yaw, Vector2 size)
        {
            var go = Shapes.Make(name, Shapes.Quad(), ModAssets.PropMaterial(tex), t, pos, Quaternion.Euler(0, yaw, 0), new Vector3(size.x, size.y, 1f));
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>The paper rolls on their brackets and the ribbon spools on the dowel: clickable, in the real colours.</summary>
        static void BuildSelectors(Transform t)
        {
            int paper = 0;
            foreach (var y in RollY)
                foreach (var z in RollZ)
                {
                    if (paper >= Papers.Length) break;
                    var pos = BenchPos + new Vector3(RollX, y, z);
                    var roll = Shapes.Make("CME_PaperRoll_" + Papers[paper], Shapes.Rod(20), PaperMaterial(paper, false), t, pos,
                        Quaternion.Euler(90f, 0f, 0f), new Vector3(0.062f, 0.42f, 0.062f));
                    var core = Shapes.Make("core", Shapes.Rod(12), ModAssets.PropMaterial("tex_gift_bench"), roll.transform, Vector3.zero, Quaternion.identity,
                        new Vector3(0.45f, 1.04f, 0.45f));
                    core.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    // a loose end of paper hanging off the front of the roll
                    var tail = Shapes.Make("CME_PaperTail", PaperQuad(0.38f, 0.15f), PaperMaterial(paper, false), t, pos + new Vector3(-0.064f, -0.075f, 0),
                        Quaternion.Euler(0, 90f, 0), Vector3.one);
                    tail.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    AddSelector(t, roll.transform, false, paper, pos, new Vector3(0.16f, 0.16f, 0.44f));
                    paper++;
                }
            for (int r = 0; r < RibbonCount; r++)
            {
                float z = -0.8f + r * 0.32f;
                var pos = BenchPos + new Vector3(DowelX, DowelY, z);
                var mat = RibbonMaterial(r) ?? ModAssets.PropMaterial("tex_gift_bench");
                var spool = Shapes.Make("CME_RibbonSpool_" + RibbonNames[r], Shapes.Rod(20), mat, t, pos, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.075f, 0.06f, 0.075f));
                // the spool's wooden flanges
                foreach (var dy in new[] { -0.55f, 0.55f })
                    Shapes.Make("flange", Shapes.Rod(20), ModAssets.PropMaterial("tex_gift_bench"), spool.transform, new Vector3(0, dy, 0), Quaternion.identity, new Vector3(1.18f, 0.12f, 1.18f));
                // a ribbon end hanging down
                var tail = Shapes.Make("tail", Shapes.Quad(), mat, t, pos + new Vector3(-0.07f, -0.12f, 0), Quaternion.Euler(0, 90f, 0), new Vector3(0.035f, 0.22f, 1f));
                tail.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                AddSelector(t, spool.transform, true, r, pos + new Vector3(0, -0.06f, 0), new Vector3(0.2f, 0.32f, 0.2f));
            }
        }

        static void AddSelector(Transform t, Transform visual, bool ribbon, int index, Vector3 pos, Vector3 size)
        {
            // a trigger: the player walks through it, our click ray finds it
            var go = new GameObject(ribbon ? "CME_RibbonPick" : "CME_PaperPick");
            go.transform.SetParent(t, false);
            go.transform.position = pos;
            go.layer = 2;   // Ignore Raycast: the game's own interaction never sees it
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = true;
            var s = new Selector { IsRibbon = ribbon, Index = index, Visual = visual, RestScale = visual.localScale };
            _selectors[bc.Pointer] = s;
            _selectorList.Add(s);
        }

        /// <summary>A few wrapped presents on the display table, one in each paper.</summary>
        static void BuildDisplayGifts(Transform t)
        {
            var spots = new (Vector3 pos, Vector3 size, float yaw, int paper, int ribbon)[]
            {
                (new Vector3(8.2f, 0.79f, 26.35f), new Vector3(0.3f, 0.22f, 0.26f), 12f, 0, 1),
                (new Vector3(8.55f, 0.79f, 26.45f), new Vector3(0.22f, 0.3f, 0.22f), -20f, 4, 0),
                (new Vector3(8.33f, 0.79f, 26.72f), new Vector3(0.26f, 0.16f, 0.2f), 40f, 2, 4),
                (new Vector3(8.22f, 1.01f, 26.36f), new Vector3(0.16f, 0.14f, 0.16f), 30f, 7, 5),
            };
            foreach (var s in spots)
            {
                var box = Shapes.Make("CME_DisplayGift", WrappedCube(), PaperMaterial(s.paper, false), t, s.pos + new Vector3(0, s.size.y / 2, 0),
                    Quaternion.Euler(0, s.yaw, 0), s.size);
                var rm = RibbonMaterial(s.ribbon) ?? ModAssets.PropMaterial("tex_gift_bench");
                var cube = Shapes.Cube();
                Shapes.Make("band", cube, rm, box.transform, Vector3.zero, Quaternion.identity, new Vector3(0.16f, 1.01f, 1.01f));
                Shapes.Make("band", cube, rm, box.transform, Vector3.zero, Quaternion.identity, new Vector3(1.01f, 1.01f, 0.16f));
            }
        }

        /// <summary>A quad of the given size whose UVs keep the paper's pattern at its real size.</summary>
        static Mesh PaperQuad(float w, float h)
        {
            var m = UnityEngine.Object.Instantiate(Shapes.Quad());
            var v = m.vertices;
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = new Vector3(v[i].x * w, v[i].y * h, 0);
                uv[i] = new Vector2(v[i].x, v[i].y) / PaperTile;
            }
            m.vertices = v;
            m.uv = uv;
            m.RecalculateBounds();
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return m;
        }

        static Mesh _wrappedCube;
        static Mesh WrappedCube()
        {
            if (_wrappedCube) return _wrappedCube;
            var src = Shapes.Cube();
            var m = UnityEngine.Object.Instantiate(src);
            var v = m.vertices; var n = m.normals; var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                float ax = Mathf.Abs(n[i].x), ay = Mathf.Abs(n[i].y);
                uv[i] = (ay > 0.5f ? new Vector2(v[i].x, v[i].z) : ax > 0.5f ? new Vector2(v[i].z, v[i].y) : new Vector2(v[i].x, v[i].y)) * 0.6f;
            }
            m.uv = uv;
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return _wrappedCube = m;
        }

        public static void OnUnlocked(bool fromSave)
        {
            _unlocked = true;
            // the room opens mid-shift: its first customers come straight away
            if (!fromSave) StartShift();
            else if (GameTimeManager.Instance != null && (GameTimeManager.Instance.CurrentTimePeriod == GameTimePeriod.Day
                     || GameTimeManager.Instance.CurrentTimePeriod == GameTimePeriod.Night))
                StartShift();
        }

        // ---------------------------------------------------------------- clicking rolls and spools

        static Selector RaySelector(out bool blocked)
        {
            blocked = false;
            var cam = Cameras.View;
            if (!cam || _selectors.Count == 0 || !Contains(cam.transform.position)) return null;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            var hits = Physics.RaycastAll(ray, ClickRange, ~0, QueryTriggerInteraction.Collide);
            Selector best = null;
            float bd = float.MaxValue;
            foreach (var h in hits)
                if (h.collider && _selectors.TryGetValue(h.collider.Pointer, out var s) && h.distance < bd) { best = s; bd = h.distance; }
            if (best == null) return null;
            // walls in the way block the click; parcels (on the bench or carried) and our own props don't
            foreach (var h in hits)
            {
                if (h.distance >= bd || !h.collider || h.collider.isTrigger) continue;
                if (h.collider.GetComponentInParent<Entity>() != null) continue;
                if (h.collider.transform.IsChildOf(_root.transform)) continue;
                blocked = true;
                return null;
            }
            return best;
        }

        static EntityParcel HeldGift()
        {
            var local = PlayerManager.Instance?.LocalPlayerEntity;
            if (local == null) return null;
            foreach (var p in ParcelTracker.Gifts())
            {
                try
                {
                    var pick = p.Interactable?.Pickable;
                    var carrier = pick?.CarryingEntity;
                    if (carrier != null && carrier.Pointer == local.Pointer) return p;
                }
                catch { }
            }
            return null;
        }

        static bool HoldingAnything()
        {
            var local = PlayerManager.Instance?.LocalPlayerEntity;
            if (local == null) return false;
            try
            {
                foreach (var p in UnityEngine.Object.FindObjectsByType<EntityParcel>(FindObjectsSortMode.None))
                {
                    var carrier = p.Interactable?.Pickable?.CarryingEntity;
                    if (carrier != null && carrier.Pointer == local.Pointer) return true;
                }
            }
            catch { }
            return false;
        }

        static bool BenchHasAnything()
        {
            try { return _bench && _bench.HasAnyChild; } catch { return false; }
        }

        static List<EntityParcel> GiftsOnBench()
        {
            var r = new List<EntityParcel>();
            if (!_bench) return r;
            try
            {
                var l = _bench.GetAllChildren();
                for (int i = 0; l != null && i < l.Count; i++)
                {
                    var p = l[i]?.TryCast<EntityParcel>();
                    if (p != null && p.Properties != null && ParcelKinds.IsGift(p.Properties.StorageConstraint)) r.Add(p);
                }
            }
            catch { }
            return r;
        }

        /// <summary>Called when the local player presses interact. True when the press was used on a roll or spool.</summary>
        public static bool TryClick()
        {
            if (!_root || !_unlocked) return false;
            var s = RaySelector(out _);
            if (s == null) return false;
            var held = HeldGift();
            var targets = held != null ? new List<EntityParcel> { held } : GiftsOnBench();
            s.Pop = 1f;
            if (targets.Count == 0)
            {
                // an ordinary parcel in hand or on the bench can't be wrapped: only gifts (with a gift tag) can
                bool parcelThere = HoldingAnything() || BenchHasAnything();
                try { InterfaceManager.Instance?.NotificationInterface?.ShowNotification(Localization.Text(parcelThere ? "CME/Gift_NotGift" : "CME/Gift_NoGift")); } catch { }
                SoundFx.Play(s.Visual.position, 0.6f, "Wrong", "Error", "Parcel_Place");
                return true;
            }
            foreach (var p in targets)
            {
                if (s.IsRibbon) p.Properties._RibbonColor_k__BackingField = (ParcelRibbonColor)(s.Index + 1);
                else p.Properties.SetStorageConstraint(ParcelKinds.GiftPaper(s.Index));
                ItemLogic(p, ParcelKinds.KindOf(p.Properties.StorageConstraint));
            }
            SoundFx.Play(targets[0].transform.position, 1f, s.IsRibbon ? "Stamp_Apply" : "Tape", "Parcel_Place");
            return true;
        }

        [HarmonyPatch(typeof(PlayerInteraction), nameof(PlayerInteraction.InputManager_OnInteractPerformed))]
        static class PlayerInteraction_Interact
        {
            static bool Prefix()
            {
                try { return !TryClick(); }
                catch (Exception e) { Plugin.Log.Warning("Gift click: " + e.Message); return true; }
            }
        }

        // ---------------------------------------------------------------- update

        public static void Update()
        {
            if (!_root) return;
            float dt = Time.deltaTime;
            if (Time.time >= _nextHover)
            {
                _nextHover = Time.time + 0.08f;
                _hovered = _unlocked ? RaySelector(out _) : null;
            }
            foreach (var s in _selectorList)
            {
                if (!s.Visual) continue;
                s.Pop = Mathf.MoveTowards(s.Pop, 0f, dt * 4f);
                float k = (s == _hovered ? 1.12f : 1f) + Mathf.Sin(s.Pop * Mathf.PI) * 0.15f;
                var target = s.RestScale * k;
                if ((s.Visual.localScale - target).sqrMagnitude > 1e-8f)
                    s.Visual.localScale = Vector3.Lerp(s.Visual.localScale, target, Mathf.Min(1f, dt * 14f));
            }
        }

        // ---------------------------------------------------------------- shifts

        static void StartShift()
        {
            if (!_unlocked || !IsServer) return;
            int level = ProgressionManager.Instance != null ? ProgressionManager.Instance.CurrentLevel : 10;
            _pendingGifts = 2 + (level >= 14 ? 1 : 0) + Random.Range(0, 2);
            Plugin.Log.Msg($"Gifts: {_pendingGifts} gift customers this shift");
        }

        static void OnPeriodChanged(GameTimePeriod period)
        {
            if (period == GameTimePeriod.Day || period == GameTimePeriod.Night) { RecapRow.OnShiftStart(); StartShift(); }
            else if (period == GameTimePeriod.EveningRecap || period == GameTimePeriod.DawnRecap) _pendingGifts = 0;
        }

        public static int GiftValue
        {
            get
            {
                // a gift wrapped just right is worth about as much again as handing over an ordinary parcel
                try { var rm = RecapManager.Instance; if (rm != null && rm.ParcelBaseValue > 0) return rm.ParcelBaseValue * 4; } catch { }
                return 20;
            }
        }

        /// <summary>Scores a gift handed to the person it is for. Returns the reaction tier.</summary>
        public static string Evaluate(EntityParcel p, out int score)
        {
            var w = WishOf(p);
            int paper = ParcelKinds.PaperOf(p.Properties.StorageConstraint);
            int ribbon = RibbonOf(p);
            int v = GiftValue;
            if (paper < 0) { score = -2 * v; return "Unwrapped"; }
            bool pOk = paper == w.Paper, rOk = ribbon == w.Ribbon;
            score = pOk && rOk ? 4 * v : pOk ? 2 * v : rOk ? v : 0;
            return pOk && rOk ? "Perfect" : pOk || rOk ? "Good" : "Wrong";
        }

        public static string Describe()
        {
            var sb = new System.Text.StringBuilder($"gifts built={Built} unlocked={_unlocked} pending={_pendingGifts} bench={(_bench ? _bench.name : "-")} " +
                $"selectors={_selectorList.Count} shift={_shiftCount}/{_shiftScore} last={LastCount}/{LastScore} ribbonsNative={_nativeRibbonCount} [{string.Join("; ", _log)}]\n");
            foreach (var p in ParcelTracker.Gifts())
            {
                var w = WishOf(p);
                Evaluate(p, out int sc);
                var br = BoxRenderer(p);
                sb.AppendLine($"  {p.name} at {p.transform.position} wants {Papers[w.Paper]}/{RibbonNames[w.Ribbon]} has paper={ParcelKinds.PaperOf(p.Properties.StorageConstraint)} ribbon={RibbonOf(p)} -> {sc} " +
                              $"mat={(br ? br.sharedMaterial?.name : "-")} mesh={(br ? br.GetComponent<MeshFilter>()?.sharedMesh?.name : "-")}");
            }
            return sb.ToString();
        }

        /// <summary>Dev: the next customer brings a gift.</summary>
        public static void ForceNext()
        {
            _pendingGifts = Math.Max(1, _pendingGifts);
            _forceNext = true;
        }

        /// <summary>Dev: makes a parcel a gift (optionally wrapped).</summary>
        public static void DevMakeGift(EntityParcel p, int paper, int ribbon)
        {
            MakeGift(p);
            if (paper >= 0) p.Properties.SetStorageConstraint(ParcelKinds.GiftPaper(paper));
            if (ribbon >= 0) p.Properties._RibbonColor_k__BackingField = (ParcelRibbonColor)(ribbon + 1);
            ItemLogic(p, ParcelKinds.KindOf(p.Properties.StorageConstraint));
        }

        // ---------------------------------------------------------------- patches: customers

        /// <summary>When a gift customer is due, the bell brings one (a depositing customer) without touching the
        /// shift's queue of normal customers.</summary>
        [HarmonyPatch(typeof(CustomerManager), nameof(CustomerManager.DequeueCustomerToServe))]
        static class CustomerManager_Dequeue
        {
            static bool Prefix(CustomerManager __instance, ref CustomerType __result)
            {
                try
                {
                    if (!_unlocked || _pendingGifts <= 0 || !IsServer) return true;
                    int remaining = Math.Max(0, __instance.CurrentTotalCustomersToSatisfy - __instance.CurrentSatisfiedCustomerCount);
                    float chance = remaining <= 2 ? 1f : Mathf.Clamp01(_pendingGifts / (float)(remaining + 1) * 1.4f);
                    if (!_forceNext && Random.value > chance) return true;
                    // a gift is one parcel, not one of the game's stacks of several
                    var list = new List<EntityInteractableStore>();
                    var all = __instance.DepositableParcels;
                    for (int i = 0; all != null && i < all.Count; i++)
                        if (all[i] && !all[i].name.Contains("&") && !all[i].name.Contains("_2")) list.Add(all[i]);
                    if (list.Count == 0) return true;
                    var prefab = list[Random.Range(0, list.Count)];
                    if (!prefab) return true;
                    _forceNext = false;
                    __instance.SetNextParcelToDeposit(prefab);
                    _nextIsGift = true;
                    _pendingGifts--;
                    __result = CustomerType.Deposit;
                    return false;
                }
                catch (Exception e) { Plugin.Log.Warning("Gift customer: " + e.Message); return true; }
            }
        }

        [HarmonyPatch(typeof(CustomerCounter), nameof(CustomerCounter.SpawnedStore_OnCompleteHierarchySpawned))]
        static class CustomerCounter_DepositSpawned
        {
            static void Prefix(out bool __state)
            {
                __state = _nextIsGift;
                _nextIsGift = false;
            }

            static void Postfix(EntityInteractableStore store, bool __state)
            {
                if (!__state) return;
                try
                {
                    var p = store?.GetFirstEntity()?.TryCast<EntityParcel>();
                    if (p != null) MakeGift(p);
                }
                catch (Exception e) { Plugin.Log.Warning("Gift deposit: " + e.Message); }
            }
        }

        /// <summary>Gift customers are extra: handing over their gift doesn't count towards the shift. Handing a gift
        /// to the person it is for scores its wrapping.</summary>
        [HarmonyPatch(typeof(CustomerCounter), nameof(CustomerCounter.ValidateRequest))]
        static class CustomerCounter_Validate
        {
            static void Prefix(CustomerCounter __instance)
            {
                try
                {
                    var dep = __instance._currentDepositingParcel;
                    if (dep != null)
                    {
                        if (dep.Properties != null && ParcelKinds.IsGift(dep.Properties.StorageConstraint)) _skipSatisfied++;
                        return;
                    }
                    var req = __instance._CurrentRequest_k__BackingField;
                    if (req == null || req.Properties == null || !ParcelKinds.IsGift(req.Properties.StorageConstraint)) return;
                    if (!_scored.Add(req.Pointer)) return;
                    var tier = Evaluate(req, out int score);
                    _shiftCount++;
                    _shiftScore += score;
                    _reactCustomer = __instance._currentCustomer != null ? __instance._currentCustomer.Pointer : IntPtr.Zero;
                    _reactTier = tier;
                    Plugin.Log.Msg($"Gift handed over: {tier} {score:+0;-0} (shift {_shiftCount} gifts, {_shiftScore} points)");
                }
                catch (Exception e) { Plugin.Log.Warning("Gift validate: " + e.Message); }
            }
        }

        [HarmonyPatch(typeof(CustomerManager), nameof(CustomerManager.OnCustomerSatisfied))]
        static class CustomerManager_Satisfied
        {
            static bool Prefix()
            {
                if (_skipSatisfied <= 0) return true;
                _skipSatisfied--;
                return false;
            }
        }

        /// <summary>The goodbye of someone just handed their gift reacts to the wrapping.</summary>
        public static string ReactionKeyFor(EntityCustomer c)
        {
            if (c == null || _reactCustomer == IntPtr.Zero || c.Pointer != _reactCustomer) return null;
            _reactCustomer = IntPtr.Zero;
            return $"CME/GiftReact_{_reactTier}_{Random.Range(0, Localization.GiftReactionsPerTier)}";
        }

        // ---------------------------------------------------------------- patches: shifts and score

        [HarmonyPatch(typeof(GameTimeManager), nameof(GameTimeManager.SetCurrentTimePeriod))]
        static class GameTimeManager_SetPeriod
        {
            static void Postfix(GameTimePeriod new_time_period)
            {
                try { OnPeriodChanged(new_time_period); }
                catch (Exception e) { Plugin.Log.Warning("Gift shift: " + e.Message); }
            }
        }

        /// <summary>
        /// The host sends the shift's numbers to everyone right after working them out; adding the gifts' score here
        /// puts it in the total the recap shows and the level progress the game adds it to.
        /// </summary>
        [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.SendRecapSynchronization))]
        static class NetworkManager_SendRecap
        {
            static void Prefix(ref int current_shift_total_score)
            {
                try
                {
                    LastCount = _shiftCount;
                    LastScore = _shiftScore;
                    _shiftCount = _shiftScore = 0;
                    if (LastCount == 0) return;
                    current_shift_total_score += LastScore;
                    var rm = RecapManager.Instance;
                    if (rm != null) rm._CurrentShiftTotalScore_k__BackingField += LastScore;
                    RecapRow.Pending = true;
                }
                catch (Exception e) { Plugin.Log.Warning("Gift score: " + e.Message); }
            }
        }
    }
}
