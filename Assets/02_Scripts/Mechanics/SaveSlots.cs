using System;
using System.IO;
using UnityEngine;
namespace Codice.Mechanics
{
    /// Three independent save files ("partidas"). Each keeps its own long-term progress (Vigía lessons,
    /// deepest floor, memory keys, endings) and, if a descent is in progress, a checkpoint at the start of
    /// the current floor so "Continuar" resumes it. Progress code keeps reading PlayerPrefs; switching slot
    /// swaps those keys, so nothing else in the game needs to know about slots.
    public static class SaveSlots
    {
        public const int Count = 3;
        [Serializable] public sealed class SaveData
        {
            public string name = ""; public long created, lastPlayed; public float playSeconds;
            public int heard, deepest, runs, memory, endingDawn, endingVigil, deaths, victories;
            public bool hasRun; public int runClass, runFloor, runOrbs, runKills; public bool runWatcher, runBell, runPilgrim, runWisp;
            public float runElapsed; public string runState; public int[] runSpells; public int runSpellSelection;
            public bool Empty => created == 0;
        }
        static readonly string[] IntKeys = { "Cinders.Vigia.Heard", "Cinders.Progress.Deepest", "Cinders.Progress.Runs", Codice.Expedition.DescentMemory.SaveKey, "Cinders.Ending.Dawn", "Cinders.Ending.Vigil" };
        const string ActiveKey = "Cinders.Save.Active";
        static SaveData[] cache;
        static float sessionStart;
        public static int Active => PlayerPrefs.GetInt(ActiveKey, -1);
        public static SaveData Current => Active >= 0 ? Get(Active) : null;
        static string Dir => Path.Combine(Application.persistentDataPath, "Saves");
        static string FilePath(int slot) => Path.Combine(Dir, $"partida{slot + 1}.json");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { cache = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            sessionStart = Time.realtimeSinceStartup; Load();
            // First launch with slots: the progress already on this PC becomes "Partida 1".
            if (Active < 0)
            {
                bool hasProgress = false; foreach (var k in IntKeys) if (PlayerPrefs.GetInt(k, 0) != 0) hasProgress = true;
                if (hasProgress) { var d = new SaveData { name = "Partida 1", created = Now() }; Capture(d); cache[0] = d; Write(0); PlayerPrefs.SetInt(ActiveKey, 0); PlayerPrefs.Save(); }
            }
            Application.quitting += Commit;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => Commit();
        }
        static long Now() => DateTime.Now.Ticks;
        static void Load()
        {
            cache = new SaveData[Count];
            for (int i = 0; i < Count; i++)
            {
                try { var p = FilePath(i); cache[i] = File.Exists(p) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(p)) : new SaveData(); }
                catch (Exception e) { Debug.LogWarning("[Saves] Partida " + (i + 1) + " ilegible: " + e.Message); cache[i] = new SaveData(); }
                if (cache[i] == null) cache[i] = new SaveData();
            }
        }
        public static SaveData Get(int slot) { if (cache == null) Load(); return cache[Mathf.Clamp(slot, 0, Count - 1)]; }
        static void Write(int slot)
        {
            try { Directory.CreateDirectory(Dir); var tmp = FilePath(slot) + "." + System.Diagnostics.Process.GetCurrentProcess().Id + ".tmp"; File.WriteAllText(tmp, JsonUtility.ToJson(cache[slot], true)); if (File.Exists(FilePath(slot))) File.Delete(FilePath(slot)); File.Move(tmp, FilePath(slot)); }
            catch (Exception e) { Debug.LogWarning("[Saves] No se pudo guardar la partida " + (slot + 1) + " (se reintentará): " + e.Message); }
        }
        static void Capture(SaveData d)
        {
            d.heard = PlayerPrefs.GetInt(IntKeys[0], 0); d.deepest = PlayerPrefs.GetInt(IntKeys[1], 0); d.runs = PlayerPrefs.GetInt(IntKeys[2], 0);
            d.memory = PlayerPrefs.GetInt(IntKeys[3], 0); d.endingDawn = PlayerPrefs.GetInt(IntKeys[4], 0); d.endingVigil = PlayerPrefs.GetInt(IntKeys[5], 0);
        }
        static void Apply(SaveData d)
        {
            int[] v = { d.heard, d.deepest, d.runs, d.memory, d.endingDawn, d.endingVigil };
            for (int i = 0; i < IntKeys.Length; i++) { if (v[i] != 0) PlayerPrefs.SetInt(IntKeys[i], v[i]); else PlayerPrefs.DeleteKey(IntKeys[i]); }
            PlayerPrefs.Save();
        }
        /// Copies the live progress into the active slot file.
        public static void Commit()
        {
            int a = Active; if (a < 0 || cache == null) return;
            var d = cache[a]; Capture(d); d.lastPlayed = Now();
            float now = Time.realtimeSinceStartup; d.playSeconds += Mathf.Max(0, now - sessionStart); sessionStart = now;
            Write(a);
        }
        /// Makes a slot the active one (creating it if empty). Progress of the previous slot is stored first.
        public static void Select(int slot)
        {
            Commit();
            var d = Get(slot);
            if (d.Empty) { d.name = "Partida " + (slot + 1); d.created = Now(); }
            PlayerPrefs.SetInt(ActiveKey, slot); Apply(d); sessionStart = Time.realtimeSinceStartup; d.lastPlayed = Now(); Write(slot);
        }
        public static void Delete(int slot)
        {
            if (cache == null) Load();
            try { if (File.Exists(FilePath(slot))) File.Delete(FilePath(slot)); } catch (Exception e) { Debug.LogError(e.Message); }
            cache[slot] = new SaveData();
            if (Active == slot) { PlayerPrefs.DeleteKey(ActiveKey); Apply(cache[slot]); }
            PlayerPrefs.Save();
        }
        /// Ensures some slot is active before a descent starts (first free slot, else slot 1).
        public static void EnsureActive()
        {
            if (Active >= 0) return;
            int free = 0; for (int i = 0; i < Count; i++) if (Get(i).Empty) { free = i; break; }
            Select(free);
        }
        // ---------------- run checkpoint ----------------
        public static bool CanContinue => Current != null && Current.hasRun && !string.IsNullOrEmpty(Current.runState);
        public static void Checkpoint(RunDirector run)
        {
            if (Active < 0 || !run || !run.Player || Codice.Multiplayer.LanSession.Active || Codice.UI.RunNavigation.EndlessMode) return;
            var d = Current; var p = run.Player; var book = p.GetComponent<Codice.Characters.RunSpellbook>();
            d.hasRun = true; d.runClass = (int)p.State.Class; d.runFloor = run.FloorNumber; d.runState = p.State.Capture();
            d.runOrbs = run.DestinyOrbs; d.runBell = run.LastBellSeal; d.runWatcher = run.WatcherEncounterComplete; d.runPilgrim = run.HiredPilgrim; d.runWisp = run.BondedWisp;
            d.runElapsed = run.Elapsed; d.runKills = run.TotalKills;
            d.runSpells = book ? book.Known.ConvertAll(s => (int)s).ToArray() : new int[0]; d.runSpellSelection = book ? book.Selected : 0;
            Commit();
        }
        public static void EndRun(bool victory)
        {
            var d = Current; if (d == null || Codice.Multiplayer.LanSession.Active) return;
            if (!d.hasRun && !victory) { d.deaths++; Commit(); return; }
            d.hasRun = false; d.runState = null; if (victory) d.victories++; else d.deaths++;
            Commit();
        }
        public static string Describe(SaveData d)
        {
            if (d.Empty) return "Vacía · Empieza un descenso nuevo";
            var t = TimeSpan.FromSeconds(d.playSeconds);
            string run = d.hasRun ? $"En curso: {ClassName(d.runClass)} · piso {World(d.runFloor)}" : "Sin descenso en curso";
            return $"{run}\nMás hondo: {(d.deepest > 0 ? World(d.deepest) : "—")} · Intentos: {d.runs} · Victorias: {d.victories} · {(int)t.TotalHours}h {t.Minutes:00}m\nÚltima vez: {(d.lastPlayed > 0 ? new DateTime(d.lastPlayed).ToString("dd/MM/yyyy HH:mm") : "—")}";
        }
        public static string World(int floor) => $"{(floor - 1) / 3 + 1}-{(floor - 1) % 3 + 1}";
        public static string ClassName(int c) => new[] { "Caballero", "Arquero", "Tanque", "Mago", "Shinobi", "Assasin" }[Mathf.Clamp(ClassIndex((Codice.Characters.PlayerClass)c), 0, 5)];
        static int ClassIndex(Codice.Characters.PlayerClass c) => c switch { Codice.Characters.PlayerClass.Warrior => 0, Codice.Characters.PlayerClass.Archer => 1, Codice.Characters.PlayerClass.Tank => 2, Codice.Characters.PlayerClass.Mage => 3, Codice.Characters.PlayerClass.Shinobi => 4, _ => 5 };
    }
}
