using System.Linq;using System;using System.Reflection;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;using Codice.Characters;using Codice.Presentation;using Codice.Enemies;using Codice.UI;
public static class VerifyLockWalk{
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static async Task<string> Run(){
 if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="Descenso")UnityEngine.SceneManagement.SceneManager.LoadScene("Descenso");for(int i=0;i<100&&(!RunHUD.Instance||!RunHUD.Instance.Player);i++)await Task.Delay(100);await Task.Delay(600);
 var p=RunHUD.Instance.Player;RunHUD.Instance.SetPaused(false);p.SetNarrativeControlsBlocked(false);p.State.Initialize(PlayerClass.Warrior);await Task.Delay(150);
 var ap=p.GetComponent<ActorPresentation>();var combat=p.GetComponent<PlayerCombat>();var mode=p.GetComponent<HumanPreviewCamera>();if(!mode.ThirdPerson)mode.Toggle();p.enabled=false;ap.enabled=false;combat.enabled=false;
 var cc=p.GetComponent<CharacterController>();var saved=p.transform.position;var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1200,99.5f,1200);floor.transform.localScale=new Vector3(40,1,40);cc.enabled=false;p.transform.SetPositionAndRotation(new Vector3(1200,100.05f,1200),Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
 GameObject target=null,wall=null;
 try{
 for(int i=0;i<20;i++)p.SimulateMotor(Vector3.zero,true,1f/60);
 p.SprintRequested=false;p.SimulateMotor(Vector3.forward,true,1f/60);float walk=p.MotorVelocity.magnitude;p.SprintRequested=true;p.SimulateMotor(Vector3.forward,true,1f/60);float run=p.MotorVelocity.magnitude;Check(run>walk*1.8f,"Sprint not faster: "+walk+"/"+run);Check(ap.Body.HasState(0,Animator.StringToHash("Walk")),"Missing walking state");p.SprintRequested=false;
 float minFront=1,minTorso=100;int poses=0;
 foreach(var w in new[]{new WeaponInstance(WeaponKind.Sword),new WeaponInstance(WeaponKind.Sword){katana=true},new WeaponInstance(WeaponKind.Polearm)}){
 p.State.EquipIntoSlot(w,0);p.State.SelectWeapon(0);await Task.Delay(100);combat.GetType().GetMethod("UpdateWeapon",flags).Invoke(combat,new object[]{w});combat.GetType().GetField("nextAttack",flags).SetValue(combat,0f);Check(combat.TryAttack(),"Attack rejected "+w.kind);
 var duration=(float)combat.GetType().GetField("swingDuration",flags).GetValue(combat);
 for(int i=0;i<=10;i++){
 combat.GetType().GetField("swingElapsed",flags).SetValue(combat,duration*i/10f);ap.Body.speed=0;ap.Body.Play(ap.CurrentState,0,i/10f);ap.Body.Update(0);await Task.Delay(30);
 var support=ap.Body.GetComponent<HumanoidSupportHand>();support.GetType().GetMethod("LateUpdate",flags).Invoke(support,null);float front=Vector3.Dot(mode.HeldWeapon.up,ap.Body.transform.forward);minFront=Mathf.Min(minFront,front);Check(front>.25f,"Blade points into body "+w.kind+" frame="+i+" dot="+front);
 var local=ap.Body.transform.InverseTransformPoint(mode.HeldWeapon.position);Check(local.z>0,"Handle behind torso "+local);foreach(var mesh in mode.HeldWeapon.GetComponentsInChildren<MeshFilter>())foreach(var vertex in mesh.sharedMesh.isReadable?mesh.sharedMesh.vertices:Enumerable.Range(0,8).Select(n=>mesh.sharedMesh.bounds.center+Vector3.Scale(mesh.sharedMesh.bounds.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1))).ToArray()){var point=mesh.transform.TransformPoint(vertex);var start=ap.Body.GetBoneTransform(HumanBodyBones.Spine).position;var end=ap.Body.GetBoneTransform(HumanBodyBones.Neck).position;var axis=end-start;float h=Vector3.Dot(point-start,axis)/axis.sqrMagnitude;if(h>=0&&h<=1){float radius=Vector3.Distance(point,start+axis*h);minTorso=Mathf.Min(minTorso,radius);Check(radius>.14f,"Weapon intersects animated torso "+w.kind+" frame="+i+" radius="+radius);}}poses++;
 }
 combat.GetType().GetMethod("CancelSwing",flags).Invoke(combat,null);ap.Body.speed=1;
 }
 target=new GameObject("Lock-on test enemy");target.transform.position=p.transform.position+Vector3.forward*7;var enemy=target.AddComponent<BasicEnemy>();enemy.enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;enemy.GetComponent<ActorPresentation>().enabled=false;enemy.GetComponent<CapsuleCollider>().center=Vector3.up;enemy.GetComponent<CapsuleCollider>().height=2;
 p.view.transform.rotation=Quaternion.LookRotation(target.transform.position+Vector3.up-p.view.transform.position);Physics.SyncTransforms();var l=p.GetComponent<PlayerLockOn>();Check(l.Acquire()&&l.Target==enemy,"Cannot acquire visible enemy alive="+enemy.IsAlive+" visible="+Codice.Mechanics.CombatTargets.Visible(p.transform.position+Vector3.up,enemy)+" viewport="+p.view.WorldToViewportPoint(enemy.GetComponent<Collider>().bounds.center)+" selected="+(l.Target?l.Target.name:"none")+" blockers="+string.Join(",",Physics.RaycastAll(p.transform.position+Vector3.up,Vector3.forward,7,~0,QueryTriggerInteraction.Ignore).Select(h=>h.collider.name)));Check(Vector3.Dot(HumanPreviewCamera.Aim(p),(l.Point-HumanPreviewCamera.Origin(p)).normalized)>.999f,"Lock aim misses target");
 wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=p.transform.position+Vector3.forward*3+Vector3.up;wall.transform.localScale=new Vector3(5,4,.5f);Physics.SyncTransforms();l.Validate();float deadline=Time.time+.75f;for(int wait=0;wait<60&&Time.time<deadline;wait++)await Task.Delay(100);l.Validate();Check(!l.Target,"Lock persists through wall");Check(!l.Acquire(),"Acquire through wall");UnityEngine.Object.Destroy(wall);await Task.Delay(100);Physics.SyncTransforms();Check(l.Acquire(),"Reacquire failed");target.SetActive(false);l.Validate();Check(!l.Target,"Inactive enemy remains locked");
 string result="PASS walk="+walk+" run="+run+"; "+poses+" forward sword/katana/spear poses; min forward dot="+minFront+"; min weapon torso radius="+minTorso+"; lock acquisition, aim, occlusion, inactive release";Debug.Log(result);return result;
 }finally{p.GetComponent<PlayerLockOn>().Clear();p.SprintRequested=false;ap.Body.speed=1;p.enabled=true;ap.enabled=true;combat.enabled=true;cc.enabled=false;p.transform.position=saved;cc.enabled=true;UnityEngine.Object.Destroy(floor);if(target)UnityEngine.Object.Destroy(target);if(wall)UnityEngine.Object.Destroy(wall);}
 }
}









