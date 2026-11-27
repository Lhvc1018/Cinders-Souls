using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Codice.UI
{
    /// Black loading veil between floors: fades in before the scene swap, hides the procedural build and the prewarm
    /// hitches, blocks gameplay input (GameInput.Blocked) and fades out once the new floor reports it is ready.
    public sealed class FloorTransition : MonoBehaviour
    {
        static FloorTransition current;
        public static bool Active => current;
        Image veil; TextMeshProUGUI title, hint; bool released; float shownAt;
        public static void Show(string heading, string subtitle)
        {
            if (current) { current.title.text = heading; current.hint.text = subtitle; current.released = false; return; }
            var root = new GameObject("Floor transition", typeof(RectTransform)); DontDestroyOnLoad(root);
            var t = current = root.AddComponent<FloorTransition>();
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1001;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();
            t.veil = new GameObject("Veil", typeof(RectTransform)).AddComponent<Image>(); t.veil.transform.SetParent(root.transform, false);
            var r = t.veil.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; t.veil.color = new Color(0, 0, 0, 0); t.veil.raycastTarget = true;
            t.title = Text(root.transform, heading, 54, new Vector2(0, 40), ProceduralUISprites.Gold);
            t.hint = Text(root.transform, subtitle, 24, new Vector2(0, -30), new Color(.75f, .72f, .66f));
            t.shownAt = Time.realtimeSinceStartup; Mechanics.GameInput.Blocked = true;
            t.StartCoroutine(t.Run());
        }
        /// Called by the new floor when its geometry, navmesh and prewarm are done.
        public static void Release() { if (current) current.released = true; }
        static TextMeshProUGUI Text(Transform parent, string s, float size, Vector2 pos, Color c)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>(); t.text = s; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = new Color(c.r, c.g, c.b, 0);
            var r = t.rectTransform; r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(1600, 90); r.anchoredPosition = pos; return t;
        }
        void SetAlpha(float a) { veil.color = new Color(0, 0, 0, a); foreach (var t in new[] { title, hint }) { var c = t.color; c.a = a; t.color = c; } }
        IEnumerator Run()
        {
            for (float t = 0; t < .35f; t += Time.unscaledDeltaTime) { SetAlpha(t / .35f); yield return null; }
            SetAlpha(1);
            // Wait for the floor (safety cap 20 s so a failed load never leaves the player stuck behind the veil).
            while (!released && Time.realtimeSinceStartup - shownAt < 20) yield return null;
            // Two extra beats: the first rendered frames of a fresh floor carry shader/instantiation hitches.
            for (float t = 0; t < .4f; t += Time.unscaledDeltaTime) yield return null;
            Mechanics.GameInput.Blocked = false;
            for (float t = 0; t < .45f; t += Time.unscaledDeltaTime) { SetAlpha(1 - t / .45f); yield return null; }
            current = null; Destroy(gameObject);
        }
        void OnDestroy() { if (current == this) { current = null; Mechanics.GameInput.Blocked = false; } }
    }
}
