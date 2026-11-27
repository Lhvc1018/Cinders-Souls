using System;
using System.Linq;
using System.Threading.Tasks;
using Codice.Mechanics;
using Codice.UI;
using Codice.Characters;
using UnityEngine;
public static class ReproduceAngel {
 public static async Task<string> Run(){
  MainMenuUI.UnlockCheats("jam");var old=RunDirector.Instance;RunNavigation.StartExpeditionTest(PlayerClass.Warrior,3);
  for(int i=0;i<400;i++){await Task.Delay(50);if(!RunNavigation.Loading&&RunDirector.Instance&&RunDirector.Instance!=old&&RunDirector.Instance.Expedition)break;}
  var run=RunDirector.Instance;var f=run.Expedition;var p=run.Player; p.State.LaboratoryImmortal=true;
  for(int i=0;i<f.Layout.Boss;i++)f.Cleared[i]=true;
  var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=f.Center(f.Layout.Boss);cc.enabled=true;
  await Task.Delay(2500);
  var b=f.context.arena.ActiveBoss;if(!b)throw new Exception("No active boss; room="+f.CurrentRoom);
  var a=b.GetComponent<Codice.Presentation.ActorPresentation>();
  return "boss="+b.name+" at="+b.transform.position+" visual="+b.visual.position+" animator="+a.Body.name+" state="+a.CurrentState+" enabledSkins="+b.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r=>r.enabled)+" bounds="+b.GetComponentsInChildren<SkinnedMeshRenderer>().First().bounds;
 }
}
