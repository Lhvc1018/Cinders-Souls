using Codice.Mechanics;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;
namespace Codice.UI
{
    /// Settings page listing every control for keyboard + mouse and gamepad. The column of the device
    /// you are holding lights up, and pad labels follow the controller family (Xbox / PlayStation).
    public sealed class ControlsSheet : MonoBehaviour
    {
        static readonly (string name, GameAction action)[] Rows =
        {
            ("Moverse",GameAction.Move),("Mover la cámara",GameAction.Look),("Atacar",GameAction.Attack),("Ataque cargado",GameAction.HeavyAttack),("Bloquear / parry",GameAction.Block),
            ("Esquivar",GameAction.Dodge),("Saltar",GameAction.Jump),("Correr",GameAction.Sprint),("Interactuar / recoger",GameAction.Interact),
            ("Especial del arma",GameAction.Special),("Frasco de ceniza",GameAction.Flask),("Reliquia activa 1",GameAction.Relic1),("Reliquia activa 2",GameAction.Relic2),
            ("Cambiar de arma",GameAction.SwapWeapon),("Recargar",GameAction.Reload),("Rotar hechizos preparados",GameAction.NextSpell),("Lanzar hechizo aprendido",GameAction.CastSpell),
            ("Fijar objetivo",GameAction.LockOn),("Pausa e inventario",GameAction.Pause),("Volver en menús",GameAction.Back),
        };
        Text device, keyColumnTitle, padColumnTitle; Text[] keyCells, padCells; Image keyBand, padBand;
        InputScheme shown = (InputScheme)(-1); string shownPad;
        static readonly Color Gold = new Color(.86f, .72f, .43f), Cream = new Color(.91f, .88f, .77f), Muted = new Color(.52f, .50f, .44f);
        public static ControlsSheet Build(Transform parent, UnityEngine.Events.UnityAction back)
        {
            var root = UIWidgets.Surface(parent, "Controles", 0, 0, 1400, 900);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            root.GetComponent<Image>().color = new Color(.035f, .036f, .032f, .97f);
            var backing = UIWidgets.Image(root, "Opaque backing", 0, 0, 1400, 900, new Color(.03f, .031f, .028f, 1)); backing.raycastTarget = true; backing.transform.SetAsFirstSibling();
            var sheet = root.gameObject.AddComponent<ControlsSheet>();
            UIWidgets.Label(root, "Eyebrow", 60, 34, 900, 30, 18, Gold).text = "CINDERS SOULS    /    AJUSTES    /    CONTROLES";
            UIWidgets.Label(root, "Heading", 60, 70, 900, 64, 46, Cream).text = "Controles";
            sheet.device = UIWidgets.Label(root, "Device", 60, 138, 1280, 32, 21, Muted);
            UIWidgets.Ornament(root, 60, 180, 1280);
            sheet.keyBand = UIWidgets.Image(root, "Keyboard column", 560, 196, 380, 26 * Rows.Length + 48, new Color(.86f, .72f, .43f, .07f));
            sheet.padBand = UIWidgets.Image(root, "Gamepad column", 960, 196, 380, 26 * Rows.Length + 48, new Color(.86f, .72f, .43f, .07f));
            UIWidgets.Label(root, "Action column", 70, 202, 480, 34, 19, Gold).text = "ACCIÓN";
            sheet.keyColumnTitle = UIWidgets.Label(root, "Keyboard title", 580, 202, 360, 34, 19, Gold);
            sheet.padColumnTitle = UIWidgets.Label(root, "Gamepad title", 980, 202, 360, 34, 19, Gold);
            sheet.keyCells = new Text[Rows.Length]; sheet.padCells = new Text[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
            {
                float y = 236 + i * 26;
                if (i % 2 == 0) UIWidgets.Image(root, "Row " + i, 60, y - 1, 1280, 25, new Color(1, 1, 1, .025f));
                UIWidgets.Label(root, "Action " + i, 70, y, 480, 25, 17, Cream).text = Rows[i].name;
                sheet.keyCells[i] = UIWidgets.Label(root, "Key " + i, 580, y, 360, 25, 17, Cream);
                sheet.padCells[i] = UIWidgets.Label(root, "Pad " + i, 980, y, 360, 25, 17, Cream);
            }
            UIWidgets.Label(root, "Tips", 60, 800, 1000, 60, 16, Muted).text =
                "Parry: pulsa bloquear justo antes del impacto. Correr con mando: pulsa el stick izquierdo; se mantiene mientras te muevas.\nLos menús se recorren con el stick o la cruceta. El juego detecta solo el dispositivo que uses.";
            UIWidgets.Button(root, "VOLVER", 1100, 806, 240, 52, back);
            sheet.Refresh(true);
            return sheet;
        }
        void OnEnable() => Refresh(true);
        void Update() => Refresh(false);
        void Refresh(bool force)
        {
            if (keyCells == null) return;
            var pad = GameInput.PadName;
            if (!force && shown == GameInput.Scheme && shownPad == pad) return;
            shown = GameInput.Scheme; shownPad = pad;
            bool gp = GameInput.UsingGamepad;
            device.text = gp ? $"Dispositivo activo: <color=#DDB86E>MANDO</color> · {pad} · Se cambia solo al tocar el teclado o el ratón."
                             : $"Dispositivo activo: <color=#DDB86E>TECLADO Y RATÓN</color> · {(pad.Length > 0 ? "Mando conectado: " + pad + " (pulsa cualquier botón para usarlo)" : "Ningún mando conectado")}";
            keyColumnTitle.text = "TECLADO Y RATÓN" + (gp ? "" : "  •  EN USO");
            padColumnTitle.text = (GameInput.PlayStation ? "MANDO PLAYSTATION" : "MANDO XBOX") + (gp ? "  •  EN USO" : "");
            keyBand.color = new Color(.86f, .72f, .43f, gp ? .015f : .07f); padBand.color = new Color(.86f, .72f, .43f, gp ? .07f : .015f);
            for (int i = 0; i < Rows.Length; i++)
            {
                keyCells[i].text = GameInput.KeyLabel(Rows[i].action); padCells[i].text = GameInput.PadLabel(Rows[i].action) + "  " + GameInput.PadText(Rows[i].action);
                keyCells[i].color = gp ? Muted : Cream; padCells[i].color = gp ? Cream : Muted;
            }
        }
    }
}
