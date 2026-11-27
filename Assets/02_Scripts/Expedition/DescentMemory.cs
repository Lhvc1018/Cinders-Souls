using UnityEngine;
using Codice.Characters;
namespace Codice.Expedition
{
    // Only route knowledge survives a run. Gold, combat upgrades and contracts do not.
    public static class DescentMemory
    {
        public const string SaveKey="Cinders.DescentMemory.v1";
        public static int Keys=>PlayerPrefs.GetInt(SaveKey,0)&7;
        public static readonly string[] Names={"Llave del Sepulturero","Sello del Exiliado","Ascua del Recuerdo"};
        public static bool Owns(int key)=>key>=0&&key<3&&(Keys&(1<<key))!=0;
        /// Keys gathered by anyone in a coop run this session; the host broadcasts the mask and every guest learns them.
        public static int TeamLearned;
        /// A sealed chamber opens if the host or any connected guest already owns the key.
        public static bool TeamOwns(int key)=>Owns(key)||(Codice.Multiplayer.LanSession.Host&&Codice.Multiplayer.LanSession.Instance.PeerOwns(key));
        public static bool Learn(int key)
        {
            if(key<0||key>2||Owns(key))return false;
            PlayerPrefs.SetInt(SaveKey,Keys|(1<<key));PlayerPrefs.Save();return true;
        }
    }
    public sealed class RouteMemoryPickup:MonoBehaviour,IPlayerInteractable
    {
        public int key;
        public string Description=>DescentMemory.Names[key]+"\n[E] Conservar para próximos descensos\nAbre cámaras selladas; no se consume.";
        public void Interact(PlayerCharacter player)
        {
            if(!player||!player.State.IsAlive||player.ControlsBlocked||Vector3.Distance(player.transform.position,transform.position)>3.5f||Codice.Multiplayer.LanSession.Client)return;
            DescentMemory.Learn(key);Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Orb);DescentMemory.TeamLearned|=1<<key;
            Codice.UI.RunHUD.Instance?.Announce("RECUERDO PERMANENTE · "+DescentMemory.Names[key]);Destroy(gameObject);
        }
        public static void Create(int key,Vector3 position,Transform parent)
        {
            if(DescentMemory.Owns(key)&&!Codice.Multiplayer.LanSession.Host)return;
            var go=new GameObject(DescentMemory.Names[key]);go.transform.SetParent(parent);go.transform.position=position;
            go.AddComponent<RouteMemoryPickup>().key=key;
            var col=go.AddComponent<SphereCollider>();col.radius=.65f;col.isTrigger=true;
            var prefab=Resources.Load<GameObject>("CindersDungeon/RelicToken");
            if(prefab){var visual=Object.Instantiate(prefab,go.transform,false);Codice.Mechanics.PickupScale.Fit(visual,.6f);}
            var light=go.AddComponent<Light>();light.color=new Color(.45f,.8f,1);light.range=4;light.intensity=1.5f;
        }
    }
}
