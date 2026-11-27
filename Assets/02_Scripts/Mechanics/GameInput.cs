using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
namespace Codice.Mechanics
{
    public enum InputScheme { Keyboard, Gamepad }
    public enum GameAction { HeavyAttack, Attack, Block, Dodge, Jump, Sprint, Interact, Special, Flask, Relic1, Relic2, Reload, LockOn, SwapWeapon, NextSpell, CastSpell, Camera, Pause, Confirm, Back, Restart, Move, Look }
    /// One place for every player input: keyboard + mouse and any gamepad (Xbox, PlayStation, generic).
    /// Tracks which device the player touched last, so prompts, tutorials and menus follow it.
    [DefaultExecutionOrder(-1000)]
    public sealed class GameInput : MonoBehaviour, ITextPreprocessor
    {
        public static InputScheme Scheme { get; private set; }
        public static bool UsingGamepad => Scheme == InputScheme.Gamepad;
        static Gamepad lastPad;
        /// The pad the player touched last (falls back to any connected pad).
        public static Gamepad ActivePad => lastPad != null && lastPad.added ? lastPad : Gamepad.current;
        public static bool PlayStation => ActivePad is UnityEngine.InputSystem.DualShock.DualShockGamepad;
        public static string PadName => ActivePad != null ? ActivePad.displayName : "";
        public static event System.Action<InputScheme> SchemeChanged;
        public static float PadSensitivity { get => PlayerPrefs.GetFloat("Cinders.PadSensitivity", 1f); set { PlayerPrefs.SetFloat("Cinders.PadSensitivity", Mathf.Clamp(value, .3f, 2.5f)); PlayerPrefs.Save(); } }
        public static bool InvertY { get => PlayerPrefs.GetInt("Cinders.PadInvertY", 0) == 1; set { PlayerPrefs.SetInt("Cinders.PadInvertY", value ? 1 : 0); PlayerPrefs.Save(); } }
        static GameInput instance;
        static bool sprintLatched; static int lockTapFrame = -1, cameraHoldFrame = -1; static float r3Down = -1; static bool r3Fired;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { instance = null; sprintLatched = false; SchemeChanged = null; lastPad = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
        {
            if (instance) return;
            var go = new GameObject("Game input"); DontDestroyOnLoad(go); instance = go.AddComponent<GameInput>();
            Scheme = Gamepad.current != null && PlayerPrefs.GetInt("Cinders.Input.Last", 0) == 1 ? InputScheme.Gamepad : InputScheme.Keyboard;
        }
        float scanAt,rumbleUntil;
        const float MouseChargeDelay = .24f;
        float mouseDownAt = -1;
        bool mouseCharging;
        int mouseTapFrame = -1, mouseChargeFrame = -1;

        void TrackAttackButton(Mouse mouse)
        {
            if (mouse == null || Blocked || Time.timeScale <= 0 || Cursor.lockState != CursorLockMode.Locked
                || Codice.Multiplayer.LanSession.WaitingForFloor || Codice.UI.RunHUD.HandlesEscapeThisFrame
                || (Codice.UI.RunHUD.Instance && (Codice.UI.RunHUD.Instance.Paused || Codice.UI.RunHUD.Instance.ResultsVisible)))
            {
                mouseDownAt = -1;
                mouseCharging = false;
                return;
            }
            var button = mouse.leftButton;
            if (button.wasPressedThisFrame)
            {
                mouseDownAt = Time.unscaledTime;
                mouseCharging = false;
            }
            if (mouseDownAt < 0) return;
            if (button.isPressed && !mouseCharging && Time.unscaledTime - mouseDownAt >= MouseChargeDelay)
            {
                mouseCharging = true;
                mouseChargeFrame = Time.frameCount;
            }
            if (button.wasReleasedThisFrame || !button.isPressed)
            {
                if (!mouseCharging) mouseTapFrame = Time.frameCount;
                mouseDownAt = -1;
                mouseCharging = false;
            }
        }
        void Update()
        {
            if(rumbleUntil>0&&Time.unscaledTime>=rumbleUntil){StopRumble();rumbleUntil=0;}
            var keys = Keyboard.current; var mouse = Mouse.current; bool padUsed = false;
            foreach (var g in Gamepad.all) if (PadActive(g)) { padUsed = true; lastPad = g; }
            var pad = ActivePad;
            bool keysUsed = keys != null && keys.anyKey.wasPressedThisFrame || mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.delta.ReadValue().sqrMagnitude > 36);
            if (padUsed && Scheme != InputScheme.Gamepad) SetScheme(InputScheme.Gamepad);
            else if (keysUsed && !padUsed && Scheme != InputScheme.Keyboard) SetScheme(InputScheme.Keyboard);
            if (Scheme == InputScheme.Gamepad && pad == null) SetScheme(InputScheme.Keyboard);
            // Sprint on a pad: click the left stick, keep running until the stick is released.
            TrackAttackButton(mouse);
            TrackR3();
            if (pad != null) { if (PadPressed(GameAction.Sprint)) sprintLatched = !sprintLatched; if (Move().magnitude < .2f) sprintLatched = false; } else sprintLatched = false;
            if (Time.unscaledTime >= scanAt) { scanAt = Time.unscaledTime + .35f; AttachPrompts(); }
            if (UsingGamepad) KeepMenuFocus();
        }
        /// R3 does two jobs: a tap fixes the target, holding it ~0.45 s swaps first / third person.
        static void TrackR3()
        {
            bool held = false; foreach (var g in Gamepad.all) if (g.rightStickButton.isPressed) held = true;
            if (held) { if (r3Down < 0) { r3Down = Time.unscaledTime; r3Fired = false; } else if (!r3Fired && Time.unscaledTime - r3Down >= .45f) { r3Fired = true; cameraHoldFrame = Time.frameCount; } }
            else if (r3Down >= 0) { if (!r3Fired) lockTapFrame = Time.frameCount; r3Down = -1; }
        }
        static bool PadActive(Gamepad pad)
        {
            if (pad.leftStick.ReadValue().magnitude > .5f || pad.rightStick.ReadValue().magnitude > .5f) return true;
            foreach (var c in pad.allControls) if (c is ButtonControl b && !(c.parent is StickControl) && b.wasPressedThisFrame) return true;
            return false;
        }
        static void SetScheme(InputScheme s)
        {
            Scheme = s; PlayerPrefs.SetInt("Cinders.Input.Last", s == InputScheme.Gamepad ? 1 : 0);
            if (s == InputScheme.Gamepad) { if (Cursor.lockState != CursorLockMode.Locked) Cursor.visible = false; }
            else if (Cursor.lockState != CursorLockMode.Locked) Cursor.visible = true;
            foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) if (t.textPreprocessor == (ITextPreprocessor)instance && t.isActiveAndEnabled && t.text != null && (t.text.IndexOf('[') >= 0 || t.text.IndexOf("lic ", System.StringComparison.OrdinalIgnoreCase) >= 0)) t.ForceMeshUpdate(true, true);
            SchemeChanged?.Invoke(s);
        }
        /// With a pad, a visible menu must always have something selected or the stick cannot move.
        void KeepMenuFocus()
        {
            var es = EventSystem.current; if (!es) return;
            var sel = es.currentSelectedGameObject;
            if (sel && sel.activeInHierarchy && sel.GetComponent<UnityEngine.UI.Selectable>() is UnityEngine.UI.Selectable s && s.IsInteractable()) return;
            if (Cursor.lockState == CursorLockMode.Locked) return;
            UnityEngine.UI.Selectable best = null; int bestOrder = int.MinValue;
            foreach (var b in UnityEngine.UI.Selectable.allSelectablesArray)
            {
                if (!b || !b.IsInteractable() || !b.isActiveAndEnabled) continue;
                var canvas = b.GetComponentInParent<Canvas>(); if (!canvas || !canvas.enabled) continue;
                var group = b.GetComponentInParent<CanvasGroup>(); if (group && (!group.interactable || group.alpha < .05f)) continue;
                int order = canvas.rootCanvas.sortingOrder * 100000 + Mathf.RoundToInt(b.transform.position.y / Mathf.Max(.01f, canvas.rootCanvas.scaleFactor));
                if (best == null || order > bestOrder) { best = b; bestOrder = order; }
            }
            if (best) best.Select();
        }
        void AttachPrompts()
        {
            foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                if (t.textPreprocessor == null) { t.textPreprocessor = this; if (UsingGamepad && t.text != null && t.text.IndexOf('[') >= 0) t.ForceMeshUpdate(true, true); }
        }
        public string PreprocessText(string text) => Prompt(text);

        // ---------------- queries ----------------
        static Keyboard K => Keyboard.current; static Mouse M => Mouse.current;
        static ButtonControl PadButton(GameAction a, Gamepad p)
        {
            if (p == null) return null;
            switch (a)
            {
                case GameAction.Attack: return p.rightShoulder;
                case GameAction.Block: return p.leftShoulder;
                case GameAction.Dodge: return p.buttonEast;
                case GameAction.Jump: return p.buttonSouth;
                case GameAction.Interact: return p.buttonWest;
                case GameAction.HeavyAttack: return p.rightTrigger;
                case GameAction.CastSpell: return p.leftTrigger;
                case GameAction.Flask: return p.buttonNorth;
                case GameAction.Relic1: return p.dpad.left;
                case GameAction.Relic2: return p.dpad.right;
                // Elden Ring layout: up rotates the prepared spells, down swaps weapons (ranged weapons reload by themselves when empty).
                case GameAction.NextSpell: return p.dpad.up;
                case GameAction.SwapWeapon: case GameAction.Reload: return p.dpad.down;
                case GameAction.LockOn: case GameAction.Camera: return p.rightStickButton;
                case GameAction.Pause: return p.startButton;
                case GameAction.Confirm: return p.buttonSouth;
                case GameAction.Back: return p.buttonEast;
                case GameAction.Restart: return p.buttonNorth;
                case GameAction.Sprint: return p.leftStickButton;
            }
            return null;
        }
        static ButtonControl KeyButton(GameAction a)
        {
            var k = K; var m = M; if (k == null) return null;
            switch (a)
            {
                case GameAction.Attack: return null;   /* tap and hold are resolved once in TrackAttackButton */
                case GameAction.Block: return m?.rightButton;
                case GameAction.Dodge: return k.leftShiftKey;
                case GameAction.Jump: return k.spaceKey;
                case GameAction.Sprint: return k.leftCtrlKey;
                case GameAction.Interact: return k.eKey;
                case GameAction.Special: return k.qKey;
                case GameAction.HeavyAttack: return k.zKey;
                case GameAction.Flask: return k.hKey;
                case GameAction.Relic1: return k.fKey;
                case GameAction.Relic2: return k.gKey;
                case GameAction.Reload: return k.rKey;
                case GameAction.LockOn: return m?.middleButton;
                case GameAction.SwapWeapon: return k.tabKey;
                case GameAction.NextSpell: return k.cKey;
                case GameAction.CastSpell: return k.tKey;
                case GameAction.Camera: return k.vKey;
                case GameAction.Pause: return k.escapeKey;
                case GameAction.Confirm: return k.enterKey;
                case GameAction.Back: return k.escapeKey;
                case GameAction.Restart: return k.rKey;
            }
            return null;
        }
        /// Set while a loading veil is up: gameplay reads no input.
        public static bool Blocked;
        public static bool Pressed(GameAction a)
        {
            if (Blocked) return false;
            if (instance && (a == GameAction.Attack && instance.mouseTapFrame == Time.frameCount ||
                a == GameAction.HeavyAttack && instance.mouseChargeFrame == Time.frameCount)) return true;
            var kb = KeyButton(a);
            if (kb != null && kb.wasPressedThisFrame) return true;
            if (a == GameAction.Pause && K != null && K.iKey.wasPressedThisFrame) return true;
            if (a == GameAction.Confirm && K != null && K.numpadEnterKey.wasPressedThisFrame) return true;
            return PadPressed(a);
        }
        public static bool PadPressed(GameAction a)
        {
            if (a == GameAction.LockOn) return lockTapFrame == Time.frameCount;
            if (a == GameAction.Camera) return cameraHoldFrame == Time.frameCount;
            foreach (var g in Gamepad.all)
            {
                // Weapon art = L1 + R1 together (Souls style); R1 alone is a plain attack.
                if (a == GameAction.Special) { if (g.rightShoulder.wasPressedThisFrame && g.leftShoulder.isPressed || g.leftShoulder.wasPressedThisFrame && g.rightShoulder.isPressed) return true; continue; }
                if (a == GameAction.Attack && g.leftShoulder.isPressed) continue;
                var pb = PadButton(a, g); if (pb != null && pb.wasPressedThisFrame) return true;
            }
            return false;
        }
        static bool PadHeld(GameAction a) { foreach (var g in Gamepad.all) { if (a == GameAction.Attack && g.leftShoulder.isPressed) continue; var pb = PadButton(a, g); if (pb != null && pb.isPressed) return true; } return false; }
        public static bool KeyPressed(GameAction a) { var kb = KeyButton(a); return kb != null && kb.wasPressedThisFrame; }
        public static bool Held(GameAction a)
        {
            if (Blocked) return false;
            if (a == GameAction.Attack) return instance && instance.mouseTapFrame == Time.frameCount || PadHeld(a);
            if (a == GameAction.HeavyAttack && instance && instance.mouseCharging) return true;
            if (a == GameAction.Sprint && sprintLatched) return true;
            var kb = KeyButton(a);
            return kb != null && kb.isPressed || PadHeld(a);
        }
        /// Movement in local space (x strafe, y forward), magnitude ≤ 1.
        public static Vector2 Move()
        {
            if (Blocked) return Vector2.zero;
            var k = K; Vector2 v = Vector2.zero;
            if (k != null) v = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            if (v.sqrMagnitude > 0) return v.normalized;
            Vector2 s = Vector2.zero; foreach (var g in Gamepad.all) { var v2 = g.leftStick.ReadValue(); if (v2.sqrMagnitude > s.sqrMagnitude) s = v2; }
            float m = s.magnitude;
            if (m < .18f) return Vector2.zero;
            return s / m * Mathf.Clamp01((m - .18f) / .72f);
        }
        /// Camera rotation this frame in degrees (x yaw, y pitch-up).
        public static Vector2 Look(float mouseSensitivity)
        {
            Vector2 look = M != null ? M.delta.ReadValue() * mouseSensitivity : Vector2.zero;
            Vector2 s = Vector2.zero; foreach (var g in Gamepad.all) { var v2 = g.rightStick.ReadValue(); if (v2.sqrMagnitude > s.sqrMagnitude) s = v2; }
            float m = s.magnitude;
            if (m > .15f)
            {
                var curved = s / m * Mathf.Pow(Mathf.Clamp01((m - .15f) / .85f), 1.6f);
                look += new Vector2(curved.x * 190f, curved.y * 120f * (InvertY ? -1 : 1)) * PadSensitivity * Time.deltaTime;
            }
            return look;
        }
        public static float WeaponScroll() => M != null ? M.scroll.ReadValue().y : 0;
        public static void Rumble(float low, float high, float seconds)
        {
            if (!UsingGamepad || instance == null) return;
            ActivePad?.SetMotorSpeeds(low, high); instance.rumbleUntil=Time.unscaledTime+Mathf.Clamp(seconds,0,.5f);
        }
        void StopRumble() { foreach (var g in Gamepad.all) g.SetMotorSpeeds(0, 0); }
        void OnDisable() { foreach (var g in Gamepad.all) g.SetMotorSpeeds(0, 0); }

        // ---------------- labels ----------------
        public static string KeyLabel(GameAction a)
        {
            switch (a)
            {
                case GameAction.Attack: return "Clic izq.";
                case GameAction.Block: return "Clic der.";
                case GameAction.Dodge: return "Mayús";
                case GameAction.Jump: return "Espacio";
                case GameAction.Sprint: return "Ctrl (mantener)";
                case GameAction.Interact: return "E";
                case GameAction.Special: return "Q";
                case GameAction.HeavyAttack: return "Mantener clic izq. o Z y soltar";
                case GameAction.Flask: return "H";
                case GameAction.Relic1: return "F";
                case GameAction.Relic2: return "G";
                case GameAction.Reload: return "R";
                case GameAction.LockOn: return "Clic central";
                case GameAction.SwapWeapon: return "Tab / Rueda";
                case GameAction.NextSpell: return "C";
                case GameAction.CastSpell: return "T";
                case GameAction.Camera: return "V";
                case GameAction.Pause: return "Esc / I";
                case GameAction.Confirm: return "Enter";
                case GameAction.Back: return "Esc";
                case GameAction.Restart: return "R";
                case GameAction.Move: return "WASD";
                case GameAction.Look: return "Ratón";
            }
            return "?";
        }
        /// Button icon(s) from the CindersButtons TMP sprite asset — what prompts show while a pad is in use.
        public static string PadLabel(GameAction a) => a == GameAction.Special ? (PlayStation ? Glyph("ps_l1") + "+" + Glyph("ps_r1") : Glyph("xb_lb") + "+" + Glyph("xb_rb")) : Glyph(GlyphName(a));
        public static string Glyph(string name) => $"<sprite=\"CindersButtons\" name=\"{name}\">";
        static string GlyphName(GameAction a)
        {
            bool ps = PlayStation;
            switch (a)
            {
                case GameAction.Attack: return ps ? "ps_r1" : "xb_rb";
                case GameAction.Block: return ps ? "ps_l1" : "xb_lb";
                case GameAction.Dodge: case GameAction.Back: return ps ? "ps_circle" : "xb_b";
                case GameAction.Jump: case GameAction.Confirm: return ps ? "ps_cross" : "xb_a";
                case GameAction.Sprint: return ps ? "ps_l3" : "xb_ls";
                case GameAction.Interact: return ps ? "ps_square" : "xb_x";
                case GameAction.HeavyAttack: return ps ? "ps_r2" : "xb_rt";
                case GameAction.CastSpell: return ps ? "ps_l2" : "xb_lt";
                case GameAction.Flask: case GameAction.Restart: return ps ? "ps_triangle" : "xb_y";
                case GameAction.Relic1: return "dpad_left";
                case GameAction.Relic2: return "dpad_right";
                case GameAction.NextSpell: return "dpad_up";
                case GameAction.SwapWeapon: case GameAction.Reload: return "dpad_down";
                case GameAction.LockOn: return ps ? "ps_r3" : "xb_rs";
                case GameAction.Camera: return ps ? "ps_r3" : "xb_rs";
                case GameAction.Pause: return ps ? "ps_options" : "xb_menu";
                case GameAction.Move: return "stick_l";
                case GameAction.Look: return "stick_r";
            }
            return "xb_a";
        }
        /// Spoken name of the button ("Círculo", "RB"), used beside the icon in the controls page.
        public static string PadText(GameAction a)
        {
            bool ps = PlayStation;
            switch (a)
            {
                case GameAction.Attack: return ps ? "R1" : "RB";
                case GameAction.Block: return ps ? "L1" : "LB";
                case GameAction.Dodge: return ps ? "Círculo" : "B";
                case GameAction.Jump: return ps ? "Cruz" : "A";
                case GameAction.Sprint: return ps ? "L3" : "LS (pulsar)";
                case GameAction.Interact: return ps ? "Cuadrado" : "X";
                case GameAction.HeavyAttack: return ps ? "R2 (mantener)" : "RT (mantener)";
                case GameAction.Special: return ps ? "L1 + R1 a la vez" : "LB + RB a la vez";
                case GameAction.CastSpell: return ps ? "L2" : "LT";
                case GameAction.Flask: return ps ? "Triángulo" : "Y";
                case GameAction.Relic1: return "Cruceta izq.";
                case GameAction.Relic2: return "Cruceta der.";
                case GameAction.NextSpell: return "Cruceta arriba";
                case GameAction.SwapWeapon: case GameAction.Reload: return "Cruceta abajo";
                case GameAction.LockOn: return ps ? "R3 (pulsar)" : "RS (pulsar)";
                case GameAction.Camera: return ps ? "R3 (mantener)" : "RS (mantener)";
                case GameAction.Pause: return ps ? "Options" : "Menú";
                case GameAction.Confirm: return ps ? "Cruz" : "A";
                case GameAction.Back: return ps ? "Círculo" : "B";
                case GameAction.Restart: return ps ? "Triángulo" : "Y";
                case GameAction.Move: return "Stick izq.";
                case GameAction.Look: return "Stick der.";
            }
            return "?";
        }
        public static string Label(GameAction a) => UsingGamepad ? PadLabel(a) : KeyLabel(a);
        /// Compact version for small HUD badges.
        public static string ShortLabel(GameAction a)
        {
            if (!UsingGamepad) return a == GameAction.SwapWeapon ? "Tab" : KeyLabel(a);
            return PadLabel(a);
        }

        static readonly Dictionary<string, GameAction> Tokens = new Dictionary<string, GameAction>
        {
            {"E",GameAction.Interact},{"Z",GameAction.HeavyAttack},{"Q",GameAction.Special},{"H",GameAction.Flask},{"R",GameAction.Reload},{"TAB",GameAction.SwapWeapon},
            {"C",GameAction.NextSpell},{"T",GameAction.CastSpell},{"F",GameAction.Relic1},{"G",GameAction.Relic2},{"V",GameAction.Camera},
            {"ESPACIO",GameAction.Jump},{"SPACE",GameAction.Jump},{"SHIFT",GameAction.Dodge},{"MAYÚS",GameAction.Dodge},{"MAYUS",GameAction.Dodge},
            {"CTRL",GameAction.Sprint},{"ESCAPE",GameAction.Pause},{"ESC",GameAction.Pause},{"I",GameAction.Pause},{"ENTER",GameAction.Confirm},
            {"CLIC IZQUIERDO",GameAction.Attack},{"CLIC IZQ.",GameAction.Attack},{"CLIC DERECHO",GameAction.Block},{"CLIC DER.",GameAction.Block},
            {"CLIC CENTRAL",GameAction.LockOn},{"WASD",GameAction.Move},{"RATÓN",GameAction.Look},{"RATON",GameAction.Look},
        };
        static readonly Regex Bracket = new Regex(@"\[([^\[\]<>]{1,24})\]", RegexOptions.Compiled);
        static readonly Regex KeyColon = new Regex(@"(?m)(^|\n|\. )([QEHRFGTCV]): ", RegexOptions.Compiled);
        static readonly Regex Clicks = new Regex(@"\b[Cc]lic (izquierdo|derecho|central)\b", RegexOptions.Compiled);
        /// Rewrites keyboard prompts ("[E] Abrir", "Clic derecho: bloquear") for the device in use.
        public static string Prompt(string text)
        {
            if (string.IsNullOrEmpty(text) || !UsingGamepad) return text;
            if (text.IndexOf('[') >= 0) text = Bracket.Replace(text, m =>
            {
                var parts = m.Groups[1].Value.Split('/'); bool any = false; var seen = new List<string>();
                foreach (var raw in parts)
                {
                    var key = raw.Trim().ToUpperInvariant();
                    if (Tokens.TryGetValue(key, out var a)) { any = true; var l = PadLabel(a); if (!seen.Contains(l)) seen.Add(l); }
                    else if (key.Length > 1 || char.IsDigit(key[0])) seen.Add(raw.Trim());
                }
                if (!any) return m.Value;
                return string.Join(" / ", seen);
            });
            text = KeyColon.Replace(text, m => Tokens.TryGetValue(m.Groups[2].Value, out var k) ? m.Groups[1].Value + PadLabel(k) + " " : m.Value);
            if (text.IndexOf("lic ", System.StringComparison.OrdinalIgnoreCase) >= 0)
                text = Clicks.Replace(text, m => m.Groups[1].Value == "izquierdo" ? PadLabel(GameAction.Attack) : m.Groups[1].Value == "derecho" ? PadLabel(GameAction.Block) : PadLabel(GameAction.LockOn));
            return text;
        }
    }
}
