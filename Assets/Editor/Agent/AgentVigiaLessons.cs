using System.Linq;using System.Reflection;using System.Threading.Tasks;using UnityEngine;
// Play-mode check of the Vigía tutorial: reads the speech built for the current class and progress.
public static class AgentVigiaLessons{public static async Task<string> Run(){
 UnityEngine.SceneManagement.SceneManager.LoadScene("Prologo");await Task.Delay(9000);
 var d=Object.FindAnyObjectByType<Codice.Prologue.PrologueDirector>();if(!d)return "no director";
 d.OpenDialogue();await Task.Delay(300);var f=typeof(Codice.Prologue.PrologueDirector).GetField("speech",BindingFlags.NonPublic|BindingFlags.Instance);var lines=(System.Collections.Generic.List<string>)f.GetValue(d);
 var cls=d.player.State.Class;return "class="+cls+" pages="+lines.Count+"\n"+string.Join("\n",lines.Select((l,i)=>(i+1)+") "+l));}}
