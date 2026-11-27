using System.Collections;
using System.Linq;
using Codice.Characters;
using Codice.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Codice.Prologue
{
    public sealed partial class PrologueDirector : MonoBehaviour
    {
        public PlayerCharacter player;
        public GameObject floorPrefab, wallPrefab, gatePrefab, torchPrefab, propPrefab, npcPrefab;
        [Tooltip("0 creates a new layout every descent; any other value reproduces that layout.")]
        public int fixedSeed;
        public int Seed { get; private set; }
        public bool IntroFinished { get; private set; }
        public bool DialogueOpen => dialogue && dialogue.activeSelf;
        public bool HasSpoken { get; private set; }
        public Vector3 ExitPosition { get; private set; }
        public Transform Npc { get; private set; }
        public PrologueLayout Layout { get; private set; }
        Canvas canvas;
        CanvasGroup curtain;
        GameObject dialogue, pause;
        TextMeshProUGUI words, prompt, objective, page;
        PlayerCombat combat;
        int line;
        float letters;
        bool leaving;
        System.Collections.Generic.List<string> speech = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<VigiaLessons.Lesson> freshLessons = new System.Collections.Generic.List<VigiaLessons.Lesson>();
        readonly string[] intro = {
            "Respiras. Bien. La ceniza todavía no ha escrito tu nombre en el muro.",
            "Fui el primer centinela de este descenso. Juré no dormir hasta que el último peregrino volviera. Ninguno volvió, y el juramento no me deja morir.",
            "Por eso me llaman el Vigía. Me vendé los ojos para no ver quién no regresa; cuento sus pasos y cuelgo sus nombres de esta cadena. Cada vela que llevo fue alguien que dudó.",
            "No dudes tú. El calabozo cambia de forma, pero castiga igual.",
            "Sigue las brasas hasta el umbral rojo. Abajo duermen tres Orbes y un eclipse que yo ayudé a encender. Tráeme su final, o enciendo otra vela con tu nombre."
        };
        void Awake()
        {
            Seed = fixedSeed != 0 ? fixedSeed : System.Guid.NewGuid().GetHashCode();
            Layout = new PrologueLayout(Seed); VigiaLessons.CountRun();
            if (!Layout.IsConnected()) throw new System.InvalidOperationException("Disconnected prologue");
            combat = player.GetComponent<PlayerCombat>();
            BuildDungeon(); BuildUI(); LockControls(true);
            Debug.Log($"[Prologue] Generated seed={Seed}, rooms={Layout.Rooms.Count}, cells={Layout.Cells.Count}, connected=true");
        }
        Vector3 World(Vector2Int cell) => new Vector3(cell.x * 4, 0, cell.y * 4);
        GameObject Place(GameObject prefab, Vector3 position, float yaw, Transform parent)
        {
            var instance = Instantiate(prefab, position, Quaternion.Euler(0, yaw, 0), parent);
            instance.SetActive(true); return instance;
        }
        void BuildDungeon()
        {
            var root = new GameObject("Procedural dungeon · " + Seed).transform;
            root.SetParent(transform);
            var sides = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            foreach (var cell in Layout.Cells.OrderBy(c => c.y).ThenBy(c => c.x))
            {
                var position = World(cell);
                Place(floorPrefab, position - Vector3.up * .35f, 0, root);
                for (int i = 0; i < 4; i++)
                {
                    if (Layout.Cells.Contains(cell + sides[i])) continue;
                    var delta = new Vector3(sides[i].x, 0, sides[i].y);
                    Place((cell.x + cell.y) % 7 == 0 ? gatePrefab : wallPrefab, position + delta * 2, i * 90, root);
                    if ((cell.x + cell.y) % 3 == 0)
                    {
                        var torch = Place(torchPrefab, position + delta * 1.65f + Vector3.up * 1.4f, i * 90, root);
                        var light = new GameObject("Ember light").AddComponent<Light>();
                        light.transform.SetParent(torch.transform); light.transform.position = torch.transform.position - delta * .45f + Vector3.up * .75f;
                        light.type = LightType.Point; light.color = new Color(1, .43f, .16f); light.range = 9; light.intensity = 2.4f;
                        light.shadows = LightShadows.None;
                    }
                }
            }
            foreach (var room in Layout.Rooms)
            {
                Place(propPrefab, World(room) + new Vector3(-4.5f, 0, 4.5f), 15, root);
                Place(propPrefab, World(room) + new Vector3(4.5f, 0, -4.5f), -20, root);
            }
            var npc = Place(npcPrefab, new Vector3(0, 0, 1.4f), 180, root);
            npc.name = "El Vigía · NPC"; Npc = npc.transform;
            Codice.Presentation.VigiaLook.Dress(npc);
            foreach (var t in npc.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
            var npcCollider = npc.AddComponent<CapsuleCollider>(); npcCollider.height = 2; npcCollider.radius = .4f; npcCollider.center = Vector3.up;
            var warm = new GameObject("Vigía lantern glow").AddComponent<Light>(); warm.transform.SetParent(root);
            warm.transform.position = new Vector3(-1.8f, 2.8f, -.7f); warm.type = LightType.Point;
            warm.range = 11; warm.intensity = 3; warm.color = new Color(1, .68f, .4f);
            ExitPosition = World(Layout.Exit) + Vector3.forward * 1;
            var exit = new GameObject("The red threshold").transform; exit.SetParent(root); exit.position = ExitPosition;
            var marker = exit.gameObject.AddComponent<Light>(); marker.type = LightType.Point;
            marker.color = new Color(1, .08f, .035f); marker.intensity = 5; marker.range = 12;
            var label = new GameObject("Threshold inscription").AddComponent<TextMeshPro>(); label.transform.SetParent(exit, false);
            label.transform.localPosition = new Vector3(0, 2.7f, 0); label.transform.localRotation = Quaternion.identity;
            label.text = "EL UMBRAL"; label.font = Resources.Load<TMP_FontAsset>("CindersFonts/UncialAntiqua");
            label.fontSize = 5; label.alignment = TextAlignmentOptions.Center; label.color = new Color(.85f,.18f,.1f);
            label.rectTransform.sizeDelta = new Vector2(5, 1);
            BuildPaths(root);
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0, .1f, -2.8f), Quaternion.identity); cc.enabled = true;
            Physics.SyncTransforms();
        }
        RectTransform Anchored(string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = UIWidgets.Rect(canvas.transform, name, 0, 0, size.x, size.y);
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; return rect;
        }
        TextMeshProUGUI Label(Transform parent, string name, string text, float x, float y, float w, float h, int size, Color color)
        { var label = UIWidgets.Label(parent, name, x, y, w, h, size, color); label.text = text; return label; }
        void BuildUI()
        {
            canvas = new GameObject("Prologue interface", typeof(RectTransform)).AddComponent<Canvas>();
            UIWidgets.ConfigureCanvas(canvas); canvas.sortingOrder = 100; canvas.gameObject.AddComponent<GraphicRaycaster>();
            var ivory = new Color(.86f,.82f,.72f); var gold = new Color(.46f,.36f,.23f);
            var heading = Anchored("Location", new Vector2(0,1), new Vector2(0,1), new Vector2(48,-330), new Vector2(1100,240));
            Label(heading, "Chapter", "PRÓLOGO  /  LA CELDA DE CENIZA", 0,0,950,50,25,gold);
            objective = Label(heading,"Objective","Escucha al Vigía",0,52,1100,180,24,ivory);
            var hint = Anchored("Interaction",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,100),new Vector2(1200,60));
            prompt = Label(hint,"Prompt","",0,0,1200,60,25,ivory); prompt.alignment = TextAlignmentOptions.Center;
            var dot = Anchored("Crosshair", new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(4,4));
            dot.gameObject.AddComponent<Image>().color = new Color(.8f,.75f,.65f,.6f);
            var panel = Anchored("Vigía dialogue",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,55),new Vector2(1660,280));
            dialogue = panel.gameObject;
            var background = dialogue.AddComponent<Image>(); background.color = new Color(.035f,.03f,.028f,.97f);
            UIWidgets.Image(panel,"Top bronze rule",0,0,1660,2,gold);
            UIWidgets.Image(panel,"Bottom bronze rule",0,278,1660,2,gold);
            UIWidgets.Image(panel,"Portrait background",20,17,238,246,new Color(.07f,.06f,.055f));
            var portrait = UIWidgets.Rect(panel,"Vigía portrait",20,17,238,246).gameObject.AddComponent<RawImage>();
            var showcase=portrait.gameObject.AddComponent<MenuModelShowcase>();showcase.vigia=true;showcase.Initialize(portrait,"Class_Mage");
            Label(panel,"Speaker","EL VIGÍA",295,25,1080,43,29,new Color(.7f,.53f,.32f));
            words = Label(panel,"Dialogue text","",295,88,1285,114,27,ivory);
            // Dialogue copy uses the existing readable body font, not ornamental capitals.
            words.font = Resources.Load<TMP_FontAsset>("CindersFonts/CrimsonText");
            page = Label(panel,"Page","",295,224,350,32,18,gold);
            UIWidgets.Button(panel,"CONTINUAR  [E / ENTER]",1135,214,480,48,AdvanceDialogue);
            dialogue.SetActive(false);
            var pauseRect = Anchored("Pause",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(560,300));
            pause = pauseRect.gameObject; pause.AddComponent<Image>().color = new Color(.025f,.02f,.02f,.98f);
            Label(pauseRect,"Title","DESCENSO EN PAUSA",35,30,490,70,29,ivory);
            UIWidgets.Button(pauseRect,"CONTINUAR",40,125,480,55,()=>SetPaused(false));
            UIWidgets.Button(pauseRect,"VOLVER AL MENÚ",40,200,480,55,RunNavigation.MainMenu); pause.SetActive(false);
            var black = new GameObject("Blood curtain",typeof(RectTransform)).AddComponent<Image>(); black.transform.SetParent(canvas.transform,false);
            black.rectTransform.anchorMin = Vector2.zero; black.rectTransform.anchorMax = Vector2.one;
            black.rectTransform.offsetMin = black.rectTransform.offsetMax = Vector2.zero; black.color = new Color(.008f,.004f,.006f);
            curtain = black.gameObject.AddComponent<CanvasGroup>();
            var titleRoot = UIWidgets.Rect(black.transform,"Descent title",0,0,1600,320);
            titleRoot.anchorMin = titleRoot.anchorMax = titleRoot.pivot = new Vector2(.5f,.5f); titleRoot.anchoredPosition = Vector2.zero;
            var title = Label(titleRoot,"Blood title","INICIA EL DESCENSO",0,70,1600,130,88,new Color(.6f,.045f,.055f));
            title.alignment = TextAlignmentOptions.Center; title.characterSpacing = 8;
            title.fontStyle = FontStyles.Bold;
            for (int i=0;i<19;i++)
                UIWidgets.Image(titleRoot,"Blood trace "+i,225+i*63,177,1+(i%3),12+(i*17%49),new Color(.34f,.025f,.035f,.75f));
            var subtitle = Label(titleRoot,"Subtitle","LA CELDA DE CENIZA",0,260,1600,55,25,new Color(.52f,.45f,.4f));
            subtitle.alignment = TextAlignmentOptions.Center; subtitle.characterSpacing = 12;
        }
        IEnumerator Start()
        {
            yield return null; LockControls(true);
            while(RunNavigation.Loading) yield return null;
            yield return new WaitForSecondsRealtime(2.6f);
            yield return curtain.DOFade(0,1.5f).SetUpdate(true).WaitForCompletion();
            curtain.gameObject.SetActive(false); IntroFinished = true; InstallHud();
            Debug.Log("[Prologue] Curtain finished; meeting the Vigía");
            OpenDialogue();
        }
        void LockControls(bool blocked)
        {
            if(player) player.SetNarrativeControlsBlocked(blocked);
            if(combat) combat.enabled = !blocked;
            Cursor.lockState = blocked ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = blocked;
        }
        public void OpenDialogue()
        {
            if (!IntroFinished || leaving || RunNavigation.Loading) return;
            speech = VigiaLessons.Build(player.State.Class, intro, out freshLessons);
            speech.Add(PathsLine());
            dialogue.SetActive(true); prompt.text = ""; line = 0; ShowLine(); LockControls(true);
            Debug.Log("[Prologue] Dialogue opened");
        }
        void ShowLine()
        { words.text = speech[line]; words.maxVisibleCharacters = 0; letters = 0; page.text = $"{line+1:00}  /  {speech.Count:00}"; }
        public void AdvanceDialogue()
        {
            if(!DialogueOpen) return;
            if(words.maxVisibleCharacters < words.text.Length) { words.maxVisibleCharacters = words.text.Length; letters = words.text.Length; return; }
            line++;
            if(line < speech.Count) { ShowLine(); return; }
            dialogue.SetActive(false); HasSpoken = true; VigiaLessons.MarkHeard(freshLessons); OpenPaths();
            LockControls(false); Debug.Log("[Prologue] Dialogue completed; exit unlocked");
        }
        void SetPaused(bool value) { pause.SetActive(value); LockControls(value); }
        public void EnterThreshold()
        {
            if(leaving || !HasSpoken || DialogueOpen || !IntroFinished || !NearAnyExit()) return;
            leaving = true; LockControls(true); Debug.Log("[Prologue] Entering first combat round"); RunNavigation.ContinueAfterPrologue();
        }
        void Update()
        {
            if(!IntroFinished || leaving || RunNavigation.Loading) return;
            var keys = Keyboard.current;
            if(DialogueOpen)
            {
                letters += Time.unscaledDeltaTime * 40; words.maxVisibleCharacters = Mathf.Min(words.text.Length,(int)letters);
                if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Interact) || Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Confirm) || Codice.Mechanics.GameInput.KeyPressed(Codice.Mechanics.GameAction.Jump)) AdvanceDialogue();
                return;
            }
            if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Pause) || pause.activeSelf && Codice.Mechanics.GameInput.PadPressed(Codice.Mechanics.GameAction.Back)) { SetPaused(!pause.activeSelf); return; }
            if(pause.activeSelf) return;
            bool nearNpc = Vector3.Distance(player.transform.position,Npc.position)<4.8f;
            bool nearExit = HasSpoken && NearAnyExit();
            TickTrial();
            prompt.text = Codice.Mechanics.GameInput.Prompt(nearNpc ? "[E]  HABLAR CON EL VIGÍA" : nearExit ? "[E]  CRUZAR EL UMBRAL" : "[WASD]  Moverte     [Mayús]  Esquivar     [Esc]  Pausa");
            if(Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Interact)) { if(nearNpc)OpenDialogue(); else if(nearExit)EnterThreshold(); }
        }
        void OnDestroy()
        {
            if(curtain) curtain.DOKill();
            if(canvas) Destroy(canvas.gameObject);
        }
    }
}
