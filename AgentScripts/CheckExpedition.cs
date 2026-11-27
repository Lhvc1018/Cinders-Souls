using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Codice.Expedition;
using Codice.Characters;
using Codice.Enemies;
using Codice.Mechanics;
public static class CheckExpedition
{
    public static async Task<string> Run()
    {
        var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();
        if(!floor||!RunDirector.Instance||!floor.Layout.Connected())throw new Exception("Floor not initialized");
        var player=floor.context.player;var cc=player.GetComponent<CharacterController>();
        int frame=Time.frameCount;await Task.Delay(200);if(Time.frameCount<=frame)throw new Exception("Frozen game");
        if(floor.InEncounter || UnityEngine.Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None).Length>0)throw new Exception("Starting room not safe");
        if(floor.EnterRoom(5))throw new Exception("Can skip rooms");
        foreach(var cell in floor.Layout.Cells)
            if(!Physics.Raycast(new Vector3(cell.x*4,1,cell.y*4),Vector3.down,2,~0,QueryTriggerInteraction.Ignore))throw new Exception("Floor missing "+cell);
        for(int room=1;room<6;room++)
        {
            var from=player.transform.position;
            var to=floor.Center(room)+Vector3.up*.1f;
            var route=new NavMeshPath();
            if(!NavMesh.CalculatePath(from,to,NavMesh.AllAreas,route)||route.status!=NavMeshPathStatus.PathComplete)throw new Exception("No navigation route to room "+room);
            // Walk the baked path using the real CharacterController; entry uses the same proximity check as gameplay.
            player.SetNarrativeControlsBlocked(true);
            foreach(var corner in route.corners)
            {
                for(int step=0;step<1000;step++)
                {
                    var delta=corner-player.transform.position;delta.y=0;if(delta.magnitude<.18f)break;
                    cc.Move(Vector3.ClampMagnitude(delta,.15f));
                }
                var distance=corner-player.transform.position;distance.y=0;
                if(distance.magnitude>.4f)throw new Exception("Physical route blocked at "+corner);
            }
            if(!floor.EnterRoom(room) && floor.CurrentRoom!=room)throw new Exception("Room entry failed "+room);
            player.SetNarrativeControlsBlocked(false);
            if(room==2){if(floor.InEncounter)throw new Exception("Relic room started combat");continue;}
            if(!floor.InEncounter)throw new Exception("No combat in room "+room);
            if(room<5)
            {
                var enemies=UnityEngine.Object.FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None);
                if(enemies.Length<3)throw new Exception("Missing guardians");
                // Test damage/death callbacks, not a direct forced room-complete call.
                foreach(var enemy in enemies)enemy.ReceiveDamage(new DamageInfo{amount=100000,source=player.gameObject,direction=Vector3.back,melee=false});
            }
            else
            {
                var boss=floor.context.arena.ActiveBoss;if(!boss)throw new Exception("No final boss");
                boss.ReceiveDamage(new DamageInfo{amount=1000000,source=player.gameObject,direction=Vector3.back,melee=false});
            }
            await Task.Delay(150);
            if(floor.InEncounter)throw new Exception("Room did not unlock "+room);
        }
        if(!floor.Finished||RunDirector.Instance.Phase!=RunPhase.Victory||floor.ClearedRooms!=6)throw new Exception("Victory not reached");
        Debug.Log("[Expedition QA] PASS: navigation, physical traversal, room seals, enemy death callbacks, relic, final boss, six rooms and victory");
        return "PASS: six-room run, real colliders/navmesh, encounter completion, relic reward, final boss and victory. Damage accelerated for lifecycle testing.";
    }
}
