using System.Collections.Generic;
using System.Linq;
using Codice.Bosses;
using Codice.Characters;
using Codice.Enemies;
using Codice.Expedition;
using UnityEngine;
namespace Codice.Multiplayer
{
    // Coop support for the living descent: companions, refuges, permanent keys, and real visuals for every
    // world's residents/bosses on guests (instead of the old Hellhound/Inquisitor fallbacks).
    public sealed partial class LanSession
    {
        static readonly string[] EnemyFolders={"ExpeditionEnemies/","ExpeditionEnemies/Cemetery/","ExpeditionEnemies/Cemetery/Variants/","ExpeditionEnemies/MuckResidents/","WorldFour/Enemies/","WorldFive/Enemies/","EnemyVariants/"};
        static readonly string[] BossFolders={"ExpeditionEnemies/","WorldFour/Bosses/","WorldFive/Bosses/","Secrets/Bosses/"};
        static readonly Dictionary<string,GameObject> resolved=new Dictionary<string,GameObject>();
        static string PrefabName(Component c)=>c?c.gameObject.name.Replace("(Clone)","").Trim():null;
        static GameObject ResolveActor(string name,string[] folders)
        {
            if(string.IsNullOrEmpty(name))return null;
            if(resolved.TryGetValue(name,out var cached))return cached;
            GameObject found=null;foreach(var folder in folders){found=Resources.Load<GameObject>(folder+name);if(found)break;}
            resolved[name]=found;return found;
        }
        public bool PeerOwns(int key)=>peers.Any(p=>p.accepted&&(p.memories&(1<<key))!=0);
        int announcedMemories;
        void LearnTeamMemories(int mask)
        {
            for(int k=0;k<3;k++)if((mask&(1<<k))!=0&&(announcedMemories&(1<<k))==0)
            {
                announcedMemories|=1<<k;
                if(DescentMemory.Learn(k))Codice.UI.RunHUD.Instance?.Announce("RECUERDO PERMANENTE · "+DescentMemory.Names[k]);
            }
        }
        void AddLivingEntities(List<LanEntity> entities)
        {
            foreach(var c in FindObjectsByType<DescentCompanion>(FindObjectsSortMode.None))
                entities.Add(new LanEntity{id=Id(c),key="Companion:"+(c.Wisp?"Wisp":"Pilgrim"),position=c.transform.position,rotation=c.transform.rotation,animation=c.Moving?"Moving":"Idle"});
            foreach(var e in FindObjectsByType<DescentEncounter>(FindObjectsSortMode.None))
                entities.Add(new LanEntity{id=Id(e),key="Encounter:"+e.offer,position=e.transform.position,rotation=e.transform.rotation,available=e.Used,name=e.Description});
            foreach(var altar in FindObjectsByType<AshBoonAltar>())entities.Add(new LanEntity{id=Id(altar),key="BoonPlinth",position=altar.transform.position+Vector3.up*.35f,rotation=altar.transform.rotation});
            foreach(var o in FindObjectsByType<AshBoonOrb>(FindObjectsSortMode.None))
                entities.Add(new LanEntity{id=Id(o),key="Boon",position=o.transform.position,rotation=o.transform.rotation,kind=(int)o.boon,name=o.Description});
            var floor=context?context.Expedition:null;if(floor&&floor.Puzzle)entities.Add(new LanEntity{id=Id(floor.Puzzle),key="Puzzle",kind=floor.Puzzle.LitMask,available=floor.Puzzle.Solved,position=floor.Puzzle.transform.position});
            foreach(var m in FindObjectsByType<RouteMemoryPickup>(FindObjectsSortMode.None))
                entities.Add(new LanEntity{id=Id(m),key="Memory:"+m.key,position=m.transform.position,rotation=m.transform.rotation,kind=m.key,name=m.Description});
        }
        GameObject LivingVisual(LanEntity entity)
        {
            bool companion=entity.key.StartsWith("Companion:"),encounter=entity.key.StartsWith("Encounter:"),memory=entity.key.StartsWith("Memory:"),boon=entity.key=="Boon";
            if(!companion&&!encounter&&!memory&&!boon)return null;
            var go=new GameObject(entity.key);go.transform.position=entity.position;go.transform.rotation=entity.rotation;
            if(boon){AshBoonOrb.Visual(go.transform,BoonCatalog.Colors[Mathf.Clamp(entity.kind,0,BoonCatalog.Count-1)]);return go;}
            if(memory)
            {
                var prefab=Resources.Load<GameObject>("CindersDungeon/RelicToken");if(prefab)Codice.Mechanics.PickupScale.Fit(Instantiate(prefab,go.transform,false),.6f);
                var light=go.AddComponent<Light>();light.color=new Color(.45f,.8f,1);light.range=4;light.intensity=1.5f;return go;
            }
            bool pilgrim=entity.key.EndsWith("Pilgrim");
            if(pilgrim)
            {
                var body=DescentCompanion.PilgrimVisual(go.transform);
                foreach(var script in body.GetComponentsInChildren<MonoBehaviour>(true))if(!(script is CompanionMaterialOwner))DestroyImmediate(script);
            }
            else DescentCompanion.FlameVisual(go.transform,entity.key.EndsWith("Rest")?new Color(1,.55f,.15f):new Color(.3f,.85f,1));
            return go;
        }
    }
    /// Guest-side stand-in for a host-owned interactable: shows the host's prompt; E is forwarded by ClientControls.
    public sealed class LanInteractProxy:MonoBehaviour,IPlayerInteractable
    {
        public string text;
        public static void Attach(GameObject go,string description)
        {
            var proxy=go.GetComponent<LanInteractProxy>();
            if(!proxy){proxy=go.AddComponent<LanInteractProxy>();var col=go.AddComponent<SphereCollider>();col.isTrigger=true;col.radius=.9f;col.center=Vector3.up;}
            proxy.text=description;var ghost=go.GetComponent<LanGhost>();if(ghost&&ghost.snapshot!=null&&ghost.snapshot.key=="Boon"){var collider=go.GetComponent<SphereCollider>();collider.center=Vector3.zero;collider.radius=.45f;}
        }
        public void Interact(PlayerCharacter player){}
    }
}
