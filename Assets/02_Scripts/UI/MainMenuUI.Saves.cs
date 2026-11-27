using Codice.Mechanics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;
namespace Codice.UI
{
    // Save slots ("Partidas"), Continue, and the Controls page in Ajustes.
    public sealed partial class MainMenuUI
    {
        Text footer, activeSlotLabel;
        ControlsSheet controlsSheet;
        RectTransform slotsPanel;
        Button continueButton;
        readonly Text[] slotTitles = new Text[SaveSlots.Count], slotInfo = new Text[SaveSlots.Count];
        readonly Button[] slotPlay = new Button[SaveSlots.Count], slotContinue = new Button[SaveSlots.Count], slotDelete = new Button[SaveSlots.Count];
        int armedDelete = -1; float armedUntil;

        void BuildSaveMenus(Transform root)
        {
            // Home: Continue + Partidas join the column; everything is re-stacked in LayoutHome.
            continueButton = Button(home, "CONTINUAR", 560, 500, 560, 50, () => RunNavigation.ContinueSaved(), true);
            Button(home, "PARTIDAS", 560, 560, 560, 46, () => ShowSlots(true));
            activeSlotLabel = Label(home, "Active slot", 60, 846, 520, 30, 17, Muted, "");
            // Options: pad sensitivity + Controls page.
            Button(options, "SENS. MANDO −", 40, 0, 325, 52, () => { GameInput.PadSensitivity -= .1f; RefreshOptions(); });
            Button(options, "SENS. MANDO +", 395, 0, 325, 52, () => { GameInput.PadSensitivity += .1f; RefreshOptions(); });
            Button(options, "INVERTIR EJE Y (MANDO)", 40, 0, 680, 52, () => { GameInput.InvertY = !GameInput.InvertY; RefreshOptions(); });
            Button(options, "CONTROLES", 40, 0, 680, 52, () => ShowControls(true));
            controlsSheet = ControlsSheet.Build(root, () => ShowControls(false));
            controlsSheet.gameObject.SetActive(false);
            BuildSlots(root);
        }
        void BuildSlots(Transform root)
        {
            slotsPanel = Centered(root, "Partidas", 1200, 860);
            UIWidgets.Image(slotsPanel, "Backing", 0, 0, 1200, 860, new Color(.035f, .036f, .032f, .96f));
            Label(slotsPanel, "Eyebrow", 60, 34, 900, 30, 18, Gold, "CINDERS SOULS    /    PARTIDAS");
            Label(slotsPanel, "Heading", 55, 70, 900, 70, 48, Cream, "Partidas");
            Label(slotsPanel, "Hint", 60, 146, 1080, 30, 19, Muted, "Cada partida guarda su propio progreso: lecciones del Vigía, llaves, finales y el descenso en curso.");
            for (int i = 0; i < SaveSlots.Count; i++)
            {
                int slot = i; float y = 200 + i * 190;
                UIWidgets.Image(slotsPanel, "Slot card " + i, 60, y, 1080, 170, new Color(.07f, .066f, .055f, .9f));
                slotTitles[i] = Label(slotsPanel, "Slot title " + i, 90, y + 16, 640, 40, 30, Gold, "");
                slotInfo[i] = Label(slotsPanel, "Slot info " + i, 90, y + 62, 640, 100, 19, Cream, "");
                slotPlay[i] = Button(slotsPanel, "USAR", 780, y + 14, 330, 46, () => { SaveSlots.Select(slot); armedDelete = -1; RefreshSlots(); ShowSlots(false); });
                slotContinue[i] = Button(slotsPanel, "CONTINUAR", 780, y + 64, 330, 46, () => { SaveSlots.Select(slot); RunNavigation.ContinueSaved(); }, true);
                slotDelete[i] = Button(slotsPanel, "BORRAR", 780, y + 114, 330, 40, () => DeleteSlot(slot));
            }
            Button(slotsPanel, "VOLVER", 60, 780, 300, 52, () => ShowSlots(false));
            slotsPanel.gameObject.SetActive(false);
        }
        void DeleteSlot(int slot)
        {
            if (armedDelete != slot || Time.unscaledTime > armedUntil) { armedDelete = slot; armedUntil = Time.unscaledTime + 4; RefreshSlots(); return; }
            SaveSlots.Delete(slot); armedDelete = -1; RefreshSlots(); RefreshHome(); slotPlay[slot].Select();
        }
        void RefreshSlots()
        {
            for (int i = 0; i < SaveSlots.Count; i++)
            {
                var d = SaveSlots.Get(i); bool active = SaveSlots.Active == i;
                slotTitles[i].text = (d.Empty ? $"PARTIDA {i + 1}" : d.name.ToUpperInvariant()) + (active ? "   <size=18><color=#DDB86E>•  ACTIVA</color></size>" : "");
                slotInfo[i].text = SaveSlots.Describe(d);
                SetLabel(slotPlay[i], d.Empty ? "CREAR PARTIDA" : active ? "EN USO" : "USAR ESTA PARTIDA");
                slotContinue[i].gameObject.SetActive(d.hasRun && !string.IsNullOrEmpty(d.runState));
                slotDelete[i].gameObject.SetActive(!d.Empty);
                SetLabel(slotDelete[i], armedDelete == i ? "¿SEGURO? PULSA OTRA VEZ" : "BORRAR");
            }
        }
        static void SetLabel(Button b, string text) { var t = b.GetComponentInChildren<Text>(); if (t) t.text = text; }
        void RefreshHome()
        {
            bool can = SaveSlots.CanContinue; var d = SaveSlots.Current;
            continueButton.gameObject.SetActive(can);
            if (can) SetLabel(continueButton, $"CONTINUAR  ·  {SaveSlots.ClassName(d.runClass).ToUpperInvariant()}  {SaveSlots.World(d.runFloor)}");
            activeSlotLabel.text = d != null && !d.Empty ? $"Partida activa: {d.name}" : "Sin partida activa · se creará al descender";
            LayoutHome();
        }
        /// Re-stacks the home column so Continue / Partidas fit above the footer.
        void LayoutHome()
        {
            string[] order = { "CONTINUAR", "INICIAR DESCENSO", "PARTIDAS", "COLISEO INFINITO", "COOPERATIVO", "OPCIONES", "SALIR" };
            float y = 505;
            foreach (var n in order)
            {
                var r = home.Find(n) as RectTransform; if (!r || !r.gameObject.activeSelf) continue;
                r.anchoredPosition = new Vector2(560, -y); r.sizeDelta = new Vector2(560, 46); y += 52;
            }
            var foot = home.Find("Footer") as RectTransform; if (foot) foot.anchoredPosition = new Vector2(350, -Mathf.Max(862, y + 8));
            string[] opts = { "VOLUMEN −", "SENSIBILIDAD −", "SENS. MANDO −", "INVERTIR EJE Y (MANDO)", "CAMBIAR PANTALLA COMPLETA", "CONTROLES" };
            var values = options.Find("Values") as RectTransform; if (values) { values.anchoredPosition = new Vector2(40, -282); values.sizeDelta = new Vector2(680, 150); }
            float oy = 430;
            void Row(string a, string b) { var ra = Find(options, a); var rb = b == null ? null : Find(options, b); if (ra) { ra.anchoredPosition = new Vector2(40, -oy); ra.sizeDelta = new Vector2(b == null ? 680 : 325, 46); } if (rb) { rb.anchoredPosition = new Vector2(395, -oy); rb.sizeDelta = new Vector2(325, 46); } oy += 52; }
            Row("VOLUMEN", "VOLUMEN +"); Row("SENSIBILIDAD", "SENSIBILIDAD +"); Row("SENS. MANDO −", "SENS. MANDO +"); Row("INVERTIR EJE Y (MANDO)", null); Row("CAMBIAR PANTALLA COMPLETA", null); Row("RESOLUCION 3D", null); Row("CONTROLES", null);   /* the 3D-resolution toggle was never laid out and sat on top of these */
            var back = options.Find("VOLVER") as RectTransform; if (back) { back.anchoredPosition = new Vector2(40, -(oy + 8)); back.sizeDelta = new Vector2(680, 52); } var saved = options.Find("Saved") as RectTransform; if (saved) saved.anchoredPosition = new Vector2(40, -(oy + 66));
        }
        // "VOLUMEN" finds the minus button whatever glyph the source file stored for "−".
        static RectTransform Find(Transform parent, string name)
        {
            var exact = parent.Find(name) as RectTransform; if (exact) return exact;
            foreach (RectTransform c in parent) if (c.name.StartsWith(name) && !c.name.EndsWith("+")) return c;
            return null;
        }
        void ShowSlots(bool show)
        {
            armedDelete = -1; RefreshSlots();
            home.gameObject.SetActive(!show); slotsPanel.gameObject.SetActive(show); FadeIn(show ? slotsPanel : home);
            if (!show) RefreshHome();
            (show ? slotPlay[Mathf.Max(0, SaveSlots.Active)] : (continueButton.gameObject.activeSelf ? continueButton : home.GetComponentInChildren<Button>())).Select();
        }
        void ShowControls(bool show)
        {
            options.gameObject.SetActive(!show); controlsSheet.gameObject.SetActive(show);
            FadeIn(show ? (RectTransform)controlsSheet.transform : options);
            (show ? controlsSheet.GetComponentInChildren<Button>() : Find(options, "CONTROLES")?.GetComponent<Button>())?.Select();
        }
        static readonly (string bad, string good)[] Mojibake = { ("Ã³","ó"),("Ã¡","á"),("Ã©","é"),("Ã­","í"),("Ãº","ú"),("Ã±","ñ"),("Ã“","Ó"),("Ã‰","É"),("Ãš","Ú"),("Ã‘","Ñ"),("Ã\u0081","Á"),("Ã\u008d","Í"),("â€¢","•"),("âˆ’","−"),("â†‘","↑"),("â†“","↓"),("â€”","—"),("â€“","–"),("Â·","·"),("Â¿","¿"),("Â¡","¡") };
        /// The authored menu layout stored some labels double-encoded ("prÃ³ximo"); repair them on load.
        static void RepairText(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                var s = t.text; if (string.IsNullOrEmpty(s) || (s.IndexOf('Ã') < 0 && s.IndexOf('â') < 0 && s.IndexOf('Â') < 0)) continue;
                foreach (var (bad, good) in Mojibake) s = s.Replace(bad, good);
                t.text = s;
            }
        }
        string DeviceLine() => GameInput.UsingGamepad ? $"MANDO EN USO: {GameInput.PadName}" : GameInput.PadName.Length > 0 ? $"TECLADO EN USO  ·  mando detectado: {GameInput.PadName}" : "TECLADO Y RATÓN EN USO  ·  ningún mando conectado";
    }
}
