using System.IO;using System.Linq;using System.Threading.Tasks;using UnityEngine;
public static class AgentPrologueShot{public static async Task<string> Run(){
 UnityEngine.SceneManagement.SceneManager.LoadScene("Prologo");await Task.Delay(9000);
 var npc=GameObject.Find("El Vigía · NPC");if(!npc)return "NPC not found in scene "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
 var dress=npc.transform.Find("El Vigía · centinela de las velas");var cam=Camera.main;string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../MapReview/agent/"));
 var anim=dress?dress.GetComponentInChildren<Animator>():null;var pole=npc.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Farol de vigilia");var hand=npc.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="B-hand.R");
 string info="dress="+(bool)dress+" renderers="+npc.GetComponentsInChildren<Renderer>().Count(r=>r.enabled)+" state="+(anim?anim.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name:"-")+" poleToHand="+(pole&&hand?Vector3.Distance(pole.position,hand.position).ToString("F3"):"-")+" lights="+npc.GetComponentsInChildren<Light>().Length+" scale="+(dress?dress.lossyScale.y.ToString("F2"):"-");
 // free camera for the portrait; the player's view is restored by leaving the scene
 var go=new GameObject("shot cam");var c=go.AddComponent<Camera>();c.fieldOfView=38;c.depth=100;var p=npc.transform.position+npc.transform.forward*4.2f+npc.transform.right*1.4f+Vector3.up*1.5f;go.transform.position=p;go.transform.LookAt(npc.transform.position+Vector3.up*1.25f);
 await Task.Delay(600);ScreenCapture.CaptureScreenshot(dir+"vigia_prologo.png");await Task.Delay(800);Object.Destroy(go);return info;}}
