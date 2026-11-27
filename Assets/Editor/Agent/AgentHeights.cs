using System.Linq;using System.Text;using UnityEditor;using UnityEngine;using UnityEngine.AI;
// Measures baked mesh height of enemy prefabs and optionally normalizes the visual to a target height.
public static class AgentHeights {
 static readonly (string path,float target)[] Targets={
  ("Assets/Resources/ExpeditionEnemies/MuckResidents/Salteador de la cripta.prefab",1.55f),
  ("Assets/Resources/ExpeditionEnemies/MuckResidents/Hondero de la cripta.prefab",1.45f),
  ("Assets/Resources/ExpeditionEnemies/MuckResidents/Lobo de hollin.prefab",1.25f),
  ("Assets/Resources/ExpeditionEnemies/MuckResidents/Golem funerario.prefab",0),
  ("Assets/Resources/ExpeditionEnemies/CryptSkeleton.prefab",0),("Assets/Resources/ExpeditionEnemies/Hellhound.prefab",0),("Assets/Resources/Presentation/Classes/Warrior.prefab",0)};
 static float Height(GameObject go){float min=999,max=-999;var mesh=new Mesh();foreach(var r in go.GetComponentsInChildren<Renderer>()){Vector3[] v;if(r is SkinnedMeshRenderer s){s.BakeMesh(mesh,true);v=mesh.vertices.Select(x=>s.transform.TransformPoint(x)).ToArray();}else if(r.GetComponent<MeshFilter>()&&r.GetComponent<MeshFilter>().sharedMesh){var mb=r.GetComponent<MeshFilter>().sharedMesh.bounds;v=new[]{r.transform.TransformPoint(mb.min),r.transform.TransformPoint(mb.max)};}else continue;foreach(var p in v){min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}}Object.DestroyImmediate(mesh);return max-min;}
 public static string Measure(){var sb=new StringBuilder();foreach(var (path,_) in Targets){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));var a=go.GetComponentInChildren<Animator>();var c=go.GetComponent<CapsuleCollider>();var ag=go.GetComponent<NavMeshAgent>();sb.AppendLine($"{go.name}: h={Height(go):F2} animScale={(a?a.transform.localScale.x:0):F3} capsule={(c?c.height:0):F2} agentH={(ag?ag.height:0):F2}");Object.DestroyImmediate(go);}return sb.ToString();}
 public static string Fix(){var sb=new StringBuilder();foreach(var (path,target) in Targets){if(target<=0)continue;var root=PrefabUtility.LoadPrefabContents(path);try{var a=root.GetComponentInChildren<Animator>();var visual=a&&a.transform!=root.transform?a.transform:root.transform.GetChild(0);float h=Height(root);if(h<.1f)continue;float k=target/h;if(Mathf.Abs(k-1)<.04f){sb.AppendLine(root.name+" ok "+h.ToString("F2"));continue;}
   visual.localScale*=k;visual.localPosition*=k;var c=root.GetComponent<CapsuleCollider>();if(c){c.height=target;c.center=Vector3.up*target*.5f;c.radius=Mathf.Clamp(c.radius*k,.3f,target>2?.9f:.5f);}var ag=root.GetComponent<NavMeshAgent>();if(ag){ag.height=target;ag.radius=c?c.radius:ag.radius*k;}
   var e=root.GetComponent<Codice.Enemies.BasicEnemy>();if(e)e.locomotionMetersPerSecond*=k; // keep feet from sliding
   PrefabUtility.SaveAsPrefabAsset(root,path);sb.AppendLine($"{root.name}: {h:F2} -> {target:F2} m (x{k:F2})");}finally{PrefabUtility.UnloadPrefabContents(root);}}return sb.ToString();}
}
