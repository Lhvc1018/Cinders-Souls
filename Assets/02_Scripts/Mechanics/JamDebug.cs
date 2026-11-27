using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Codice.Characters;
using Codice.Expedition;
using Codice.UI;
namespace Codice.Mechanics
{
    // Debug tools behind the "jam" code: free movement between rooms, class switching and test scenarios. F3 toggles the panel.
    public sealed class JamDebug : MonoBehaviour
    {
        public static bool Enabled => MainMenuUI.CheatsUnlocked && !Codice.Multiplayer.LanSession.Active;
        // Off by default: seals must close in normal play; toggle it in the F3 panel for testing.
        static bool freeRoam = false;
        public static bool FreeRoam => Enabled && freeRoam;
        bool open; GUIStyle title, button, label; Texture2D dark;
        static readonly string[] classes = { "Caballero", "Tanque", "Arquero", "Mago", "Shinobi" };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() { var go = new GameObject("Jam debug"); DontDestroyOnLoad(go); go.AddComponent<JamDebug>(); freeRoam = false; }
        PlayerCharacter Player => RunDirector.Instance ? RunDirector.Instance.Player : null;
        void Update()
        {
            if (!Enabled || !Player) { if (open) Show(false); return; }
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) Show(!open);
        }
        void Show(bool value)
        {
            open = value; var p = Player; if (!p) return;
            p.SetNarrativeControlsBlocked(value);
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = value;
        }
        public static bool Teleport(int room)
        {
            var run = RunDirector.Instance; var floor = run ? run.Expedition : null; if (!Enabled || !floor || room < 0 || room >= floor.RoomCount) return false;
            // An active encounter is finished first (its enemies die normally), so the next room can start its own.
            if (floor.InEncounter) ClearEncounter();
            var p = run.Player; var cc = p.GetComponent<CharacterController>(); cc.enabled = false;
            // Arrive just inside the south edge so the encounter of an uncleared room starts in front of the player.
            p.transform.position = floor.Center(room) + new Vector3(0, .15f, -(floor.Layout.Radius(room) * 4 - 3)); cc.enabled = true;
            Debug.Log("[Jam] Teleport to room " + (room + 1)); return true;
        }
        public static void ClearEncounter()
        {
            var run = RunDirector.Instance; if (!Enabled || !run || !run.Player) return;
            var hit = new DamageInfo { amount = 1e7f, source = run.Player.gameObject, direction = Vector3.forward, ignoreGuard = true };
            for (int pass = 0; pass < 12; pass++) // waves spawn as others fall
            {
                var enemies = FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None); var bosses = FindObjectsByType<Codice.Bosses.BossCharacter>(FindObjectsSortMode.None);
                if (enemies.Length + bosses.Length == 0) break;
                foreach (var e in enemies) if (e.IsAlive) e.ReceiveDamage(hit);
                foreach (var b in bosses) if (b.IsAlive) b.ReceiveDamage(hit);
            }
        }
        public static bool ChangeClass(PlayerClass role) { var p = RunDirector.Instance ? RunDirector.Instance.Player : null; return Enabled && p && p.SelectLaboratoryClass(role); }
        public static void StartEarthquakeLord(PlayerClass role) { ExpeditionFloor.DebugSeed = ExpeditionLayout.GiantSeed(); RunNavigation.StartExpeditionTest(role, 4); }
        void OnGUI()
        {
            if (!Enabled || !Player) return;
            if (dark == null)
            {
                dark = new Texture2D(1, 1); dark.SetPixel(0, 0, new Color(.05f, .05f, .06f, .93f)); dark.Apply();
                title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold }; title.normal.textColor = new Color(.83f, .66f, .36f);
                label = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true }; label.normal.textColor = new Color(.9f, .87f, .8f);
                button = new GUIStyle(GUI.skin.button) { fontSize = 15, fixedHeight = 30 };
            }
            float s = Mathf.Max(1, Screen.height / 1080f); var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
            if (!open) { GUI.Label(new Rect(Screen.width / s - 380, Screen.height / s - 70, 370, 26), "JAM · F3: panel de pruebas" + (FreeRoam ? " · paso libre" : ""), label); GUI.matrix = old; return; }
            var area = new Rect(Screen.width / s - 470, 60, 450, 760); GUI.DrawTexture(area, dark);
            GUILayout.BeginArea(new Rect(area.x + 16, area.y + 12, area.width - 32, area.height - 24));
            GUILayout.Label("PRUEBAS · JAM", title);
            var run = RunDirector.Instance; var floor = run.Expedition;
            freeRoam = GUILayout.Toggle(freeRoam, " Paso libre: sin sellos ni orden de salas");
            if (floor)
            {
                if (freeRoam) floor.OpenAllSeals();
                GUILayout.Label($"Piso {(floor.FloorNumber - 1) / 3 + 1}-{(floor.FloorNumber - 1) % 3 + 1} · semilla {floor.Seed} · sala actual {floor.CurrentRoom + 1}/{floor.RoomCount}", label);
                GUILayout.Label("IR A SALA", title);
                for (int row = 0; row * 4 < floor.RoomCount; row++)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = row * 4; i < Mathf.Min(floor.RoomCount, row * 4 + 4); i++)
                    {
                        string name = i == floor.Layout.Boss ? "JEFE" : i == floor.Layout.MerchantRoom ? "Tienda" : i == floor.Layout.StatueRoom ? "Estatua" : i == floor.Layout.SecretRoom ? "Secreta" : "Sala " + (i + 1);
                        if (GUILayout.Button(name + (floor.Cleared[i] ? " ✓" : ""), button)) { Teleport(i); Show(false); }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            if (floor && floor.InEncounter && GUILayout.Button("Terminar el combate de esta sala", button)) ClearEncounter();
            GUILayout.Space(8); GUILayout.Label("CLASE", title);
            int current = (int)Player.State.Class; int pick = GUILayout.SelectionGrid(current, classes, 3, button);
            if (pick != current) ChangeClass((PlayerClass)pick);
            GUILayout.Space(8); GUILayout.Label("ESCENARIOS", title);
            for (int row = 0; row < (RunDirector.FinalExpeditionFloor+2)/3; row++)
            {
                GUILayout.BeginHorizontal();
                for (int i = 1; i <= 3; i++) { int f = row * 3 + i; if (f <= RunDirector.FinalExpeditionFloor && GUILayout.Button($"{row + 1}-{i}", button)) { Show(false); RunNavigation.StartExpeditionTest(Player.State.Class, f); } }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("EL SEÑOR DE LOS TERREMOTOS (2-1, arena gigante)", button)) { var role = Player.State.Class; Show(false); StartEarthquakeLord(role); }
            GUILayout.Space(8);
            bool god = SessionCheats.God; bool wanted = GUILayout.Toggle(god, " Good!: vida, daño y maná infinitos"); if (wanted != god) SessionCheats.Execute(wanted ? "Good!" : "Good! off");
            GUILayout.Label("F3 cierra el panel.", label);
            GUILayout.EndArea(); GUI.matrix = old;
        }
    }
}
