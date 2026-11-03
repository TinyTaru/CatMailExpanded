using System;
using System.Collections.Generic;
using System.Globalization;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The mirror room: a plum-and-gold wing west of the extended pile corridor, just north of the planetarium,
    /// behind a door further down the corridor than the returns room's. Two big gilded mirrors are real mirrors:
    /// each is drawn by a camera standing at the viewer's reflection behind the glass, its frustum fitted to the
    /// glass (so the picture lands exactly on the mirror's quad), and they render only while someone can see them.
    /// Spectral parcels (Spectral) are solid only in here; the mirrors show them wherever they are.
    /// </summary>
    internal static class MirrorRoom
    {
        // interior (keep in sync with Art/Blender/mirror.py)
        public const float IX0 = -1.6f, IX1 = 4.25f, IZ0 = 27.35f, IZ1 = 36.4f, FY = 0.04f, WH = 3.8f;
        public const float OX0 = IX0 - 0.2f, OZ0 = IZ0 - 0.2f, OZ1 = IZ1 + 0.2f;
        const float EX = PileWing.CX0;
        const float OpenZ0 = PileWing.MOpenZ0, OpenZ1 = PileWing.MOpenZ1, OpenTop = PileWing.MOpenTop;
        static readonly Vector3 Centre = new((IX0 + IX1) / 2, 0f, (IZ0 + IZ1) / 2);
        // the door models are authored at the storage corridor's service door (hinge (-2.25, 0, 1.02))
        static readonly Vector3 Hinge = new(EX, 0f, OpenZ0);
        static readonly Vector3 DoorModelOffset = Hinge - new Vector3(-2.25f, 0f, 1.02f);
        const int StoreIdBase = 500, LabelIdBase = 600;

        sealed class Mirror
        {
            public string Name;
            public Vector3 Centre, Normal, Right, Up;   // Right/Up as seen by someone facing the mirror
            public float W, H;
            public Camera Cam;
            public RenderTexture Texture;
            public MeshRenderer Glass;
        }

        static GameObject _root;
        static Transform _doorPivot;
        static readonly List<Mirror> _mirrors = new();
        static readonly List<MeshRenderer> _candles = new();
        static Material _candleMat;
        static bool _open;
        static float _lastNight = -1f;
        static readonly List<string> _log = new();
        // the local player sees a first-person body without a head; mirrors show a full copy of the body (and eyes)
        // instead, drawn only while a mirror camera renders
        static readonly List<SkinnedMeshRenderer> _reflection = new();
        static SkinnedMeshRenderer _firstPersonBody;
        static float _nextBodyCheck;
        static bool _renderHooked;

        public static bool Built => _root;

        public static void Reset()
        {
            foreach (var m in _mirrors) if (m.Texture) m.Texture.Release();
            _root = null;
            _doorPivot = null;
            _mirrors.Clear();
            _candles.Clear();
            _candleMat = null;
            _open = false;
            _lastNight = -1f;
            _log.Clear();
            foreach (var r in _reflection) if (r) UnityEngine.Object.Destroy(r.gameObject);
            _reflection.Clear();
            _firstPersonBody = null;
            _nextBodyCheck = 0f;
        }

        public static bool Contains(Vector3 p) => p.x > IX0 - 0.05f && p.x < EX && p.z > IZ0 - 0.05f && p.z < IZ1 + 0.05f && p.y > -0.5f && p.y < WH + 0.3f;

        public static void Build()
        {
            if (_root) return;
            try { BuildInternal(); }
            catch (Exception e) { Plugin.Log.Error("Mirror room build failed: " + e); }
        }

        static void BuildInternal()
        {
            _root = World.Root("CME_MirrorRoom");
            var t = _root.transform;
            var room = World.Mesh("mirror_room", "tex_mirror_room", t, Vector3.zero, 0);
            room.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            BuildDoor(t);
            BuildColliders(t);
            BuildShelves(t);
            BuildCandles(t);
            BuildMirrors(t);
            Plugin.Log.Msg("Mirror room built" + (_log.Count > 0 ? ": " + string.Join("; ", _log) : ""));
        }

        // ------------------------------------------------------------------ door

        static void BuildDoor(Transform t)
        {
            World.Mesh("door_frame", "tex_door_frame", t, DoorModelOffset, 0).name = "CME_MirrorDoorFrame";
            var pivot = new GameObject("CME_MirrorDoorPivot");
            pivot.transform.SetParent(t, false);
            pivot.transform.position = Hinge;
            World.Mesh("door_leaf", "tex_door_leaf", pivot.transform, Hinge, 0);   // the leaf is authored around its hinge
            var leaf = World.Box(pivot.transform, "leaf_collider", Hinge + new Vector3(0, 1.37f, 0.83f), new Vector3(0.08f, 2.7f, 1.66f), 0);
            leaf.transform.SetParent(pivot.transform, true);
            _doorPivot = pivot.transform;
        }

        public static void SetOpen(bool open, bool animate)
        {
            if (!_root) return;
            if (_open == open && !animate) return;
            _open = open;
            if (_doorPivot) _doorPivot.gameObject.SetActive(!open);
            if (animate && open) SoundFx.Play(Hinge + new Vector3(0, 1.2f, 0.8f), 1f, "Unlockable_Door_Activation", "Boat_Door");
        }

        // ------------------------------------------------------------------ colliders

        static void BuildColliders(Transform t)
        {
            const int wall = 9;
            float top = WH + 0.4f;
            World.Box(t, "floor", new Vector3((OX0 + EX) / 2, FY - 0.15f, (OZ0 + OZ1) / 2), new Vector3(EX - OX0, 0.3f, OZ1 - OZ0), wall);
            World.Box(t, "ceiling", new Vector3((OX0 + EX) / 2, WH + 0.15f, (OZ0 + OZ1) / 2), new Vector3(EX - OX0, 0.3f, OZ1 - OZ0), wall);
            World.Box(t, "wall_w", new Vector3(OX0 + 0.1f, top / 2, (OZ0 + OZ1) / 2), new Vector3(0.2f, top, OZ1 - OZ0), wall);
            World.Box(t, "wall_s", new Vector3((OX0 + EX) / 2, top / 2, OZ0 + 0.1f), new Vector3(EX - OX0, top, 0.2f), wall);
            World.Box(t, "wall_n", new Vector3((OX0 + EX) / 2, top / 2, OZ1 - 0.1f), new Vector3(EX - OX0, top, 0.2f), wall);
            World.Box(t, "wall_e1", new Vector3((IX1 + EX) / 2, top / 2, (OZ0 + OpenZ0) / 2), new Vector3(EX - IX1, top, OpenZ0 - OZ0), wall);
            World.Box(t, "wall_e2", new Vector3((IX1 + EX) / 2, top / 2, (OpenZ1 + OZ1) / 2), new Vector3(EX - IX1, top, OZ1 - OpenZ1), wall);
            World.Box(t, "wall_e3", new Vector3((IX1 + EX) / 2, (OpenTop + top) / 2, (OpenZ0 + OpenZ1) / 2), new Vector3(EX - IX1, top - OpenTop, OpenZ1 - OpenZ0), wall);
            // furniture
            World.Box(t, "dressing_table", new Vector3(-0.55f, 0.45f, IZ0 + 0.35f), new Vector3(1.15f, 0.9f, 0.55f), 0);
            World.Box(t, "coat_stand", new Vector3(IX0 + 0.4f, 1f, IZ1 - 0.45f), new Vector3(0.45f, 2.1f, 0.45f), 0);
            World.Box(t, "curtain_s", new Vector3(IX0 + 0.15f, 1.7f, 29.95f), new Vector3(0.35f, 3.4f, 0.6f), 0);
            World.Box(t, "curtain_n", new Vector3(IX0 + 0.15f, 1.7f, 33.8f), new Vector3(0.35f, 3.4f, 0.6f), 0);
        }

        // ------------------------------------------------------------------ shelves and labels

        static void BuildShelves(Transform t)
        {
            // the game's warm wooden shelves suit the gilt better than the dark room's
            var shelfL = World.Find("p_Shelves_L", "PostOffice_Environment") ?? World.Find("p_Shelves_Dark_L", "p_DarkRoom");
            var shelfM = World.Find("p_Shelves_M", "PostOffice_Environment") ?? World.Find("p_Shelves_Dark_M", "p_DarkRoom");
            var tagL = World.Find("prop_StorageTag_Left", "p_DarkRoom");
            var tagM = World.Find("prop_StorageTag_Left (1)", "p_DarkRoom") ?? tagL;
            // M units face their local -z, L units their local +z (as in the dark room)
            var shelfS = World.Find("p_Shelves_S", "p_BigRoom");   // the square cubby unit with drawers
            // units stand 0.42 off the walls: the wainscot, its panels and the rail stand 0.085 proud of the wall
            const float Off = 0.42f;
            var units = new (GameObject tpl, GameObject tag, Vector3 pos, float yaw, Vector3 tagLocal, float tagYaw)[]
            {
                // along the east wall, between the corner and the door
                (shelfM, tagM, new Vector3(IX1 - Off, FY, 28.55f), 90f, new Vector3(-0.79f, 1.60f, -0.22f), 90f),
                (shelfM, tagM, new Vector3(IX1 - Off, FY, 30.95f), 90f, new Vector3(-0.79f, 1.60f, -0.22f), 90f),
                // the south wall, beside the dressing table: a cubby unit for variety
                (shelfS ?? shelfM, tagM, new Vector3(1.35f, FY, IZ0 + 0.47f), 180f, new Vector3(-1.22f, 1.75f, -0.3f), 90f),
                // west wall, beyond the curtains
                (shelfM, tagM, new Vector3(IX0 + Off, FY, 28.45f), 270f, new Vector3(-0.79f, 1.60f, -0.22f), 90f),
                (shelfM, tagM, new Vector3(IX0 + Off, FY, 35.2f), 270f, new Vector3(-0.79f, 1.60f, -0.22f), 90f),
                // east wall, on the far side of the door from the corner, so the doorway stays clear
                (shelfL, tagL, new Vector3(IX1 - 0.64f, FY, 32.95f), 270f, new Vector3(0.49f, 2.14f, 0.54f), 270f),
            };
            int id = StoreIdBase, label = LabelIdBase;
            int cubbyId = StoreIdBase + 60;   // its own id range: the other units keep the ids saves refer to
            int mStores = 0;
            if (shelfM) for (int i = 0; i < shelfM.transform.childCount; i++) if (shelfM.transform.GetChild(i).GetComponent<EntityInteractableStore>()) mStores++;
            foreach (var u in units)
            {
                GameObject unit;
                if (shelfS && u.tpl == shelfS)
                {
                    unit = Yard.BuildShelf(u.tpl, t, u.pos, u.yaw, ref cubbyId, keepLightmap: true, nestedStores: true);
                    id += mStores;   // the M unit that stood here
                }
                else unit = Yard.BuildShelf(u.tpl, t, u.pos, u.yaw, ref id, keepLightmap: true);
                if (!unit) { _log.Add("no shelf template"); continue; }
                unit.name = "CME_MirrorShelf";
                var rot = Quaternion.Euler(0, u.yaw, 0);
                Planetarium.CloneLabel(u.tag, t, u.pos + rot * u.tagLocal, u.yaw + u.tagYaw, label++);
            }
            // a floor pallet in front of the west mirror (low enough to keep the glass clear), label under the glass
            var pallet = World.Find("p_Floor_Storage_XL", "PostOffice_Environment") ?? World.Find("p_Floor_Storage_Dark_XL", "p_DarkRoom");
            var p = Yard.BuildShelf(pallet, t, new Vector3(IX0 + 0.85f, FY, 31.875f), 90f, ref id, keepLightmap: true);
            if (p) p.name = "CME_MirrorPallet";
            Planetarium.CloneLabel(tagL, t, new Vector3(IX0 + 0.05f, 0.36f, 31.875f), 0f, label++);
        }

        // ------------------------------------------------------------------ candles

        static void BuildCandles(Transform t)
        {
            _candleMat = ModAssets.UnlitMaterial("mirror_candle", null, new Color(1.8f, 1.4f, 0.8f));
            var text = ModAssets.Text("mirror_lights.txt");
            if (text == null) return;
            foreach (var raw in text.Split('\n'))
            {
                var p = raw.Trim().Split(' ');
                if (p.Length < 3) continue;
                var pos = new Vector3(float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture),
                    float.Parse(p[2], CultureInfo.InvariantCulture));
                var flame = Shapes.Make("CME_Flame", Shapes.Sphere(10, 6), _candleMat, t, pos, Quaternion.identity, new Vector3(0.035f, 0.06f, 0.035f));
                var r = flame.GetComponent<MeshRenderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _candles.Add(r);
            }
        }

        // ------------------------------------------------------------------ mirrors

        static void BuildMirrors(Transform t)
        {
            // west wall, facing east; north wall, facing south (glass sizes from mirror.py)
            AddMirror(t, "west", new Vector3(IX0 + 0.085f, 1.75f, 31.875f), Vector3.right, 3.0f, 2.4f);
            AddMirror(t, "north", new Vector3(1.325f, 1.75f, IZ1 - 0.085f), Vector3.back, 2.6f, 2.4f);
        }

        static void AddMirror(Transform t, string name, Vector3 centre, Vector3 normal, float w, float h)
        {
            var m = new Mirror { Name = name, Centre = centre, Normal = normal, Up = Vector3.up, W = w, H = h };
            // facing the mirror (looking along -normal), right is up x forward = up x (-normal)
            m.Right = Vector3.Cross(Vector3.up, -normal).normalized;
            int tw = 1024, th = Mathf.RoundToInt(1024 * h / w);
            m.Texture = new RenderTexture(tw, th, 24, RenderTextureFormat.DefaultHDR) { name = "CME_Mirror_" + name, antiAliasing = 2 };
            m.Texture.Create();

            // the glass: a quad whose left edge (as the viewer sees it) shows the camera's right edge
            var mesh = new Mesh { name = "CME_MirrorGlass" };
            var r = m.Right * (w / 2);
            var u = Vector3.up * (h / 2);
            mesh.vertices = new[] { -r - u, r - u, r + u, -r + u };
            mesh.uv = new[] { new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.normals = new[] { normal, normal, normal, normal };
            // wound to face the viewer: Unity draws a triangle (a, b, c) whose Cross(b - a, c - a) points at the viewer
            var toViewer = Vector3.Dot(Vector3.Cross(mesh.vertices[1] - mesh.vertices[0], mesh.vertices[2] - mesh.vertices[0]), normal) > 0;
            mesh.triangles = toViewer ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            var glass = new GameObject("CME_MirrorGlass_" + name);
            glass.transform.SetParent(t, false);
            glass.transform.position = centre;
            glass.AddComponent<MeshFilter>().sharedMesh = mesh;
            m.Glass = glass.AddComponent<MeshRenderer>();
            m.Glass.sharedMaterial = ModAssets.UnlitMaterial("mirror_" + name, m.Texture, new Color(0.93f, 0.95f, 1.0f));
            m.Glass.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m.Glass.receiveShadows = false;

            var camGo = new GameObject("CME_MirrorCamera_" + name);
            camGo.transform.SetParent(t, false);
            m.Cam = camGo.AddComponent<Camera>();
            m.Cam.targetTexture = m.Texture;
            m.Cam.enabled = false;
            m.Cam.allowMSAA = true;
            // the camera stands behind the glass, outside the building: the game's baked occlusion would cull the room
            m.Cam.useOcclusionCulling = false;
            // the game's volumetric fog is ray-marched from the camera: from behind the glass it would add the fog of the
            // empty space between camera and glass, a haze over the far mirror. Mirror cameras see no volumes at all.
            try
            {
                var data = camGo.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()
                           ?? camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.volumeLayerMask = 0;
                data.renderPostProcessing = false;
            }
            catch (Exception e) { _log.Add("mirror camera data: " + e.Message); }
            _mirrors.Add(m);
        }

        /// <summary>
        /// Places each mirror's camera at the viewer's reflection, looking through the glass, with an off-axis
        /// frustum through the glass's corners and its near plane on the glass (so nothing behind it shows).
        /// </summary>
        static void UpdateMirrors(Camera view)
        {
            Vector3 eye = view.transform.position;
            // anywhere the glass could be seen from: the room, or the corridor around its door
            bool near = Contains(eye) || (eye - new Vector3(EX, 1.5f, (OpenZ0 + OpenZ1) / 2)).sqrMagnitude < 15f * 15f;
            Plane[] planes = null;
            foreach (var m in _mirrors)
            {
                if (!m.Cam) continue;
                float side = Vector3.Dot(eye - m.Centre, m.Normal);
                bool visible = near && side > 0.05f;
                if (visible)
                {
                    planes ??= GeometryUtility.CalculateFrustumPlanes(view);
                    visible = GeometryUtility.TestPlanesAABB(planes, m.Glass.bounds);
                }
                if (m.Cam.enabled != visible) m.Cam.enabled = visible;
                if (!visible) continue;

                var reflected = eye - 2f * side * m.Normal;
                var rot = Quaternion.LookRotation(m.Normal, Vector3.up);   // looking out of the glass into the room
                m.Cam.transform.SetPositionAndRotation(reflected, rot);
                var inv = Quaternion.Inverse(rot);
                float d = Vector3.Dot(m.Centre - reflected, m.Normal);
                float l = float.MaxValue, rr = float.MinValue, b = float.MaxValue, tp = float.MinValue;
                foreach (var cx in new[] { -0.5f, 0.5f })
                    foreach (var cy in new[] { -0.5f, 0.5f })
                    {
                        var corner = m.Centre + m.Right * (cx * m.W) + Vector3.up * (cy * m.H);
                        var local = inv * (corner - reflected);
                        l = Mathf.Min(l, local.x); rr = Mathf.Max(rr, local.x);
                        b = Mathf.Min(b, local.y); tp = Mathf.Max(tp, local.y);
                    }
                // clip just behind the glass: anything further back (the frame's backing board) would fill the picture
                float n = Mathf.Max(0.02f, d - 0.003f);
                float k = n / d;
                m.Cam.cullingMask = view.cullingMask | Spectral.HiddenLayerMask;
                m.Cam.clearFlags = view.clearFlags;
                m.Cam.backgroundColor = view.backgroundColor;
                m.Cam.farClipPlane = 60f;
                m.Cam.nearClipPlane = n;
                m.Cam.projectionMatrix = Matrix4x4.Frustum(l * k, rr * k, b * k, tp * k, n, 60f);
            }
        }

        // ------------------------------------------------------------------ reflection body

        /// <summary>
        /// The local player is drawn with a first-person body that has no head (the full body only casts its shadow).
        /// Copies of the full body and the eyes, skinned to the same bones, are drawn only by the mirror cameras, and
        /// the first-person body is hidden from them, so the reflection has a head and the player never sees it.
        /// </summary>
        static void EnsureReflectionBody()
        {
            if (!_renderHooked)
            {
                _renderHooked = true;
                try
                {
                    UnityEngine.Rendering.RenderPipelineManager.add_beginCameraRendering(
                        (Il2CppSystem.Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>)new Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>(OnBeginCamera));
                }
                catch (Exception e) { Plugin.Log.Warning("Mirror render hook: " + e.Message); }
            }
            if (_reflection.Count > 0 && _firstPersonBody && _firstPersonBody.enabled && _firstPersonBody.gameObject.activeInHierarchy) return;
            if (Time.time < _nextBodyCheck) return;
            _nextBodyCheck = Time.time + 1f;
            foreach (var r in _reflection) if (r) UnityEngine.Object.Destroy(r.gameObject);
            _reflection.Clear();
            _firstPersonBody = null;
            try
            {
                foreach (var pv in UnityEngine.Object.FindObjectsByType<PlayerVisuals>(FindObjectsSortMode.None))
                {
                    var rig = pv.transform.Find("Visuals/sk_PlayerCharacter");
                    var fps = rig ? rig.Find("sk_PlayerCharacter_FPS")?.GetComponent<SkinnedMeshRenderer>() : null;
                    var body = rig ? rig.Find("sk_PlayerCharacter")?.GetComponent<SkinnedMeshRenderer>() : null;
                    if (!fps || !body || !fps.enabled || !fps.gameObject.activeInHierarchy) continue;   // remote players have no FPS body
                    var eyes = rig.Find("sk_PlayerCharacter_Eyes")?.GetComponent<SkinnedMeshRenderer>();
                    foreach (var src in new[] { body, eyes })
                        if (src) _reflection.Add(CopySkinned(src));
                    _firstPersonBody = fps;
                    _log.Add($"reflection body (eyes={(eyes ? "yes" : "no")})");
                    break;
                }
            }
            catch (Exception e) { Plugin.Log.Warning("Reflection body: " + e.Message); }
        }

        static SkinnedMeshRenderer CopySkinned(SkinnedMeshRenderer src)
        {
            var go = new GameObject("CME_Reflection_" + src.name);
            go.transform.SetParent(src.transform.parent, false);
            go.transform.localPosition = src.transform.localPosition;
            go.transform.localRotation = src.transform.localRotation;
            go.transform.localScale = src.transform.localScale;
            go.layer = src.gameObject.layer;
            var r = go.AddComponent<SkinnedMeshRenderer>();
            r.sharedMesh = src.sharedMesh;
            r.bones = src.bones;
            r.rootBone = src.rootBone;
            r.sharedMaterials = src.sharedMaterials;
            r.localBounds = src.localBounds;
            r.updateWhenOffscreen = true;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.forceRenderingOff = true;
            return r;
        }

        /// <summary>Before each camera renders: the reflection copies only for mirrors, the first-person body never for them.</summary>
        static void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera cam)
        {
            try
            {
                bool mirror = false;
                foreach (var m in _mirrors) if (m.Cam && cam && m.Cam.Pointer == cam.Pointer) { mirror = true; break; }
                foreach (var r in _reflection) if (r && r.forceRenderingOff == mirror) r.forceRenderingOff = !mirror;
                if (_firstPersonBody && _firstPersonBody.forceRenderingOff != mirror) _firstPersonBody.forceRenderingOff = mirror;
            }
            catch { }
        }

        // ------------------------------------------------------------------ update

        public static void Update()
        {
            if (!_root) return;
            var cam = Cameras.View;
            if (!cam) return;
            if ((cam.transform.position - Centre).sqrMagnitude > 30f * 30f)
            {
                foreach (var m in _mirrors) if (m.Cam && m.Cam.enabled) m.Cam.enabled = false;
                return;
            }
            EnsureReflectionBody();
            UpdateMirrors(cam);
            float night = Lighting.NightFactor();
            if (_candleMat && Mathf.Abs(night - _lastNight) > 0.01f)
            {
                _lastNight = night;
                _candleMat.SetColor("_BaseColor", Color.Lerp(new Color(1.5f, 1.2f, 0.75f), new Color(2.4f, 1.8f, 0.95f), night));
            }
            // a gentle flicker
            float f = 1f + 0.06f * Mathf.Sin(Time.time * 13f) + 0.04f * Mathf.Sin(Time.time * 29f);
            foreach (var c in _candles) if (c) c.transform.localScale = new Vector3(0.035f, 0.06f * f, 0.035f);
        }

        public static string Describe()
        {
            var sb = new System.Text.StringBuilder($"mirror room built={Built} open={_open} log=[{string.Join("; ", _log)}]\n");
            foreach (var m in _mirrors)
                sb.AppendLine($"  mirror {m.Name} cam={(m.Cam ? m.Cam.enabled.ToString() : "-")} pos={(m.Cam ? m.Cam.transform.position.ToString() : "-")} tex={(m.Texture ? m.Texture.width + "x" + m.Texture.height : "-")}");
            return sb.ToString();
        }
    }
}
