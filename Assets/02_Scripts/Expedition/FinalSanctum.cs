using System.Collections;
using UnityEngine;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
namespace Codice.Expedition
{
    public sealed class FinalSanctum:MonoBehaviour,IPlayerInteractable
    {
        Transform left,right;Material stone,gold;bool opening;
        public bool Opened {get;private set;}
        public string Description=>"UMBRAL DEL ALBA\n"+RunDirector.Instance.EndingRequirements+"\n"+(Opened?"Cruza y examina el corazón.":RunDirector.Instance.TrueEndingReady?"[E] Abrir · Los orbes no se consumen":"Faltan requisitos. El memorial permite concluir el descenso.");
        public static void Create(ExpeditionFloor floor)
        {
            if(floor.GetComponentInChildren<FinalSanctum>())return;
            var go=new GameObject("Umbral del Alba");go.transform.SetParent(floor.transform);go.transform.position=floor.Center(floor.Layout.Boss)+Vector3.forward*10;
            go.AddComponent<FinalSanctum>().Build();GameAudio.Play(SoundCue.Portal);
        }
        void Build()
        {
            stone=new Material(Shader.Find("Universal Render Pipeline/Lit"));stone.color=new Color(.12f,.14f,.16f);stone.SetFloat("_Smoothness",.25f);
            gold=new Material(Shader.Find("Universal Render Pipeline/Lit"));gold.color=new Color(.38f,.29f,.12f);gold.SetFloat("_Metallic",.7f);
            Box("Pilar izquierdo",new Vector3(-2,2,0),new Vector3(.6f,4,.8f),stone);
            Box("Pilar derecho",new Vector3(2,2,0),new Vector3(.6f,4,.8f),stone);
            Box("Dintel del alba",new Vector3(0,4,0),new Vector3(4.6f,.6f,.8f),gold);
            left=Box("Hoja izquierda",new Vector3(-.85f,1.75f,0),new Vector3(1.7f,3.5f,.3f),stone).transform;
            right=Box("Hoja derecha",new Vector3(.85f,1.75f,0),new Vector3(1.7f,3.5f,.3f),stone).transform;
            for(int i=0;i<3;i++)Box("Sello del orbe",new Vector3((i-1)*.55f,3.3f,-.22f),new Vector3(.2f,.32f,.12f),gold);
            var c=gameObject.AddComponent<BoxCollider>();c.isTrigger=true;c.center=new Vector3(0,1.4f,-.7f);c.size=new Vector3(3,2.8f,1);
            Box("Basa del corazón",new Vector3(0,.45f,4),new Vector3(1.5f,.9f,1.5f),stone);
            var heart=GameObject.CreatePrimitive(PrimitiveType.Sphere);heart.name="Corazón de los nombres";heart.transform.SetParent(transform,false);heart.transform.localPosition=new Vector3(0,1.35f,4);heart.transform.localScale=Vector3.one*.65f;heart.GetComponent<Renderer>().sharedMaterial=gold;
            var choice=heart.AddComponent<EndingChoice>();choice.gate=this;choice.release=true;
            var light=heart.AddComponent<Light>();light.color=new Color(.65f,.8f,1);light.range=8;light.intensity=2;
            var memorial=Box("Memorial del último vigía",new Vector3(-5,.8f,-3),new Vector3(1.2f,1.6f,.8f),stone);
            var exit=memorial.AddComponent<EndingChoice>();exit.gate=this;
            Box("Inscripción de retorno",new Vector3(-5,1.5f,-3.44f),new Vector3(.8f,.1f,.1f),gold);
        }
        GameObject Box(string title,Vector3 p,Vector3 size,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=title;go.transform.SetParent(transform,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
        public void Interact(PlayerCharacter player)
        {
            if(!player||!player.State.IsAlive||player.ControlsBlocked||Vector3.Distance(player.transform.position,transform.position)>4||opening||Opened||Codice.Multiplayer.LanSession.Client)return;
            if(!RunDirector.Instance.TrueEndingReady){GameAudio.Play(SoundCue.Error);Codice.UI.RunHUD.Instance?.Announce("El umbral exige los tres orbes y el sello de 4-3. Puedes concluir en el memorial.");return;}
            opening=true;GameAudio.PlayAt(SoundCue.Seal,transform.position);StartCoroutine(Open());
        }
        internal void GuestOpen(){if(!opening&&!Opened){opening=true;StartCoroutine(Open());}}
        IEnumerator Open(){float t=0;while(t<1){t+=Time.deltaTime/1.6f;left.localPosition=new Vector3(-.85f-1.6f*Mathf.SmoothStep(0,1,t),1.75f,0);right.localPosition=new Vector3(.85f+1.6f*Mathf.SmoothStep(0,1,t),1.75f,0);yield return null;}Opened=true;GetComponent<BoxCollider>().enabled=false;foreach(var c in left.GetComponents<Collider>())c.enabled=false;foreach(var c in right.GetComponents<Collider>())c.enabled=false;Codice.UI.RunHUD.Instance?.Announce("El último nombre es el tuyo. Cruza y decide si romperás la deuda.");}
        void OnDestroy(){if(stone)Destroy(stone);if(gold)Destroy(gold);}
    }
    public sealed class EndingChoice:MonoBehaviour,IPlayerInteractable
    {
        public FinalSanctum gate;public bool release;float confirmUntil;
        public string Description=>release?"CORAZÓN DE LOS NOMBRES\n[E] Liberar los nombres y romper el ciclo\nFinal: El Alba sin Deuda":Time.unscaledTime<confirmUntil?"[E] Confirmar el final El Último Vigía\nLa campana seguirá sonando.":"MEMORIAL DEL ÚLTIMO VIGÍA\n[E] Concluir sin romper el ciclo\nPuedes volver al umbral antes de confirmar.";
        public void Interact(PlayerCharacter p)
        {
            if(!p||Codice.Multiplayer.LanAvatar.IsRemote(p)||!p.State.IsAlive||p.ControlsBlocked||Vector3.Distance(p.transform.position,transform.position)>3.5f||Codice.Multiplayer.LanSession.Client)return;
            if(release&&(!gate||!gate.Opened))return;
            if(!release&&Time.unscaledTime>=confirmUntil){confirmUntil=Time.unscaledTime+8;GameAudio.Play(SoundCue.Click);return;}
            RunDirector.Instance.FinishDescent(release);
        }
    }
}
