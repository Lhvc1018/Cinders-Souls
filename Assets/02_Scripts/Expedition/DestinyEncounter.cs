using Codice.Mechanics;
using Codice.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Codice.Expedition
{
    public sealed class DestinyEncounter : MonoBehaviour
    {
        string[] dialogue = {
            "Has llegado más lejos que mis hermanos de guardia. El ángel no custodiaba un tesoro: custodiaba mi promesa rota.",
            "Ese Orbe guarda la memoria de quienes desafiaron el eclipse. Yo sostuve la campana cuando el cielo se cerró. Mi farol arde con lo que quedó de ellos.",
            "Quedan dos Orbes bajo tierras que el sol abandonó. Reunirlos abre el camino. Decidir qué hacer con ellos será tu condena, como fue la mía.",
            "Tras el umbral yacen las Cisternas del Osario. Algo enorme camina allí y hace temblar la piedra. Yo no puedo bajar: el juramento me ata a esta orilla. Vigilaré tu regreso."
        };
        GameObject orb, watcher, panel;
        TextMeshProUGUI text;
        Canvas canvas;
        int page = -1;
        bool complete;
        string orbName="MEMORIA";int world=2;
        public static void Create(ExpeditionFloor floor)
        {
            if(floor.GetComponentInChildren<DestinyEncounter>())return;
            var go=new GameObject("Orbe del Destino · El Vigía");
            go.transform.SetParent(floor.transform);
            go.transform.position=floor.Center(floor.Layout.Boss)+Vector3.right*4;
            go.AddComponent<DestinyEncounter>();
        }
        void Start()
        {
            int floor=GetComponentInParent<ExpeditionFloor>().FloorNumber;
            world=floor/3+1;
            if(floor==6){orbName="VOLUNTAD";dialogue=new[]{
                "La Lanza llamaba obediencia a este silencio. Has recuperado el Orbe de la Voluntad: el derecho a negarse.",
                "Nereo vendió los nombres de quienes se negaron. Yo los entregué a la campana. Ninguno de los dos conserva el suyo.",
                "La Memoria recuerda. La Voluntad elige. Falta el Duelo, porque nadie puede volver a vivir mientras sus muertos sigan trabajando.",
                "Busca al nigromante en los Campos del Último Suspiro. No resucita cuerpos: retiene despedidas. Rompe su coro."};}
            else if(floor==9){orbName="DUELO";dialogue=new[]{
                "El último muerto ha pronunciado su nombre. Este es el Orbe del Duelo. No devuelve a nadie: permite dejarlos partir.",
                "Memoria, Voluntad y Duelo. Los llamaron destino para convencernos de que no podíamos elegir. Fueron fabricados.",
                "Bajo estas tumbas está la Fundición de los Juramentos. Sus hornos devoran nombres; sus campanas devuelven soldados.",
                "Nereo conserva el registro que falta. Cruza los hornos, lee el archivo y alcanza la última campana. Esta vez, no obedezcas su voz."};}

            orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name="Orbe de "+orbName;
            orb.transform.SetParent(transform,false);
            orb.transform.localPosition=Vector3.up*1.2f;
            orb.transform.localScale=Vector3.one*.32f;
            Destroy(orb.GetComponent<Collider>());
            var material=Resources.Load<Material>("Presentation/LordsVfx/Core");
            if(material)orb.GetComponent<Renderer>().sharedMaterial=material;
            var light=orb.AddComponent<Light>();light.color=new Color(.4f,.75f,1);light.intensity=2;light.range=5;
            var prefab=Resources.Load<GameObject>("WorldProgression/Watcher");
            if(prefab){watcher=Instantiate(prefab,transform);watcher.transform.localPosition=new Vector3(1,0,0);Codice.Presentation.VigiaLook.Dress(watcher);watcher.SetActive(false);}
            canvas=new GameObject("Diálogo del Vigía").AddComponent<Canvas>();canvas.transform.SetParent(transform,false);UIWidgets.ConfigureCanvas(canvas);canvas.sortingOrder=90;
            panel=UIWidgets.Surface(canvas.transform,"Memoria",350,830,1220,180).gameObject;
            text=UIWidgets.Label(panel.transform,"Diálogo",35,20,1150,140,26);text.enableWordWrapping=true;
            panel.SetActive(false);
        }
        public int Stage=>page;
        internal void GuestPage(int index){
            if(!panel)return;
            page=index;orb.SetActive(index<0);if(watcher)watcher.SetActive(index>=0);
            panel.SetActive(index>=0);
            if(index>=0)text.text="EL VIGÍA\n"+dialogue[Mathf.Min(index,dialogue.Length-1)];
        }
        void Update()
        {
            var run=RunDirector.Instance;if(Codice.Multiplayer.LanSession.Client||!run||!run.Player||complete)return;
            bool near=Vector3.Distance(run.Player.transform.position,transform.position)<4;
            panel.SetActive(near||page>=0);
            if(page<0)text.text="ORBE DE "+orbName+"\n[E] Recuperar";
            if(!(near||page>=0)||!Codice.Mechanics.GameInput.Pressed(Codice.Mechanics.GameAction.Interact))return;
            Advance();
        }
        public void Advance()
        {
            var run=RunDirector.Instance;if(complete||!run||!run.Player)return;
            if(page<0){run.CollectDestinyOrb();Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Orb);orb.SetActive(false);if(watcher)watcher.SetActive(true);run.Player.SetNarrativeControlsBlocked(true);}
            page++;
            if(page<dialogue.Length){text.text="EL VIGÍA\n"+dialogue[page]+"\n<size=19>[E] Continuar</size>";return;}
            run.CompleteWatcherEncounter();run.Player.SetNarrativeControlsBlocked(false);
            complete=run.TravelNextFloor();
            if(!complete){page=dialogue.Length-1;text.text="[E] Cruzar al Mundo "+world;}
        }
        void OnDestroy(){if(RunDirector.Instance&&RunDirector.Instance.Player)RunDirector.Instance.Player.SetNarrativeControlsBlocked(false);}
    }
}
