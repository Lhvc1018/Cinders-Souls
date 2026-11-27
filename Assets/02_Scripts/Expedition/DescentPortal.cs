using UnityEngine;
using Codice.Characters;
using Codice.Mechanics;
namespace Codice.Expedition
{
    public sealed class DescentPortal : MonoBehaviour
    {
        float armedAt; bool crossed;
        public static void Create(Vector3 position,Transform parent,int destinationFloor=2)
        {
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Portal);
            var go=new GameObject("Portal · Descenso "+destinationFloor);go.transform.SetParent(parent);go.transform.position=position;
            var portal=go.AddComponent<DescentPortal>();portal.armedAt=Time.time+1.5f;
            var trigger=go.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up*1.4f;trigger.size=new Vector3(2,2.8f,1);
            var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
            Codice.Presentation.RitualSurface.Portal(go.transform);
            var ring=Codice.Bosses.BloodVisual.Ring(Vector3.zero,1.4f,new Color(.35f,.48f,.55f),.035f);
            ring.transform.SetParent(go.transform,false);var line=ring.GetComponent<LineRenderer>();Codice.Multiplayer.LanSession.ForgetLocalRing(line);line.useWorldSpace=false;
            for(int i=0;i<line.positionCount;i++){float a=i*Mathf.PI*2/line.positionCount;line.SetPosition(i,new Vector3(Mathf.Cos(a)*1.1f,1.4f+Mathf.Sin(a)*1.4f,0));}
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.45f,.7f,1);light.range=7;light.intensity=2;
            Debug.Log($"[Expedition] Floor {destinationFloor-1} cleared; crossing portal unlocks floor {destinationFloor}");
        }
        void OnTriggerEnter(Collider other){TryCross(other);}
        void OnTriggerStay(Collider other){TryCross(other);}
        void TryCross(Collider other)
        {
            if(crossed||Time.time<armedAt)return;
            var player=other.GetComponentInParent<PlayerCharacter>();var run=RunDirector.Instance;
            if(!player||!run||(player!=run.Player&&!Codice.Multiplayer.LanAvatar.IsRemote(player)))return;   /* coop: any pilgrim can take the team down */
            crossed=run.TravelNextFloor();
        }
    }
}
