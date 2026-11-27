using Codice.Characters;
using UnityEngine;
using UnityEngine.AI;
namespace Codice.Expedition
{
    public sealed partial class ExpeditionFloor
    {
        SecretWall secretWall;
        void BuildSecretWall(Transform root)
        {
            if(Layout.SecretRoom<0)return;
            int direction=System.Math.Sign(Layout.Rooms[Layout.SecretRoom].x);
            var go=FloorNumber>=13?Camp("Palisade",new Vector3(direction*10,0,0),90,root):FloorNumber>=10?Forge("Wall",new Vector3(direction*10,0,0),90,root):Place(wallPrefab,new Vector3(direction*10,0,0),90,root);
            go.name="Cracked secret wall";
            // Only breakable walls use the cracked mesh (cemetery boundaries now use the intact one), so the look is a reliable hint.
            if(FloorNumber<10){var cracked=Resources.Load<GameObject>("GothicArchitecture/SecretCrackedWall")?.GetComponentInChildren<MeshFilter>()?.sharedMesh;if(cracked)foreach(var mf in go.GetComponentsInChildren<MeshFilter>())mf.sharedMesh=cracked;}go.transform.localScale=new Vector3(1,5.4f/4.3f,1);
            secretWall=go.AddComponent<SecretWall>();secretWall.floor=this;
            secretWall.key=FloorNumber>1&&((Seed&1)==0)?(int)((uint)(Seed^FloorNumber*173)%3):-1;
            var obstacle=go.AddComponent<NavMeshObstacle>();obstacle.carving=true;obstacle.center=new Vector3(0,2,0);obstacle.size=new Vector3(4,5,.8f);
            // Hairline fractures reveal the physical entrance without a floating marker.
            var cracks=new GameObject("Stone fractures");cracks.transform.SetParent(go.transform,false);
            var line=Codice.Bosses.BloodVisual.Line(cracks,new Color(.24f,.17f,.10f),.025f);
            line.positionCount=5;
            for(int i=0;i<5;i++)line.SetPosition(i,new Vector3(direction*9.8f,.5f+i*.65f,(i%2==0?-.16f:.17f)));
        }
        public void RevealSecret()
        {
            if(SecretRevealed||Layout.SecretRoom<0)return;SecretRevealed=true;Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Seal);
            if(secretWall)secretWall.gameObject.SetActive(false);
            Debug.Log($"[Expedition] Secret wall broken seed={Seed} room={Layout.SecretRoom}");
        }
    }
    public sealed class SecretWall:MonoBehaviour,IDamageReceiver,IPlayerInteractable
    {
        public ExpeditionFloor floor;
        public int key=-1;
        public string Description=>key<0?"MURO AGRIETADO\nGolpéalo para descubrir una cámara":DescentMemory.Names[key]+"\n"+(DescentMemory.Owns(key)?"[E] Abrir cámara · La llave no se consume":"Necesitas este recuerdo permanente para entrar.");
        public void Interact(PlayerCharacter player)
        {
            if(key<0||!player||!player.State.IsAlive||player.ControlsBlocked||Codice.Multiplayer.LanSession.Client)return;
            var col=GetComponentInChildren<Collider>();if(!col||Vector3.Distance(col.ClosestPoint(player.transform.position),player.transform.position)>3.5f)return;
            if(DescentMemory.TeamOwns(key))floor.RevealSecret();else Codice.UI.RunHUD.Instance?.Announce("CÁMARA SELLADA · "+DescentMemory.Names[key]);
        }
        float nextHint;
        public float Health {get;private set;}=35;
        public void ReceiveDamage(DamageInfo hit)
        {
            if(Health<=0||hit.amount<=0||float.IsNaN(hit.amount)||float.IsInfinity(hit.amount)||!hit.source||!hit.source.GetComponentInParent<PlayerCharacter>())return;
            // Keyed walls do not crumble: tell the player why instead of silently absorbing the hit.
            if(key>=0){if(Time.time>nextHint){nextHint=Time.time+4;Codice.UI.RunHUD.Instance?.Announce(DescentMemory.TeamOwns(key)?"MURO SELLADO · Pulsa [E] para abrirlo con tu recuerdo":"MURO SELLADO · Necesitas el recuerdo «"+DescentMemory.Names[key]+"»");}return;}
            Health=Mathf.Max(0,Health-hit.amount);
            Codice.Mechanics.RelicPulse.Spawn(transform.position+Vector3.up, .55f,new Color(.4f,.33f,.22f));
            if(Health<=0)floor.RevealSecret();
        }
    }
}
