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
            if(index==2)
            {
                var shrine=Resources.Load<GameObject>("CindersDungeon/EstusShrine");
                if(!shrine)throw new InvalidOperationException("Estus shrine prefab missing");
                Instantiate(shrine,center+new Vector3(-4,0,2),Quaternion.identity,root);
            }
            Debug.Log($"[Dungeon layout] room={index} shape={plan.shape} size={(plan.width*2+1)*4}x{(plan.depth*2+1)*4}m traps={plan.traps.Count} pillars={plan.pillars.Count} seed={Seed}");
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
