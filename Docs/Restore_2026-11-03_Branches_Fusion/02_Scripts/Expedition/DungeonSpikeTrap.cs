using Codice.Characters;
using UnityEngine;
namespace Codice.Expedition
{
    public sealed class DungeonSpikeTrap:MonoBehaviour
    {
        public const float CycleSeconds=4.2f,Damage=18;
        public PlayerCharacter player;
        public float phaseOffset;
        public int room;
        public bool Active=>Phase>=2.8f&&Phase<3.55f;
        public bool Warning=>Phase>=2.05f&&Phase<2.8f;
        public float Phase=>Mathf.Repeat(Time.time+phaseOffset,CycleSeconds);
        Transform spikes;Renderer spikeRenderer,plateRenderer;MaterialPropertyBlock colors,plateColors;int lastHitCycle=int.MinValue;
        void Awake(){spikes=transform.Find("Spikes");spikeRenderer=spikes?spikes.GetComponent<Renderer>():null;plateRenderer=transform.Find("Plate")?.GetComponent<Renderer>();colors=new MaterialPropertyBlock();plateColors=new MaterialPropertyBlock();}
        void Update()
        {
            if(spikes){float height=Active?1:Warning?.12f:Phase>=3.55f?Mathf.Clamp01(1-(Phase-3.55f)/.25f):.02f;spikes.localScale=new Vector3(1,height,1);}
            if(spikeRenderer){colors.SetColor("_BaseColor",Active?new Color(.8f,.12f,.035f):Warning?new Color(1,.57f,.10f):new Color(.32f,.35f,.37f));spikeRenderer.SetPropertyBlock(colors);}
            if(plateRenderer){plateColors.SetColor("_BaseColor",Warning?new Color(.85f,.43f,.08f):Active?new Color(.5f,.065f,.02f):new Color(.20f,.18f,.13f));plateColors.SetColor("_EmissionColor",Warning?new Color(.65f,.25f,.025f):Active?new Color(.3f,.02f,0):Color.black);plateRenderer.SetPropertyBlock(plateColors);}
            TryDamage(player);
        }
        public bool TryDamage(PlayerCharacter target)
        {
            int cycle=Mathf.FloorToInt((Time.time+phaseOffset)/CycleSeconds);
            if(!Active||Time.timeScale<=0||!target||!target.State.IsAlive||cycle==lastHitCycle||Codice.UI.RunNavigation.Loading)return false;
            var p=target.transform.position-transform.position;
            if(Mathf.Abs(p.x)>1.35f||Mathf.Abs(p.z)>1.35f||p.y>.85f||p.y<-.5f)return false;
            float health=target.State.Health;
            target.ReceiveDamage(new DamageInfo{amount=Damage,source=gameObject,direction=Vector3.up,ignoreGuard=true});
            if(target.State.Health>=health)return false;
            lastHitCycle=cycle;Debug.Log("[Dungeon trap] Hit; room="+room+" damage="+(health-target.State.Health));return true;
        }
    }
}
