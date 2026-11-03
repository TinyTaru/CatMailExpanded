using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// After the game's ending the break room becomes the speaker room: two speakers play music, a trash can
    /// appears, the empty tuna cans become carryable, and each shelf compartment turns into parcel storage once
    /// its cans have been thrown away. Restless parcels calm down in here while the music plays.
    /// </summary>
    internal static class SpeakerRoom
    {
        // storage compartments (world space, measured from the bookshelf and pantry)
        sealed class Compartment
        {
            public string Store;
            public Vector3 Pos;
            public float Yaw;
            public Box Area;
            public float MaxHeight;
            public EntityInteractableStore Instance;
            public bool Enabled;
        }

        static readonly List<Compartment> _compartments = new();
        static GameObject _root;
        static AudioSource _music;
        static Transform[] _speakers = Array.Empty<Transform>();
        static readonly List<MusicNotes> _notes = new();
        static Vector3[] _speakerBase = Array.Empty<Vector3>();
        static int _track;
        static bool _ducked;
        static bool _hintShown;

        public static bool Built { get; private set; }
        public static bool StoresBuilt { get; private set; }
        public static bool MusicOn { get; set; } = true;

        static Vector3 TrashPos = new(8.45f, 0.05f, 25.15f);

        public static bool MusicAudibleAt(Vector3 pos) =>
            Built && MusicOn && _music && _music.isPlaying && Zones.InBreakRoom(pos);

        public static void Reset()
        {
            Built = false;
            StoresBuilt = false;
            _compartments.Clear();
            _root = null;
            _music = null;
            _speakers = Array.Empty<Transform>();
            foreach (var n in _notes) n.Destroy();
            _notes.Clear();
            MusicNotes.ResetShared();
            _ducked = false;
            _hintShown = false;
            TunaCans.Reset();
        }

        static void DefineCompartments()
        {
            _compartments.Clear();
            const float bz = 18.47f;          // bookshelf depth centre
            void Book(string store, float x0, float x1, float y, float h)
            {
                _compartments.Add(new Compartment
                {
                    Store = store, Pos = new Vector3((x0 + x1) / 2, y, bz), Yaw = 180, MaxHeight = h,
                    Area = new Box(new Vector3(x0, y - 0.05f, 18.1f), new Vector3(x1, y + h, 18.8f)),
                });
            }
            // bookshelf rows: board top heights and dividers (x 8.15..11.75)
            Book("p_Entity_Storage_2x4", 10.72f, 11.75f, 0.91f, 0.68f);
            Book("p_Entity_Storage_2x6", 8.65f, 10.67f, 0.91f, 0.68f);   // 1.5m grid centred in a 2.0m bay
            Book("p_Entity_Storage_2x6", 9.32f, 11.75f, 1.66f, 0.72f);
            Book("p_Entity_Storage_2x4", 8.15f, 9.27f, 1.66f, 0.72f);
            Book("p_Entity_Storage_2x4", 10.72f, 11.75f, 2.47f, 0.68f);
            Book("p_Entity_Storage_2x6", 8.65f, 10.67f, 2.47f, 0.68f);
            // pantry rows (z 23.84..25.48), facing -x
            foreach (var y in new[] { 0.22f, 0.94f, 1.72f, 2.48f, 3.26f })
            {
                _compartments.Add(new Compartment
                {
                    Store = "p_Entity_Storage_2x6", Pos = new Vector3(11.6f, y, 24.66f), Yaw = 90, MaxHeight = 0.7f,
                    Area = new Box(new Vector3(11.1f, y - 0.05f, 23.8f), new Vector3(12.1f, y + 0.7f, 25.5f)),
                });
            }
        }

        /// <summary>Storage grids must exist before the save loads so parcels stored on them reattach.</summary>
        public static void BuildStores()
        {
            if (StoresBuilt) return;
            try
            {
                DefineCompartments();
                var holder = GameObject.Find("CME_SpeakerRoomStores") ?? new GameObject("CME_SpeakerRoomStores");
                int id = 100;
                foreach (var c in _compartments)
                {
                    // left enabled until the save has loaded so parcels stored here reattach; ApplyState() runs after
                    c.Instance = StoreFactory.Create(c.Store, holder.transform, c.Pos, c.Yaw, id++, c.MaxHeight);
                    c.Enabled = true;
                }
                StoresBuilt = true;
            }
            catch (Exception e) { Plugin.Log.Error("Speaker room stores failed: " + e); }
        }

        static void SetCompartmentEnabled(Compartment c, bool on)
        {
            if (c.Enabled == on || c.Instance == null) return;
            c.Enabled = on;
            try { c.Instance.SetStoreEnabled(on); }
            catch (Exception e) { Plugin.Log.Warning("SetStoreEnabled failed: " + e.Message); }
            // the grid's surface collider sits right under the cans and would steal the player's aim from them
            try
            {
                foreach (var col in c.Instance.GetComponentsInChildren<Collider>(true))
                    if (col.gameObject.name == "col_Storage") col.enabled = on;
            }
            catch (Exception e) { Plugin.Log.Warning("Storage collider toggle failed: " + e.Message); }
        }

        /// <summary>Converts the break room (called once the ending has been reached).</summary>
        public static void Build()
        {
            if (Built) return;
            try { BuildInternal(); Built = true; RefreshCompartments(); }
            catch (Exception e) { Plugin.Log.Error("Speaker room build failed: " + e); }
        }

        static void BuildInternal()
        {
            BuildStores();
            _root = World.Root("CME_SpeakerRoom");
            var t = _root.transform;

            // floor speaker beside the bookshelf, and a smaller one on the end of the table, both aimed at the room
            var s1 = World.Mesh("speaker", "tex_speaker", t, new Vector3(11.56f, 0.05f, 19.3f), -30, true);
            s1.name = "CME_speaker_floor";
            s1.transform.localScale = Vector3.one * 1.3f;
            var s2 = World.Mesh("speaker", "tex_speaker", t, new Vector3(8.42f, 0.63f, 20.95f), 56, true);
            s2.name = "CME_speaker_table";
            Placement.SettleWithBounds(s1);
            Placement.SettleWithBounds(s2);
            _speakers = new[] { s1.transform, s2.transform };
            // notes pour out of the speaker fronts (model faces local +Z; cones between 0.27 and 0.55 m up)
            foreach (var sp in _speakers) _notes.Add(new MusicNotes(sp, new Vector3(0f, 0.42f, 0.24f)));
            _speakerBase = new[] { s1.transform.localScale, s2.transform.localScale };

            World.Mesh("poster_frame", "tex_poster_frame", t, new Vector3(7.975f, 2.2f, 20.75f), 180);
            // picture sits 12 mm inside the frame so its edges never share a plane with the frame bars
            World.Mesh("poster_face", "poster_music", t, new Vector3(7.963f, 2.2f, 20.75f), 180);

            var trash = World.Mesh("trashcan", "tex_trashcan", t, TrashPos, 90, false);
            Placement.SettleWithBounds(trash);
            TrashPos = trash.transform.position;
            // hollow-ish collider: a low box so thrown cans drop inside the bin
            World.Box(trash.transform, "trash_body", TrashPos + new Vector3(0, 0.22f, 0), new Vector3(0.5f, 0.44f, 0.5f), 0);
            TunaCans.SetTrashPos(TrashPos);

            // music: a 3D source between the speakers, routed through the game's music mixer
            var src = new GameObject("CME_SpeakerMusic");
            src.transform.SetParent(t, false);
            src.transform.position = new Vector3(10.1f, 1.4f, 21.0f);
            _music = src.AddComponent<AudioSource>();
            _music.spatialBlend = 1f;
            _music.rolloffMode = AudioRolloffMode.Linear;
            _music.minDistance = 4.5f;
            _music.maxDistance = 13f;
            _music.dopplerLevel = 0f;
            _music.volume = 0.85f;
            _music.loop = false;
            _track = UnityEngine.Random.Range(0, Playlist.Length);

            // the catnip pot on the bookshelf moves up to the (unused) top shelf so its compartment is free
            var catnip = World.Find("prop_Catnip (1)", "p_BreakRoom");
            if (catnip) catnip.transform.position = new Vector3(10.55f, 3.21f, 18.43f);

            TunaCans.Begin(TrashPos);
            RefreshCompartments();
            Plugin.Log.Msg("Speaker room built");
        }

        static readonly string[] Playlist = { "audioasset_Music_MainMenuTheme", "audioasset_Music_MorningTheme_Loop", "audioasset_Music_EveningTheme_Loop" };

        static void NextTrack()
        {
            for (int tries = 0; tries < Playlist.Length; tries++)
            {
                var asset = SoundFx.Find(Playlist[_track % Playlist.Length]);
                _track++;
                if (asset == null || asset.AudioClips == null || asset.AudioClips.Length == 0) continue;
                _music.clip = asset.AudioClips[0];
                if (asset.MixerGroupType) _music.outputAudioMixerGroup = asset.MixerGroupType;
                _music.Play();
                return;
            }
        }

        static void RefreshCompartments()
        {
            foreach (var c in _compartments)
                SetCompartmentEnabled(c, Built && !TunaCans.AnyCanIn(c.Area));
        }

        public static void Update()
        {
            if (!Built) return;
            float dt = Time.deltaTime;
            if (_music)
            {
                if (MusicOn && !_music.isPlaying) NextTrack();
                if (!MusicOn && _music.isPlaying) _music.Stop();
                // speakers bounce gently to the beat
                float beat = Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * 1.75f));
                for (int i = 0; i < _speakers.Length; i++)
                {
                    if (!_speakers[i]) continue;
                    float k = _music.isPlaying ? beat * 0.025f : 0f;
                    _speakers[i].localScale = new Vector3(_speakerBase[i].x * (1 + k * 0.4f), _speakerBase[i].y * (1 - k), _speakerBase[i].z * (1 + k * 0.4f));
                }
                DuckGameMusic();
                var cam = Core.Cameras.View;
                foreach (var n in _notes)
                {
                    n.Emitting = _music.isPlaying;
                    n.Update(dt, cam, _music.isPlaying ? beat : 0f);
                }
            }
            if (TunaCans.Update()) RefreshCompartments();
        }

        /// <summary>Turns the regular background music down while the player is inside the speaker room.</summary>
        static void DuckGameMusic()
        {
            var cam = Core.Cameras.View;
            bool inside = cam && Zones.InBreakRoom(cam.transform.position) && _music.isPlaying;
            if (inside == _ducked) return;
            _ducked = inside;
            try
            {
                if (inside) AudioManager.Instance?.SetMusicVolume(0.1f, true, 1.2f);
                else AudioManager.Instance?.RestoreMusicVolume(1.2f);
            }
            catch (Exception e) { Plugin.Log.Warning("Music duck failed: " + e.Message); }
            if (inside && !_hintShown && TunaCans.Remaining > 0)
            {
                _hintShown = true;
                try { InterfaceManager.Instance?.NotificationInterface?.ShowNotification(Localization.Text("CME/Cans_Hint")); } catch { }
            }
        }

        public static void OnShelfCleared()
        {
            try { InterfaceManager.Instance?.NotificationInterface?.ShowNotification(Localization.Text("CME/Shelf_Cleared")); } catch { }
        }

        public static void ForceRefresh() => RefreshCompartments();

        /// <summary>Called once the save has loaded: compartments only work once their cans are gone.</summary>
        public static void ApplyState() => RefreshCompartments();

        public static IEnumerable<(Box area, bool enabled)> DebugCompartments()
        {
            foreach (var c in _compartments) yield return (c.Area, c.Enabled);
        }
    }
}
