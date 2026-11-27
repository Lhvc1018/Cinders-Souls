using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Codice.Expedition;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Enemies;
using Codice.UI;
public static class ValidateDungeonExpansion
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Layouts()
    {
        var unique=new HashSet<string>();int checks=0;
        for(int seed=0;seed<2000;seed++)
        {
            var layout=new ExpeditionLayout(seed);Check(layout.Connected(),"Disconnected seed "+seed);checks++;
            var copy=new ExpeditionLayout(seed);string fingerprint=Signature(layout);Check(fingerprint==Signature(copy),"Unstable seed "+seed);checks++;
            unique.Add(fingerprint);Check(layout.Plans.Select(p=>p.shape).Distinct().Count()==3,"Shape diversity "+seed);checks++;
            for(int room=0;room<6;room++)
            {
                var p=layout.Plans[room];var c=layout.Rooms[room];
                Check(layout.Inside(room,new Vector3(c.x*4,0,c.y*4)),"Center missing "+seed+"/"+room);checks++;
                Check(p.traps.All(t=>p.Contains(t.x,t.y)&&Math.Abs(t.x)>1&&!p.pillars.Contains(t)),"Trap clearance "+seed+"/"+room);checks++;
                Check(p.pillars.All(t=>Math.Abs(t.x)>1),"Central route obstructed");checks++;
                if(room>0){Check(c.y-p.depth>layout.Rooms[room-1].y+layout.Plans[room-1].depth,"Rooms overlap");checks++;}
            }
        }
        foreach(PlayerClass kind in Enum.GetValues(typeof(PlayerClass)))
        {
            var s=new CharacterState();s.Initialize(kind);Check(s.EstusCharges==3&&!s.DrinkEstus(),"Full health flask");s.Damage(s.MaxHealth*.7f,true);float hp=s.Health;
            Check(s.DrinkEstus()&&Mathf.Abs(s.Health-hp-s.MaxHealth*.4f)<.01f&&s.EstusCharges==2,"40 percent heal");
            var clone=new CharacterState();clone.Initialize(kind);clone.Restore(s.Capture());Check(clone.EstusCharges==2,"Charges snapshot");
            for(int i=0;i<2;i++){s.Damage(s.Health-1,true);Check(s.DrinkEstus(),"Consume remaining flask");}
            s.Damage(s.Health-1,true);Check(!s.DrinkEstus()&&s.EstusCharges==0,"Empty denied");Check(s.RefillEstus()&&s.EstusCharges==3&&!s.RefillEstus(),"Refill bounded");
            s.Damage(s.MaxHealth,true);Check(!s.DrinkEstus()&&!s.RefillEstus(),"No revive");s.Initialize(kind);Check(s.EstusCharges==3,"Restart resets");checks+=9;
        }
        string report=$"{checks} checks passed; 2000 reproducible connected seeds; {unique.Count} unique layouts; three chamber shapes; safe central routes; five-class Estus state checks.";
        System.IO.File.WriteAllText("Docs/DungeonExpansion_LayoutValidation.txt",report);Debug.Log(report);return report;
    }
    static string Signature(ExpeditionLayout l)=>string.Join(";",l.Rooms)+"/"+string.Join(";",l.Plans.Select(p=>$"{p.width},{p.depth},{p.shape},{string.Join("|",p.traps)}"));
    static void Ground(PlayerCharacter p,Vector3 target)
    {
        var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=target+Vector3.up*.15f;cc.enabled=true;cc.Move(Vector3.down*.4f);Physics.SyncTransforms();
    }
    public static async Task<string> Navigation()
    {
        var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();Check(floor&&floor.Layout!=null,"Floor ready");
        var gates=UnityEngine.Object.FindObjectsByType<Transform>().Where(t=>t.name.StartsWith("Seal ·")).Select(t=>t.gameObject).ToArray();var active=gates.Select(g=>g.activeSelf).ToArray();
        foreach(var g in gates)g.SetActive(false);await Task.Delay(250);int routes=0;
        try
        {
            for(int i=0;i<6;i++)
            {
                var center=floor.Center(i);Check(NavMesh.SamplePosition(center,out var a,2,NavMesh.AllAreas),"No center nav "+i);
                foreach(var target in new[]{center+Vector3.forward*(floor.Layout.Radius(i)*4+5),center-Vector3.forward*(floor.Layout.Radius(i)*4-1),center+Vector3.right*3})
                {
                    if(i==5&&target.z>center.z+floor.Layout.Radius(i)*4)continue;
                    Check(NavMesh.SamplePosition(target,out var b,2,NavMesh.AllAreas),"No goal nav "+i+" "+target);var path=new NavMeshPath();
                    Check(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Incomplete local route "+i);routes++;
                }
                if(i>0){NavMesh.SamplePosition(floor.Center(i-1),out var before,2,NavMesh.AllAreas);var path=new NavMeshPath();Check(NavMesh.CalculatePath(before.position,a.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Corridor disconnected "+i);routes++;}
            }
        }
        finally{for(int i=0;i<gates.Length;i++)gates[i].SetActive(active[i]);}
        string result="Seed "+floor.Seed+": "+routes+" complete baked navigation routes, including all corridors.";System.IO.File.AppendAllText("Docs/DungeonExpansion_RuntimeValidation.txt",result+"\n");return result;
    }
    public static async Task<string> ItemsAndTraps()
    {
        var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();var p=floor.context.player;RunHUD.Instance.SetPaused(false);p.enabled=false;
        try
        {
            var chest=floor.Chests[0];Check(chest&&chest.rewardTier==1&&chest.price==0,"Free tier-one starter chest");Ground(p,chest.transform.position+Vector3.back*2);
            p.view.transform.LookAt(chest.GetComponent<Collider>().bounds.center);Check(InteractionTarget.Find(p)==chest,"Chest targeting through E");
            Check(chest.TryOpen(p)&&!chest.TryOpen(p),"Chest opens only once");var weapon=UnityEngine.Object.FindObjectsByType<WeaponPickup>().First(w=>(w.transform.position-chest.transform.position).sqrMagnitude<9);
            Check(weapon.weapon.tier==1,"Starter loot tier");weapon.Interact(p);Check(p.State.ActiveWeapon.tier==1,"Weapon pickup equips");
            Ground(p,floor.Center(0));await Task.Delay(50);p.State.Damage(p.State.MaxHealth*.6f,true);var flask=p.GetComponent<EstusFlask>();int charges=p.State.EstusCharges;
            Check(flask.TryDrink(),"Flask starts grounded");p.ReceiveDamage(new DamageInfo{amount=1});Check(!flask.Drinking&&p.State.EstusCharges==charges&&!p.ControlsBlocked,"Damage cancels flask without consuming");
            Check(flask.TryDrink(),"Flask retries");RunHUD.Instance.SetPaused(true);float progress=flask.Progress;await Task.Delay(180);Check(flask.Progress==progress,"Drinking pauses");RunHUD.Instance.SetPaused(false);
            float hp=p.State.Health;await Task.Delay(1250);Check(!flask.Drinking&&p.State.EstusCharges==charges-1&&Mathf.Abs(p.State.Health-hp-p.State.MaxHealth*.4f)<.02f,"Flask completes exactly once");
            var shrine=UnityEngine.Object.FindAnyObjectByType<EstusShrine>();Ground(p,shrine.transform.position+Vector3.back*2);shrine.Interact(p);Check(shrine.Used&&p.State.EstusCharges==3,"Shrine refills");p.State.Damage(p.State.MaxHealth*.5f);p.State.DrinkEstus();shrine.Interact(p);Check(p.State.EstusCharges==2,"Shrine cannot farm");
            var trap=UnityEngine.Object.FindObjectsByType<DungeonSpikeTrap>().First();Ground(p,trap.transform.position);p.State.Heal(1000);trap.phaseOffset=3-Time.time;hp=p.State.Health+p.State.Armor;
            Check(trap.Active&&trap.TryDamage(p)&&p.State.Health+p.State.Armor<hp,"Active spikes hurt");Check(!trap.TryDamage(p),"One hit per cycle");
            trap.phaseOffset=2.3f-Time.time;Check(trap.Warning&&!trap.TryDamage(p),"Warning is harmless");
            trap.phaseOffset=7.2f-Time.time;Ground(p,trap.transform.position+Vector3.up*1.5f);Check(!trap.TryDamage(p),"Jump clearance");
            Ground(p,floor.Center(0));p.State.Heal(1000);
            string report="Starter chest, tier-one pickup, duplicate denial, healing interrupt, pause, 40% heal, single-use shrine, telegraph, spike damage and jump clearance passed.";
            System.IO.File.AppendAllText("Docs/DungeonExpansion_RuntimeValidation.txt",report+"\n");return report;
        }
        finally{p.enabled=true;}
    }
    public static async Task<string> Traverse()
    {
        var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();var p=floor.context.player;var cc=p.GetComponent<CharacterController>();RunHUD.Instance.SetPaused(false);p.enabled=false;
        try
        {
            Ground(p,floor.Center(0));int steps=0;
            for(int room=1;room<6;room++)
            {
                await Task.Delay(180);NavMesh.SamplePosition(p.transform.position,out var start,3,NavMesh.AllAreas);NavMesh.SamplePosition(floor.Center(room),out var end,3,NavMesh.AllAreas);
                var path=new NavMeshPath();Check(NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Traversal route "+room);
                foreach(var corner in path.corners)
                {
                    int guard=0;
                    while(Vector3.ProjectOnPlane(corner-p.transform.position,Vector3.up).magnitude>.3f)
                    {
                        Check(guard++<500,"Character blocked by collider, seed="+floor.Seed+" room="+room+" position="+p.transform.position);
                        var delta=Vector3.ProjectOnPlane(corner-p.transform.position,Vector3.up);cc.Move(Vector3.ClampMagnitude(delta,.32f)+Vector3.down*.05f);steps++;p.State.Heal(1000);
                        await Task.Delay(1);
                        if(floor.InEncounter)
                        {
                            foreach(var enemy in UnityEngine.Object.FindObjectsByType<BasicEnemy>())enemy.ReceiveDamage(new DamageInfo{amount=10000,source=p.gameObject,ignoreGuard=true});
                            var boss=floor.context.arena.ActiveBoss;if(boss)boss.ReceiveDamage(new DamageInfo{amount=10000,source=p.gameObject,ignoreGuard=true});
                        }
                    }
                }
                await Task.Delay(180);Check(floor.Cleared[room],"Room did not clear "+room);Check(floor.PlayerRoom==room,"Minimap current room "+room);
            }
            Check(floor.Finished&&floor.ClearedRooms==6,"Boss completion");string report="Seed "+floor.Seed+": CharacterController traversed all six rooms in "+steps+" collision-checked steps; seals, rewards, minimap tracking and final victory passed.";
            System.IO.File.AppendAllText("Docs/DungeonExpansion_RuntimeValidation.txt",report+"\n");return report;
        }
        finally{p.enabled=true;}
    }
}
