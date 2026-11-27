using System;
using Codice.Mechanics;
using UnityEngine;
using UnityEngine.AI;
namespace Codice.Expedition
{
    public sealed partial class ExpeditionFloor
    {
        void DecorateRoom(int index,Transform root)
        {
            var plan=Layout.Plans[index];var center=Center(index);
            foreach(var cell in plan.pillars)
            {
                var pillar=Place(pillarPrefab,center+new Vector3(cell.x*4,0,cell.y*4),0,root);
                pillar.transform.localScale*=.65f;
            }
            // Narrow side alcoves remain outside the central route and spawn markers.
            for(int side=-1;side<=1;side+=2)
            {
                int x=plan.width*side,z=plan.shape==ChamberShape.Cross?0:plan.depth-1;
                if(plan.Contains(x,z))Place(propPrefab,center+new Vector3(x*4-side*.8f,0,z*4),side*35,root);
            }
            var trapPrefab=Resources.Load<GameObject>("CindersDungeon/SpikeTrap");
            if(!trapPrefab)throw new InvalidOperationException("Build Cinders dungeon content before playing");
            int n=0;
            foreach(var cell in plan.traps)
            {
                var trap=Instantiate(trapPrefab,center+new Vector3(cell.x*4,.03f,cell.y*4),Quaternion.identity,root).GetComponent<DungeonSpikeTrap>();
                trap.player=context.player;trap.room=index;trap.phaseOffset=(new System.Random(Seed^index*7919).Next(0,420)/100f)+n++*.7f;
            }
            if(index==Layout.MerchantRoom)
            {
                var shrine=Resources.Load<GameObject>("CindersDungeon/EstusShrine");
                if(!shrine)throw new InvalidOperationException("Estus shrine prefab missing");
                Instantiate(shrine,center+new Vector3(-4,0,2),Quaternion.identity,root);
                var merchant=Instantiate(Resources.Load<Codice.Characters.Merchant>("CindersDungeon/Merchant"),center+new Vector3(0,0,2),Quaternion.Euler(0,180,0),root);
                merchant.transform.localScale*=1.5f;merchant.OpenExpedition(Seed^index);context.dungeon.merchant=merchant;BuildStall(merchant,center,root);
            }
            if(index==Layout.StatueRoom)
            {
                var statue=Instantiate(Resources.Load<GameObject>("CindersDungeon/BlessingStatue"),center+new Vector3(0,0,1),Quaternion.identity,root).GetComponent<BlessingStatue>();
                statue.transform.localScale*=1.8f;
                statue.blessing=(Codice.Characters.StatueBlessing)new System.Random(Seed^317).Next(3);
            }
            Debug.Log($"[Dungeon layout] room={index} shape={plan.shape} size={(plan.width*2+1)*4}x{(plan.depth*2+1)*4}m traps={plan.traps.Count} pillars={plan.pillars.Count} seed={Seed}");
        }
        void BuildStall(Codice.Characters.Merchant merchant,Vector3 center,Transform root)
        {
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.16f,.065f,.045f));
            void Box(string name,Vector3 offset,Vector3 size){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=center+offset;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;}
            Box("Merchant counter",new Vector3(0,.4f,.2f),new Vector3(4.5f,.8f,.7f));
            Box("Burgundy canopy",new Vector3(0,2.65f,1.5f),new Vector3(5,.13f,3));
            foreach(float x in new[]{-2.3f,2.3f})Box("Stall post",new Vector3(x,1.3f,2.4f),new Vector3(.13f,2.6f,.13f));
            for(int i=0;i<4;i++){
                var display=new GameObject("Merchant offer "+i);display.transform.SetParent(root);display.transform.position=center+new Vector3(-1.65f+i*1.1f, .83f,.2f);
                string asset=i==1?"ManaShard":i==3?"RelicToken":"EstusBottle";var model=Instantiate(Resources.Load<GameObject>("CindersDungeon/"+asset),display.transform,false);PickupScale.Fit(model,.32f);
                var collider=display.AddComponent<BoxCollider>();collider.size=Vector3.one*.45f;var offer=display.AddComponent<MerchantDisplay>();offer.merchant=merchant;offer.slot=i;
            }
            Glow(center+new Vector3(0,2,0),new Color(1,.65f,.3f),3,9,root);
        }
        Vector3 SpawnPosition(Vector3 wanted)
        {
            if(NavMesh.SamplePosition(wanted,out var hit,2,NavMesh.AllAreas))return hit.position+Vector3.up*.08f;
            throw new InvalidOperationException("No navigable enemy spawn; seed="+Seed+" position="+wanted);
        }
        void SpawnChest(int room,bool relic,bool active,int tier)
        {
            if(Chests[room])return;
            var prefab=Resources.Load<GameObject>("CindersDungeon/RewardChest");
            if(!prefab)throw new InvalidOperationException("Reward chest prefab missing");
            var chest=Instantiate(prefab,Center(room)+new Vector3(3,.04f,-3),Quaternion.Euler(0,180,0),transform).GetComponent<TreasureChest>();
            chest.price=0;chest.expeditionReward=true;chest.expeditionRoom=room;chest.rewardSeed=Seed^(room*3571);chest.rewardTier=tier;chest.relicReward=relic;chest.activeReward=active;Chests[room]=chest;
        }
    }
}
