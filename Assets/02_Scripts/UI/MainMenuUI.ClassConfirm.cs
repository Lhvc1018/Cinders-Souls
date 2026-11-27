using Codice.Mechanics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TextMeshProUGUI;
namespace Codice.UI
{
    // Class pick: press the same legacy twice (double click / double confirm) → "¿Seguro?" → descend.
    public sealed partial class MainMenuUI
    {
        RectTransform confirmPanel;
        Text confirmText, classHint;
        Button confirmYes;
        int armedClass = -1;
        void ClassPressed(int index)
        {
            if (confirmPanel && confirmPanel.gameObject.activeSelf) return;
            if (armedClass == index && selectedClass == index) { AskConfirm(); return; }
            PreviewClass(index); armedClass = index; RefreshClassHint();
        }
        void RefreshClassHint()
        {
            if (!classHint) return;
            string confirm = GameInput.UsingGamepad ? GameInput.PadLabel(GameAction.Confirm) : "Clic / [Enter]";
            classHint.text = armedClass == selectedClass && armedClass >= 0
                ? $"Pulsa {confirm} otra vez sobre {classNames[selectedClass]} para elegirlo."
                : $"Elige un legado y pulsa {confirm} dos veces para confirmarlo.";
        }
        void BuildClassConfirm(Transform root)
        {
            classHint = classes.Find("Controls")?.GetComponent<Text>();
            confirmPanel = Centered(root, "Confirmar legado", 1920, 1080);
            var shade = UIWidgets.Image(confirmPanel, "Shade", 0, 0, 1920, 1080, new Color(0, 0, 0, .72f)); shade.raycastTarget = true;
            UIWidgets.Image(confirmPanel, "Card", 560, 330, 800, 400, new Color(.04f, .04f, .035f, .98f));
            UIWidgets.Ornament(confirmPanel, 600, 360, 720);
            Label(confirmPanel, "Eyebrow", 600, 380, 720, 30, 18, Gold, "ELEGIR LEGADO");
            confirmText = Label(confirmPanel, "Question", 600, 420, 720, 140, 30, Cream, ""); confirmText.alignment = TextAlignmentOptions.Center;
            confirmYes = Button(confirmPanel, "SÍ, DESCENDER", 600, 590, 350, 60, () => { confirmPanel.gameObject.SetActive(false); Launch(); }, true);
            Button(confirmPanel, "NO, VOLVER", 970, 590, 350, 60, () => CloseConfirm());
            confirmPanel.gameObject.SetActive(false);
            RefreshClassHint();
        }
        void AskConfirm()
        {
            confirmText.text = $"¿Seguro que quieres usar {classNames[selectedClass]}\ny {(choosingEndless ? "entrar al Coliseo infinito" : "iniciar el descenso")}?";
            confirmPanel.gameObject.SetActive(true); confirmPanel.SetAsLastSibling(); FadeIn(confirmPanel); confirmYes.Select();
        }
        void CloseConfirm()
        {
            confirmPanel.gameObject.SetActive(false); armedClass = -1; RefreshClassHint();
            if (selectedClass >= 0 && selectedClass < classButtons.Length && classButtons[selectedClass]) classButtons[selectedClass].Select();
        }
        void Launch()
        {
            if (choosingEndless) RunNavigation.StartEndless(classKinds[selectedClass]);
            else if (CheatsUnlocked) RunNavigation.StartExpeditionTest(classKinds[selectedClass], CheatRound);
            else RunNavigation.StartGame(classKinds[selectedClass], 1);
        }
        /// Snappier stick/arrow navigation in menus (Unity's default waits half a second before repeating).
        static void TuneMenuNavigation()
        {
            var module = UnityEngine.EventSystems.EventSystem.current ? UnityEngine.EventSystems.EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() : null;
            if (module) { module.moveRepeatDelay = .28f; module.moveRepeatRate = .09f; }
        }
    }
}
