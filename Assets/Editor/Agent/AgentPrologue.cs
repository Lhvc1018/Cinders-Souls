using System.Linq;using System.Reflection;using System.Text;using System.Threading.Tasks;using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using Codice.Prologue;using Codice.Characters;
public static class AgentPrologue{
 static string Arg=>System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath,"../AgentScripts/_bridge/arg.txt")).Trim();
 static string ShotDir=>System.IO.Path.Combine(Application.dataPath,"../AgentScripts/shots/");
 public static string Start(){var c=Arg.Length>0?(PlayerClass)System.Enum.Parse(typeof(PlayerClass),Arg):PlayerClass.Warrior;Codice.UI.RunNavigation.StartGame(c,1);return "start "+c;}
 static Gamepad sim;static Gamepad Sim(){if(sim==null||!sim.added)sim=InputSystem.AddDevice<Gamepad>("AgentPad");return sim;}
 static async Task Frames(int n){int f=Time.frameCount+n;while(Time.frameCount<f)await Task.Yield();}
 static async Task Hold(GamepadState s,int ms){InputSystem.QueueStateEvent(Sim(),s);float end=Time.unscaledTime+ms/1000f;await Frames(2);while(Time.unscaledTime<end)await Task.Yield();}
 static async Task Tap(GamepadButton b){await Hold(new GamepadState().WithButton(b),120);await Hold(new GamepadState(),120);}
 static object F(object o,string n)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);
 static void Move(PlayerCharacter p,Vector3 pos,float yaw){var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));cc.enabled=true;}
 public static async Task<string> Run(){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  var sb=new StringBuilder();var d=Object.FindFirstObjectByType<PrologueDirector>();if(!d)return "no prologue";var p=d.player;
  sb.AppendLine($"cells={d.Layout.Cells.Count} connected={d.Layout.IsConnected()}");
  int guard=0;while(!d.IntroFinished&&guard++<400)await Frames(5);
  guard=0;while(d.DialogueOpen&&guard++<200){d.AdvanceDialogue();await Frames(2);}
  await Frames(60);var direct=(Transform)F(d,"directGate");var trial=(Transform)F(d,"trialGate");
  sb.AppendLine($"spoken={d.HasSpoken} directOpen={!direct.GetComponent<BoxCollider>().enabled} trialOpen={!trial.GetComponent<BoxCollider>().enabled} objective='{((TMPro.TextMeshProUGUI)F(d,"objective")).text.Replace("\n"," | ")}'");
  Move(p,new Vector3(0,.1f,14),0);await Frames(20);ScreenCapture.CaptureScreenshot(ShotDir+"pro_north.png");await Frames(5);
  Move(p,new Vector3(0,.1f,16),-90);p.view.transform.localRotation=Quaternion.identity;await Frames(20);ScreenCapture.CaptureScreenshot(ShotDir+"pro_west.png");await Frames(5);
  // enter trial
  Move(p,new Vector3(-20,.1f,14),-90);await Frames(10);
  string Step()=>F(d,"step").ToString();
  sb.AppendLine("trial step="+Step());
  Move(p,new Vector3(-28,.1f,14),-90);await Frames(5);Move(p,new Vector3(-21,.1f,12),-90);await Frames(5);sb.AppendLine("after moving: "+Step());
  await Hold(new GamepadState{leftStick=new Vector2(0,1)}.WithButton(GamepadButton.LeftStick),150);await Hold(new GamepadState{leftStick=new Vector2(0,1)},1500);await Hold(new GamepadState(),100);sb.AppendLine("after sprint: "+Step());
  await Tap(GamepadButton.South);await Frames(10);sb.AppendLine("after jump: "+Step());
  await Frames(40);await Tap(GamepadButton.East);await Frames(10);sb.AppendLine("after dodge: "+Step());
  var dummy=Object.FindFirstObjectByType<TrainingDummy>();Move(p,dummy.transform.position+new Vector3(0,.1f,-2),0);await Frames(30);
  for(int i=0;i<6&&Step()=="Attack";i++){await Tap(GamepadButton.RightShoulder);await Frames(40);}sb.AppendLine($"after attacks: {Step()} hits={dummy.Hits}");
  ScreenCapture.CaptureScreenshot(ShotDir+"pro_trial.png");await Frames(5);
  await Hold(new GamepadState{rightTrigger=1},1100);await Hold(new GamepadState(),300);await Frames(20);sb.AppendLine($"after charged: {Step()} executed={p.GetComponent<ChargedAttack>().Executed} hits={dummy.Hits}");
  await Hold(new GamepadState().WithButton(GamepadButton.LeftShoulder),7500);await Hold(new GamepadState(),100);sb.AppendLine($"after defend: {Step()} blocked={dummy.Blocked} hp={p.State.Health:0}");
  await Hold(new GamepadState().WithButton(GamepadButton.LeftShoulder),120);await Hold(new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.RightShoulder),150);await Hold(new GamepadState(),150);await Frames(40);sb.AppendLine("after special (L1+R1): "+Step());
  await Frames(60);await Tap(GamepadButton.DpadLeft);await Frames(30);await Tap(GamepadButton.DpadRight);await Frames(20);sb.AppendLine($"after relics: {Step()} waits={p.State.RelicWait(0):0.0}/{p.State.RelicWait(1):0.0} mana={p.State.Mana:0}");
  var orb=Object.FindObjectsByType<Codice.Expedition.AshBoonOrb>(FindObjectsSortMode.None).FirstOrDefault();if(orb){Move(p,orb.transform.position+new Vector3(0,-1f,-1f),0);await Frames(10);await Tap(GamepadButton.West);await Frames(10);}sb.AppendLine($"after passive: {Step()} boons={p.State.BoonTotal} maxhp={p.State.MaxHealth:0}");
  var scroll=Object.FindObjectsByType<SpellPickup>(FindObjectsSortMode.None).FirstOrDefault();if(scroll){Move(p,scroll.transform.position+new Vector3(0,-.5f,-1.6f),0);p.view.transform.localRotation=Quaternion.Euler(15,0,0);await Frames(30);ScreenCapture.CaptureScreenshot(ShotDir+"pro_scroll.png");await Frames(5);await Tap(GamepadButton.West);await Frames(10);}
  await Hold(new GamepadState{leftTrigger=1},150);await Hold(new GamepadState(),150);await Frames(60);sb.AppendLine($"after scroll: {Step()} known={p.GetComponent<RunSpellbook>()?.Known.Count} last={Codice.Presentation.GameAudio.Instance.LastCue}");
  await Frames(60);await Tap(GamepadButton.North);await Frames(20);sb.AppendLine("after flask: "+Step());
  var exitGate=(Transform)F(d,"trialExitGate");sb.AppendLine($"exit gate open={!exitGate.GetComponent<BoxCollider>().enabled} finished={d.TrialFinished}");
  return sb.ToString();}
 public static async Task<string> Charge(){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  var d=Object.FindFirstObjectByType<PrologueDirector>();if(!d)return "no prologue";var p=d.player;int guard=0;while(!d.IntroFinished&&guard++<400)await Frames(5);guard=0;while(d.DialogueOpen&&guard++<200){d.AdvanceDialogue();await Frames(2);}await Frames(30);
  var dummy=Object.FindFirstObjectByType<TrainingDummy>();Move(p,dummy.transform.position+new Vector3(0,.1f,-2.2f),0);p.view.transform.localRotation=Quaternion.Euler(8,0,0);await Frames(30);
  int h0=dummy.Hits;await Hold(new GamepadState{rightTrigger=1},1100);await Hold(new GamepadState(),400);await Frames(30);
  return $"{p.State.Class}: executed={p.GetComponent<ChargedAttack>().Executed} dummyHits={dummy.Hits-h0} pos={p.transform.position}";}
 public static string RestorePrefs(){AgentControls.RemoveVirtualPads();string r=$"before runs={PlayerPrefs.GetInt("Cinders.Progress.Runs")} heard={PlayerPrefs.GetInt("Cinders.Vigia.Heard")} deepest={PlayerPrefs.GetInt("Cinders.Progress.Deepest")} device={PlayerPrefs.GetString("Cinders.Vigia.Device")}";
  PlayerPrefs.SetInt("Cinders.Progress.Runs",2);PlayerPrefs.SetInt("Cinders.Vigia.Heard",63);PlayerPrefs.SetInt("Cinders.Progress.Deepest",15);PlayerPrefs.DeleteKey("Cinders.Vigia.Device");PlayerPrefs.Save();Codice.Mechanics.SaveSlots.Commit();
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus;
  return r+" -> restored; slot="+Codice.Mechanics.SaveSlots.Active;}
}
