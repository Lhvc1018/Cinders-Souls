using System.Linq;using System.Text;using System.Threading.Tasks;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using Codice.Mechanics;
public static class AgentControls{
 public static string Pads()=>$"scheme={GameInput.Scheme} current={(Gamepad.current!=null?Gamepad.current.displayName+" ("+Gamepad.current.GetType().Name+")":"none")} all=[{string.Join(", ",Gamepad.all.Select(g=>g.displayName+"/"+g.layout))}] joysticks=[{string.Join(", ",Joystick.all.Select(j=>j.displayName))}] devices=[{string.Join(", ",InputSystem.devices.Select(d=>d.displayName))}]";
 static Gamepad sim; public static bool PsSim=false;
 static Gamepad Sim(){if(sim==null||!sim.added){sim=PsSim?(Gamepad)InputSystem.AddDevice<UnityEngine.InputSystem.DualShock.DualShock4GamepadHID>("AgentDS4"):InputSystem.AddDevice<Gamepad>("AgentPad");}sim.MakeCurrent();return sim;}
 public static void RemoveSim(){if(sim!=null&&sim.added)InputSystem.RemoveDevice(sim);sim=null;}
 static async Task Frames(int n){int f=Time.frameCount+n;while(Time.frameCount<f)await Task.Yield();}
 static async Task Hold(GamepadState s,int ms){var p=Sim();InputSystem.QueueStateEvent(p,s);float end=Time.unscaledTime+ms/1000f;await Frames(2);while(Time.unscaledTime<end)await Task.Yield();}
 static async Task Tap(GamepadButton b){await Hold(new GamepadState().WithButton(b),120);await Hold(new GamepadState(),120);}
 public static async Task<string> Gameplay(){var sb=new StringBuilder();int f0=Time.frameCount;float t0=Time.realtimeSinceStartup;await Frames(10);sb.AppendLine($"fps={(Time.frameCount-f0)/(Time.realtimeSinceStartup-t0):0.0}");InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;await Task.Delay(100);var r=RunDirector.Instance;var p=r.Player;p.State.LaboratoryImmortal=true;
  await Tap(GamepadButton.South);sb.AppendLine($"after A: scheme={GameInput.Scheme} cursor={Cursor.lockState} prompt='{GameInput.Prompt("[E] Abrir · [Q] Especial · Clic derecho: bloquear · [Tab] Arma · [1] uno")}'");
  var start=p.transform.position;float yaw=p.transform.eulerAngles.y;
  await Hold(new GamepadState{leftStick=new Vector2(0,1)},900);var moved=Vector3.Distance(start,p.transform.position);
  await Hold(new GamepadState{rightStick=new Vector2(1,0)},500);float turned=Mathf.DeltaAngle(yaw,p.transform.eulerAngles.y);await Hold(new GamepadState(),100);
  sb.AppendLine($"move stick: moved={moved:0.00}m turned={turned:0}° cursor={Cursor.lockState}");
  int slot=p.State.ActiveSlot;await Tap(GamepadButton.DpadUp);sb.AppendLine($"dpad up swap: {slot}->{p.State.ActiveSlot}");
  var combat=p.GetComponent<Codice.Characters.PlayerCombat>();int played=Codice.Presentation.GameAudio.Instance.PlayedCount;await Tap(GamepadButton.RightShoulder);await Task.Delay(300);sb.AppendLine($"RB attack: sounds {played}->{Codice.Presentation.GameAudio.Instance.PlayedCount} last={Codice.Presentation.GameAudio.Instance.LastCue}");
  await Hold(new GamepadState().WithButton(GamepadButton.LeftShoulder),300);sb.AppendLine($"LB block: blocking={combat.Blocking}");await Hold(new GamepadState(),100);
  float y0=p.transform.position.y;await Hold(new GamepadState().WithButton(GamepadButton.South),150);await Task.Delay(150);sb.AppendLine($"A jump: dy={p.transform.position.y-y0:0.00}");await Hold(new GamepadState(),300);
  await Tap(GamepadButton.Start);sb.AppendLine($"Start: paused={Codice.UI.RunHUD.Instance.Paused} selected={UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.name}");
  await Tap(GamepadButton.DpadRight);sb.AppendLine($"dpad right in pause: selected={UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.name}");
  await Tap(GamepadButton.East);sb.AppendLine($"B: paused={Codice.UI.RunHUD.Instance.Paused}");
  var mouse=Mouse.current;if(mouse!=null){InputSystem.QueueStateEvent(mouse,new MouseState{delta=new Vector2(40,0)});await Task.Delay(150);}sb.AppendLine($"mouse moved: scheme={GameInput.Scheme}");
  return sb.ToString();}
 public static string Labels(){var sb=new StringBuilder();foreach(GameAction a in System.Enum.GetValues(typeof(GameAction)))sb.Append($"{a}={GameInput.KeyLabel(a)}|{GameInput.PadLabel(a)}; ");return sb.ToString();}
 public static string Vigia()=>Codice.Prologue.VigiaLessons.ControlsLesson(true);
 public static string Shot(){var name=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();var dir=System.IO.Path.Combine(Application.dataPath,"../AgentScripts/shots/");System.IO.Directory.CreateDirectory(dir);ScreenCapture.CaptureScreenshot(dir+name+".png");return "shot "+name;}
 public static string Click(){var name=System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();var b=Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);if(!b)return "no button "+name;b.onClick.Invoke();return "clicked "+name;}
 public static string SaveA(){var sb=new StringBuilder();sb.AppendLine($"active={SaveSlots.Active} deepest={PlayerPrefs.GetInt("Cinders.Progress.Deepest")} runs={PlayerPrefs.GetInt("Cinders.Progress.Runs")} slot0='{SaveSlots.Describe(SaveSlots.Get(0)).Replace("\n"," | ")}'");
  SaveSlots.Select(2);sb.AppendLine($"slot3 selected: active={SaveSlots.Active} deepest={PlayerPrefs.GetInt("Cinders.Progress.Deepest")} runs={PlayerPrefs.GetInt("Cinders.Progress.Runs")}");
  var r=RunDirector.Instance;r.Player.State.AddGold(1234);SaveSlots.Checkpoint(r);var d=SaveSlots.Current;sb.AppendLine($"checkpoint: hasRun={d.hasRun} floor={d.runFloor} class={d.runClass} gold={r.Player.State.Gold} canContinue={SaveSlots.CanContinue} file={System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,"Saves/partida3.json"))}");
  PlayerPrefs.SetInt("Agent.ExpectGold",r.Player.State.Gold);Codice.UI.RunNavigation.ContinueSaved();return sb.ToString();}
 public static string SaveB(){var r=RunDirector.Instance;var sb=new StringBuilder();sb.AppendLine($"after continue: scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} floor={r?.FloorNumber} gold={r?.Player?.State.Gold} expected={PlayerPrefs.GetInt("Agent.ExpectGold")} class={r?.Player?.State.Class}");
  SaveSlots.Select(0);sb.AppendLine($"back to slot1: deepest={PlayerPrefs.GetInt("Cinders.Progress.Deepest")} runs={PlayerPrefs.GetInt("Cinders.Progress.Runs")} heard={PlayerPrefs.GetInt("Cinders.Vigia.Heard")}");
  SaveSlots.Delete(2);sb.AppendLine($"slot3 deleted: empty={SaveSlots.Get(2).Empty} active={SaveSlots.Active}");PlayerPrefs.DeleteKey("Agent.ExpectGold");
  sb.AppendLine("vigia(pad? "+GameInput.UsingGamepad+"): "+Codice.Prologue.VigiaLessons.ControlsLesson(true));return sb.ToString();}
 static string ShotDir=>System.IO.Path.Combine(Application.dataPath,"../AgentScripts/shots/");
 public static async Task<string> PadShots(){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  await Tap(GamepadButton.South);await Frames(10);ScreenCapture.CaptureScreenshot(ShotDir+"hud_pad.png");await Frames(5);
  await Tap(GamepadButton.Start);await Frames(5);var b=Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().FirstOrDefault(x=>x.name=="CONTROLES"&&x.gameObject.activeInHierarchy);b?.onClick.Invoke();await Frames(5);
  ScreenCapture.CaptureScreenshot(ShotDir+"pause_controls_pad.png");await Frames(5);await Tap(GamepadButton.East);await Frames(3);bool sheet=Resources.FindObjectsOfTypeAll<Codice.UI.ControlsSheet>().Any(x=>x.gameObject.activeInHierarchy);await Tap(GamepadButton.East);await Frames(3);
  return $"sheet closed by B: {!sheet} paused after 2nd B: {Codice.UI.RunHUD.Instance.Paused} selected={UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.GetComponentInChildren<TMPro.TMP_Text>()?.text}";}
 public static string RestoreInput(){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus;UnityEditor.EditorUtility.SetDirty(InputSystem.settings);UnityEditor.AssetDatabase.SaveAssets();return "input settings restored "+UnityEditor.AssetDatabase.GetAssetPath(InputSystem.settings);}
 public static async Task<string> MenuPad(){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;var sb=new StringBuilder();
  await Tap(GamepadButton.East);await Frames(5);sb.AppendLine($"scheme={GameInput.Scheme} ps={GameInput.PlayStation}");
  var b=Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>().First(x=>x.name=="INICIAR DESCENSO"&&x.gameObject.activeInHierarchy);b.onClick.Invoke();await Frames(40);
  var es=UnityEngine.EventSystems.EventSystem.current;string Sel()=>es.currentSelectedGameObject?es.currentSelectedGameObject.name:"-";
  sb.AppendLine("classes selected="+Sel());
  for(int k=0;k<3;k++){float t=Time.realtimeSinceStartup;string before=Sel();await Hold(new GamepadState().WithButton(GamepadButton.DpadDown),40);int f=0;while(Sel()==before&&f<200){await Frames(1);f++;}float dt=Time.realtimeSinceStartup-t;await Hold(new GamepadState(),60);sb.AppendLine($"down {before}->{Sel()} in {dt*1000:0}ms");}
  ScreenCapture.CaptureScreenshot(ShotDir+"classes_pad.png");await Frames(5);
  await Tap(GamepadButton.South);await Frames(3);var hint=Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>().First(x=>x.name=="Controls"&&x.gameObject.activeInHierarchy);sb.AppendLine("after 1st cross: "+hint.text);
  await Tap(GamepadButton.South);await Frames(10);var q=Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>().First(x=>x.name=="Question");sb.AppendLine($"after 2nd cross: dialog={q.gameObject.activeInHierarchy} '{q.text.Replace("\n"," ")}' selected={Sel()}");
  ScreenCapture.CaptureScreenshot(ShotDir+"confirm_pad.png");await Frames(5);
  await Tap(GamepadButton.East);await Frames(5);sb.AppendLine($"after circle: dialog={q.gameObject.activeInHierarchy} selected={Sel()} classesOpen={Resources.FindObjectsOfTypeAll<RectTransform>().First(x=>x.name=="Elegir clase").gameObject.activeSelf}");
  return sb.ToString();}
 public static string MageLoad(){int s=1000;Codice.Expedition.ExpeditionFloor.DebugSeed=s;Codice.UI.MainMenuUI.UnlockCheats("jam");Codice.UI.RunNavigation.StartExpeditionTest(Codice.Characters.PlayerClass.Mage,2);return "mage floor 2";}
 public static async Task<string> MageBeam(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new StringBuilder();
  int first=l.Links.First(e=>e.x==0).y;UnityEngine.AI.NavMesh.SamplePosition(f.Center(first),out var h,4,UnityEngine.AI.NavMesh.AllAreas);var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=h.position+Vector3.up*.1f;cc.enabled=true;
  await Task.Delay(2500);var enemies=Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Where(e=>e.IsAlive).ToList();if(enemies.Count==0)return "no enemies";

  var target=enemies.OrderBy(e=>Vector3.Distance(e.transform.position,p.transform.position)).First();
  cc.enabled=false;var to=target.transform.position-p.transform.position;to.y=0;p.transform.position=target.transform.position-to.normalized*7+Vector3.up*.1f;p.transform.rotation=Quaternion.LookRotation(to);cc.enabled=true;p.view.transform.localRotation=Quaternion.Euler(4,0,0);await Task.Delay(300);
  float hp0=target.Health;{var o=Codice.Presentation.HumanPreviewCamera.Origin(p);var fw=Codice.Presentation.HumanPreviewCamera.Aim(p);var cols=Physics.OverlapCapsule(o,o+fw*28,1.3f,~0,QueryTriggerInteraction.Ignore).Select(c=>c.name).Distinct();sb.AppendLine($"origin={o} aim={fw} toTarget={(target.transform.position-o).normalized} capsule=[{string.Join(",",cols)}]");}var w=p.State.ActiveWeapon;w.ultimateCharge=100;var m=p.GetComponent<PlayerMechanics>();bool ok=m.TryUltimate();
  sb.AppendLine($"weapon={w.kind} T{w.tier} ultimate={ok} mana={p.State.Mana:0} target={target.name} hp0={hp0:0}/{target.MaxHealth:0} dist={Vector3.Distance(p.transform.position,target.transform.position):0.0} 3p={p.GetComponent<Codice.Presentation.HumanPreviewCamera>()?.ThirdPerson}");
  await Task.Delay(3000);sb.AppendLine($"after beam: hp={target.Health:0} alive={target.IsAlive} active={m.UltimateActive}");
  return sb.ToString();}
 public static async Task<string> MageSpells(){var r=RunDirector.Instance;var f=r.Expedition;var l=f.Layout;var p=r.Player;p.State.LaboratoryImmortal=true;var sb=new StringBuilder();
  int first=l.Links.First(e=>e.x==0).y;UnityEngine.AI.NavMesh.SamplePosition(f.Center(first),out var h,4,UnityEngine.AI.NavMesh.AllAreas);var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=h.position+Vector3.up*.1f;cc.enabled=true;await Task.Delay(2500);
  var spells=p.GetComponent<Codice.Characters.StaffSpells>();var pose=p.GetComponent<Codice.Presentation.ActorPresentation>();sb.AppendLine($"conjure state={(pose&&pose.Body?pose.Body.HasState(0,Animator.StringToHash("Conjure")):false)}");
  foreach(var sp in new[]{Codice.Characters.StaffSpell.Ice,Codice.Characters.StaffSpell.Lightning,Codice.Characters.StaffSpell.Lightning}){
   var enemies=Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Where(e=>e.IsAlive).ToList();if(enemies.Count==0){sb.AppendLine("no enemies");break;}
   var t=enemies.OrderBy(e=>Vector3.Distance(e.transform.position,p.transform.position)).First();var to=t.transform.position-p.transform.position;to.y=0;
   cc.enabled=false;p.transform.position=t.transform.position-to.normalized*6+Vector3.up*.1f;p.transform.rotation=Quaternion.LookRotation(to);cc.enabled=true;var aimY=Quaternion.LookRotation((t.GetComponent<Collider>().bounds.center-Codice.Presentation.HumanPreviewCamera.Origin(p)).normalized).eulerAngles.x;p.view.transform.localRotation=Quaternion.Euler(aimY,0,0);await Task.Delay(200);
   p.State.SelectSpell((int)sp);int played=Codice.Presentation.GameAudio.Instance.Count(Codice.Presentation.SoundCue.SpellFrost)+Codice.Presentation.GameAudio.Instance.Count(Codice.Presentation.SoundCue.SpellLightning);
   bool cast=spells.TryCast(~0);await Task.Delay(1600);var st=t.GetComponent<Codice.Characters.ElementalStatus>();
   sb.AppendLine($"{sp}: cast={cast} target={t.name} hp={t.Health:0} frostUntil={(st?st.Until[2]-Time.time:-99):0.0} shockBuild={(st?st.Buildup[4]:0):0} aura={(t.GetComponent<Codice.Presentation.StatusAura>()!=null)} spellSounds+={Codice.Presentation.GameAudio.Instance.Count(Codice.Presentation.SoundCue.SpellFrost)+Codice.Presentation.GameAudio.Instance.Count(Codice.Presentation.SoundCue.SpellLightning)-played} magicCue={Codice.Presentation.GameAudio.Instance.Count(Codice.Presentation.SoundCue.Magic)}");}
  p.State.Damage(40);p.State.SelectSpell(3);bool heal=spells.TryCast(~0);await Task.Delay(1200);sb.AppendLine($"heal cast={heal} hp={p.State.Health:0}/{p.State.MaxHealth:0} heals={Codice.Presentation.GameAudio.Instance.Count(Codice.Presentation.SoundCue.Heal)}");
  return sb.ToString();}
 public static async Task<string> Statuses(){var p=RunDirector.Instance.Player;var sb=new StringBuilder();var a=Codice.Presentation.GameAudio.Instance;
  var enemies=Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Where(e=>e.IsAlive).Take(4).ToList();int k=0;
  foreach(var el in new[]{Codice.Characters.Element.Burn,Codice.Characters.Element.Frost,Codice.Characters.Element.Poison,Codice.Characters.Element.Shock}){if(k>=enemies.Count)break;var t=enemies[k++];
   var st=t.GetComponent<Codice.Characters.ElementalStatus>()??t.gameObject.AddComponent<Codice.Characters.ElementalStatus>();int before=a.PlayedCount;st.Add(el,100,p.gameObject);await Task.Delay(400);
   var aura=t.GetComponent<Codice.Presentation.StatusAura>();var label=aura?aura.GetComponentInChildren<TMPro.TextMeshPro>():null;sb.AppendLine($"{el} on {t.name}: aura={aura!=null} label='{label?.text}' sounds+={a.PlayedCount-before} last={a.LastCue} hp={t.Health:0}");
   cc(p,t);}
  ScreenCapture.CaptureScreenshot(ShotDir+"status.png");await Task.Delay(300);return sb.ToString();}
 static void cc(Codice.Characters.PlayerCharacter p,Component t){var c=p.GetComponent<CharacterController>();var to=t.transform.position-p.transform.position;to.y=0;c.enabled=false;p.transform.position=t.transform.position-to.normalized*5+Vector3.up*.1f;p.transform.rotation=Quaternion.LookRotation(to);c.enabled=true;}
 public static string Probe(){var go=GameObject.Find("Agent click probe");if(!go){go=new GameObject("Agent click probe");go.AddComponent<ClickProbe>();Object.DontDestroyOnLoad(go);}var pr=go.GetComponent<ClickProbe>();
  var p=RunDirector.Instance?RunDirector.Instance.Player:Object.FindFirstObjectByType<Codice.Characters.PlayerCharacter>();
  return $"scheme={GameInput.Scheme} lock={Cursor.lockState} visible={Cursor.visible} mouse={Mouse.current?.displayName} enabled={Mouse.current?.enabled} rawClicks={pr.raw} giAttack={pr.gi} blockHeld={pr.block} devices=[{string.Join(",",InputSystem.devices.Select(d=>d.displayName+(d.enabled?"":"(off)")))}] focus={Application.isFocused} player={(p?p.name:"-")} blocked={(p?p.ControlsBlocked:false)} timeScale={Time.timeScale} settings={InputSystem.settings.backgroundBehavior}/{InputSystem.settings.editorInputBehaviorInPlayMode}";}
 public static string RemoveVirtualPads(){var gone=InputSystem.devices.Where(d=>d is Gamepad&&d.description.empty).ToList();foreach(var d in gone)InputSystem.RemoveDevice(d);sim=null;return $"removed {gone.Count}; now=[{string.Join(",",InputSystem.devices.Select(d=>d.displayName))}]";}
}
public class ClickProbe:MonoBehaviour{public int raw,gi,block;void Update(){if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)raw++;if(GameInput.Pressed(GameAction.Attack))gi++;if(GameInput.Held(GameAction.Block))block++;}}
