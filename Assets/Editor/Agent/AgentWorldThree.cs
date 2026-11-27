using System.IO;using System.Linq;using System.Text;using System.Threading.Tasks;using UnityEngine;using UnityEngine.AI;
using Codice.Characters;using Codice.Expedition;using Codice.Mechanics;using Codice.UI;using Codice.Presentation;
// World 3 verticality audit: layout statistics across seeds, then in Play Mode NavMesh reachability and captures.
public static class AgentWorldThree {
 static string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../MapReview/agent/world3/"));
 static string Arg=>File.ReadAllText(Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();
 public static string Audit(){var sb=new StringBuilder();
  foreach(int floor in new[]{7,8,9,10,11,12,13,14,15}){int vertical=0,sides=0,stairs=0,flatFallback=0,bad=0;float lo=0,hi=0;const int N=300;
   for(int s=0;s<N;s++){var seed=unchecked(1000+s+(floor-1)*104729);var l=new ExpeditionLayout(seed,floor);if(!l.Connected())bad++;
    if(l.Vertical)vertical++;else flatFallback++;sides+=l.SideRooms.Count;stairs+=l.Levels.Values.Count(v=>v.Stair);
    for(int r=0;r<l.Count;r++){lo=Mathf.Min(lo,l.Height(r));hi=Mathf.Max(hi,l.Height(r));}
    if(l.Levels.Count!=l.Cells.Count)bad++;}
   sb.AppendLine($"floor {floor}: vertical {vertical}/{N} flat {flatFallback} sideRooms avg {(float)sides/N:F2} stairCells avg {(float)stairs/N:F1} heights [{lo},{hi}] invalid {bad}");}
  return sb.ToString();}
 static void Move(PlayerCharacter p,Vector3 pos,Vector3 look){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(Vector3.ProjectOnPlane(look-pos,Vector3.up).normalized+Vector3.forward*.0001f));cc.enabled=true;p.view.transform.localRotation=Quaternion.identity;}
 public static string Nav(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var sb=new StringBuilder();
  sb.AppendLine($"floor={f.FloorNumber} seed={f.Seed} rooms={l.Count} side=[{string.Join(",",l.SideRooms)}] heights=[{string.Join(",",Enumerable.Range(0,l.Count).Select(i=>l.Height(i).ToString("0.0")))}] stairs={l.Levels.Values.Count(v=>v.Stair)}");
  NavMesh.SamplePosition(f.Center(0)+Vector3.back*3,out var start,3,NavMesh.AllAreas);int ok=0;
  for(int i=0;i<l.Count;i++){var path=new NavMeshPath();bool sampled=NavMesh.SamplePosition(f.Center(i),out var hit,4,NavMesh.AllAreas);bool found=sampled&&NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path);
   float len=0;for(int k=1;k<path.corners.Length;k++)len+=Vector3.Distance(path.corners[k-1],path.corners[k]);
   bool complete=found&&path.status==NavMeshPathStatus.PathComplete;if(complete)ok++;
   sb.AppendLine($" room {i} h={l.Height(i):0.0} {(l.SideRooms.Contains(i)?"SIDE ":"")}{(i==l.Boss?"BOSS ":"")}path={path.status} len={len:F0} maxY={(path.corners.Length>0?path.corners.Max(c=>c.y):0):F1}");}
  sb.AppendLine("reachable "+ok+"/"+l.Count);return sb.ToString();}
 public static async Task<string> Walk(){// physically walk the player along the NavMesh path to the furthest-height room
  var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;f.OpenAllSeals();
  int target=Enumerable.Range(1,l.Count-1).OrderByDescending(i=>Mathf.Abs(l.Height(i))).First();
  NavMesh.SamplePosition(p.transform.position,out var a,3,NavMesh.AllAreas);NavMesh.SamplePosition(f.Center(target),out var b,4,NavMesh.AllAreas);var path=new NavMeshPath();NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path);
  var cc=p.GetComponent<CharacterController>();float t0=Time.time;int corner=1;
  while(corner<path.corners.Length&&Time.time-t0<60){var to=path.corners[corner]-p.transform.position;to.y=0;if(to.magnitude<.6f){corner++;continue;}
   cc.Move(to.normalized*6f*Time.deltaTime+Vector3.down*4*Time.deltaTime);p.transform.rotation=Quaternion.LookRotation(to);await Task.Yield();}
  return $"walk to room {target} h={l.Height(target)} status={path.status} reached={corner>=path.corners.Length} final={p.transform.position} roomAt={l.RoomAt(p.transform.position)} t={Time.time-t0:F1}s";}
 static void Shot(Vector3 from,Vector3 to,string file){var go=new GameObject("agent shot cam");var cam=go.AddComponent<Camera>();cam.fieldOfView=55;cam.farClipPlane=400;cam.transform.position=from;cam.transform.LookAt(to);
  var urp=UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);urp.renderPostProcessing=true;
  var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();RenderTexture.active=null;
  File.WriteAllBytes(file,tex.EncodeToJPG(85));Object.DestroyImmediate(tex);cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
 public static string Tour(){
  string tag=Arg;Directory.CreateDirectory(Dir);var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;int n=0;
  foreach(var kv in l.Levels.Where(v=>v.Value.Stair).GroupBy(v=>Mathf.RoundToInt(v.Key.x/3f)*1000+Mathf.RoundToInt(v.Key.y/3f)).Select(g=>g.First()).Take(3)){
   var lv=kv.Value;var dir=new Vector3(lv.dir.x,0,lv.dir.y);var cell=new Vector3(kv.Key.x*4,lv.Low,kv.Key.y*4);bool up=lv.b>lv.a;var side=new Vector3(dir.z,0,dir.x);
   Shot(cell-dir*(up?9:-9)+side*3+Vector3.up*(lv.Low+3.2f-cell.y),cell+Vector3.up*(lv.High-lv.Low)*.5f,Dir+tag+"_stair"+(n++)+".jpg");}
  foreach(int room in l.SideRooms.Concat(new[]{Mathf.Min(2,l.Boss-1),Mathf.Min(4,l.Boss-1),Mathf.Min(6,l.Boss-1)}).Distinct()){var c=f.Center(room);float d=l.Radius(room)*4;
   Shot(c+new Vector3(-l.Width(room)*2.5f,9,-d-2),c+Vector3.forward*2,Dir+tag+"_room"+room+".jpg");n++;}
  { var c=f.Center(0);Shot(c+new Vector3(-30,45,-25),c+new Vector3(0,0,40),Dir+tag+"_overview.jpg");n++;}
  return "captured "+n+" "+tag;}
 public static string LoadAs(){var a=Arg.Split(' ');int floor=int.Parse(a[0]);var cls=(PlayerClass)System.Enum.Parse(typeof(PlayerClass),a[1]);ExpeditionFloor.DebugSeed=1000;MainMenuUI.UnlockCheats("jam");RunNavigation.StartExpeditionTest(cls,floor);return "loading floor "+floor+" as "+cls;}
 public static async Task<string> Fight(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new StringBuilder();
  int room=l.Links.First(e=>e.x==0&&e.y!=l.SecretRoom).y;var c=f.Center(room);
  NavMesh.SamplePosition(c+Vector3.back*(l.Radius(room)*4-4),out var hit,4,NavMesh.AllAreas);Move(p,hit.position+Vector3.up*.1f,c);await Task.Delay(2500);
  sb.AppendLine($"room={room} h={l.Height(room)} inEncounter={f.InEncounter} current={f.CurrentRoom} title={f.TitleFor(room)}");
  var enemies=Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None);
  foreach(var e in enemies){var an=e.GetComponentInChildren<Animator>();string st=an&&an.runtimeAnimatorController?string.Join("",new[]{"Idle","Patrol","Chase","Run","Melee","Shoot","Hit","Stun"}.Where(n=>an.GetCurrentAnimatorStateInfo(0).IsName(n))):"-";sb.AppendLine($" {e.name} variant='{e.variantName}' hp={e.Health:0}/{e.MaxHealth:0} onNav={e.GetComponent<NavMeshAgent>().isOnNavMesh} y={e.transform.position.y:0.0} state={st}");}
  Shot(p.transform.position+Vector3.up*5-p.transform.forward*6,p.transform.position+p.transform.forward*8,Dir+"fight_"+f.FloorNumber+".jpg");
  // arsenal checks
  var s=p.State;var w=s.ActiveWeapon;sb.AppendLine($"class={s.Class} weapon={Codice.UI.UIWidgets.WeaponName(w)} affinity={ClassArsenal.Effective(s,w)} speed x{s.AttackSpeedMultiplier:0.00} status='{p.GetComponent<ClassIdentity>().Status}{p.GetComponent<ClassIdentity>().Arsenal}'");
  var hammer=p.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance(WeaponKind.Hammer,2),p.transform.position+p.transform.forward);sb.AppendLine("looted Hammer became "+hammer.weapon.kind+" ("+ClassArsenal.Affinity(s.Class,hammer.weapon)+")");
  var forced=new WeaponInstance(WeaponKind.Hammer,2);var back=s.Equip(forced);sb.AppendLine("equip proscribed hammer: "+(back==forced?"rechazado":"EQUIPADO"));
  // kill everything to confirm death animation and room clear
  foreach(var e in enemies)if(e&&e.IsAlive)e.ReceiveDamage(new DamageInfo{amount=1e6f,source=p.gameObject,direction=Vector3.forward});
  await Task.Delay(2500);sb.AppendLine($"after kill: inEncounter={f.InEncounter} cleared={f.Cleared[room]}");return sb.ToString();}
 public static string Overview(){string tag=Arg;Directory.CreateDirectory(Dir);var f=RunDirector.Instance.Expedition;var l=f.Layout;
  foreach(int room in new[]{1,Mathf.Min(3,l.Boss-1),l.Boss}){var c=f.Center(room);float d=l.Radius(room)*4;Shot(c+new Vector3(-l.Width(room)*2.5f,10,-d-3),c+Vector3.forward*2,Dir+tag+"_room"+room+".jpg");}
  if(l.StatueRoom>0){var c=f.Center(l.StatueRoom);Shot(c+new Vector3(4,3.5f,-9),c+new Vector3(-3.6f,1,-4.5f),Dir+tag+"_smith.jpg");}
  return "smiths="+Object.FindObjectsByType<Codice.Expedition.Blacksmith>(FindObjectsSortMode.None).Length+" boss="+ExpeditionFloor.BossResource(f.Seed,f.FloorNumber)+" title="+f.TitleFor(1);}
 public static string Smith(){var p=RunDirector.Instance.Player;var s=Object.FindObjectsByType<Codice.Expedition.Blacksmith>(FindObjectsSortMode.None).FirstOrDefault();if(!s)return "no smith";
  p.State.AddGold(2000);var w=p.State.ActiveWeapon;int before=w.tier;float dmg=w.Damage;Move(p,s.transform.position+s.transform.forward*1.5f,s.transform.position);string d1=s.Describe(p);s.Interact(p);string d2=s.Describe(p);s.Interact(p);
  return $"weapon={Codice.UI.UIWidgets.WeaponName(w)} katana={w.katana} T{before}->T{w.tier} dmg {dmg:0}->{w.Damage:0} gold={p.State.Gold}\n{d1}\n{d2}";}
 public static string Prefs(){var a=Arg.Split(' ');string k=a[0];if(a.Length>1){int old=PlayerPrefs.GetInt(k,0);PlayerPrefs.SetInt(k,int.Parse(a[1]));PlayerPrefs.Save();return k+" "+old+"->"+a[1];}return k+"="+PlayerPrefs.GetInt(k,0);}
 public static async Task<string> BossShots(){var r=RunDirector.Instance;var f=r.Expedition;var p=r.Player;p.State.LaboratoryImmortal=true;Directory.CreateDirectory(Dir);var sb=new StringBuilder();
  for(int i=0;i<f.Layout.Boss;i++){f.Cleared[i]=true;f.Visited[i]=true;}var c=f.Center(f.Layout.Boss);Move(p,c+new Vector3(0,.2f,-f.Layout.Radius(f.Layout.Boss)*4+6),c);
  Codice.Bosses.BossCharacter b=null;for(int i=0;i<80&&!b;i++){await Task.Delay(100);b=f.context.arena.ActiveBoss;}if(!b)return "no boss";
  var anim=b.GetComponentInChildren<Animator>();int n=0;var seen=new System.Collections.Generic.HashSet<string>();float t0=Time.time;
  while(Time.time-t0<14&&n<4){await Task.Delay(120);var pres=b.GetComponent<Codice.Presentation.ActorPresentation>();if(pres)seen.Add(pres.CurrentState);
   if((b.State==Codice.Bosses.BossState.Windup||seen.Count>n+1)&&n<4){var bp=b.transform.position;Shot(bp+b.transform.forward*7+b.transform.right*3+Vector3.up*3.5f,bp+Vector3.up*1.8f,Dir+"goblin_play_"+(n++)+".jpg");await Task.Delay(500);}}
  sb.AppendLine("controller="+(anim&&anim.runtimeAnimatorController?anim.runtimeAnimatorController.name:"none")+" states="+string.Join(",",seen));
  b.ReceiveDamage(new DamageInfo{amount=1e7f,source=p.gameObject,direction=Vector3.forward,ignoreGuard=true});await Task.Delay(1200);
  var corpse=GameObject.Find("Animated remains");if(corpse)Shot(corpse.transform.position+new Vector3(5,4,5),corpse.transform.position+Vector3.up,Dir+"goblin_play_death.jpg");
  return sb.ToString()+" corpse="+(bool)corpse;}
 public static async Task<string> RageShots(){var r=RunDirector.Instance;var f=r.Expedition;var p=r.Player;p.State.LaboratoryImmortal=true;Directory.CreateDirectory(Dir);
  for(int i=0;i<f.Layout.Boss;i++){f.Cleared[i]=true;f.Visited[i]=true;}var c=f.Center(f.Layout.Boss);Move(p,c+new Vector3(0,.2f,-f.Layout.Radius(f.Layout.Boss)*4+6),c);
  Codice.Bosses.BossCharacter b=null;for(int i=0;i<80&&!b;i++){await Task.Delay(100);b=f.context.arena.ActiveBoss;}if(!b)return "no boss";
  var fi=typeof(Codice.Bosses.BossCharacter).GetField("rage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);string last="None";var seen=new System.Collections.Generic.List<string>();float hp0=p.State.Health;int hits=0;p.State.Changed+=()=>{};
  float t0=Time.time;int shots=0;bool phased=false;string trace="";
  float rt0=Time.realtimeSinceStartup;while(Time.time-t0<45&&Time.realtimeSinceStartup-rt0<(Arg=="near"?150:70)&&b){bool near=Arg=="near";await Task.Delay(100);
   if(!phased&&(near||Time.time-t0>15)){phased=true;b.ReceiveDamage(new DamageInfo{amount=near?b.Health-b.MaxHealth*.25f:Mathf.Max(1,b.Health-b.MaxHealth*.25f)*.5f,source=p.gameObject,direction=Vector3.forward,ignoreGuard=true});seen.Add("[fase "+b.Phase+"]");}
   // keep the player at mid range so leaps/charges trigger too
   if(b)trace=$"t={Time.time-t0:F1} rt={Time.realtimeSinceStartup-rt0:F0} ts={Time.timeScale} rage={fi.GetValue(b)} bossHp={b.Health:0} state={b.State} pos={b.transform.position} playerHp={p.State.Health:0} phase={r.Phase}";if(!b)break;var d=p.transform.position-b.transform.position;d.y=0;if(near&&d.magnitude>3.6f&&shots<6){var close=b.transform.position+b.transform.forward*2.8f;if(NavMesh.SamplePosition(close,out var hc,2,NavMesh.AllAreas))Move(p,hc.position+Vector3.up*.1f,b.transform.position);}
   else if(!near&&d.magnitude<3&&Time.time-t0>8){var to=c+(c-b.transform.position).normalized*Mathf.Min(9,f.Layout.Radius(f.Layout.Boss)*4-5);if(NavMesh.SamplePosition(to,out var h,3,NavMesh.AllAreas))Move(p,h.position+Vector3.up*.1f,b.transform.position);}
   if(!b)break;string now=fi.GetValue(b).ToString();if(now!=last){last=now;if(now!="None"){seen.Add(now);if(shots<6){await Task.Delay(Mathf.RoundToInt(250));var bp=b.transform.position;Shot(bp+(p.transform.position-bp).normalized*-2+Vector3.up*11+Vector3.right*6,bp+Vector3.up,Dir+"rage_"+(shots++)+"_"+now+".jpg");}}}}
  if(b){b.ApplyStun(2);await Task.Delay(400);}if(b){var bp=b.transform.position;Shot(bp+b.transform.forward*6+Vector3.up*4,bp+Vector3.up*2.5f,Dir+"rage_stun.jpg");}
  return trace+" | runPhase="+r.Phase+" | attacks="+string.Join(",",seen)+" phase="+(b?b.Phase:0)+" scale="+(b?b.visual.lossyScale.y:0).ToString("F2");}
}
