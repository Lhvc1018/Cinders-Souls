using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;using UnityEngine.AI;using Codice.Bosses;using Codice.Enemies;
// Golem back to a common-enemy size; Big Chunk becomes "El Señor de los Terremotos": colossal, basalt and ember, crowned with obsidian shards.
public static class AgentTerremotos {
 const string Dir="Assets/Resources/ExpeditionEnemies/";
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";var sb=new StringBuilder();
  // 1. Golem funerario: 2.5 m
  {string path=Dir+"MuckResidents/Golem funerario.prefab";var root=PrefabUtility.LoadPrefabContents(path);try{var c=root.GetComponent<CapsuleCollider>();float target=2.5f,k=target/c.height;if(Mathf.Abs(k-1)>.02f){var e=root.GetComponent<BasicEnemy>();var visual=e.animator.transform;while(visual.parent!=root.transform)visual=visual.parent;visual.localScale*=k;visual.localPosition*=k;c.height=target;c.radius=.7f;c.center=Vector3.up*target*.5f;var a=root.GetComponent<NavMeshAgent>();a.height=target;a.radius=.7f;e.locomotionMetersPerSecond*=k;e.miniboss=false;PrefabUtility.SaveAsPrefabAsset(root,path);sb.AppendLine($"Golem funerario x{k:F2} -> {target} m");}else sb.AppendLine("Golem already "+c.height);}finally{PrefabUtility.UnloadPrefabContents(root);}}
  // 2. Rename
  string oldPath=Dir+"Big Chunk.prefab",newPath=Dir+"El Señor de los Terremotos.prefab";
  if(File.Exists(oldPath)){var err=AssetDatabase.RenameAsset(oldPath,"El Señor de los Terremotos");sb.AppendLine("rename: "+(string.IsNullOrEmpty(err)?"ok":err));}
  // 3. Material
  string matPath=Dir+"Terremotos_Basalto.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
  var old=AssetDatabase.LoadAssetAtPath<Material>(Dir+"BigChunk_StoneGolem.mat");if(old&&old.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",old.GetTexture("_BaseMap"));
  var nrm=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Research/MuckNative/Texture2D/GolemTexture_normal.png");if(nrm){mat.SetTexture("_BumpMap",nrm);mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",1.6f);}
  mat.SetTexture("_EmissionMap",old&&old.HasProperty("_EmissionMap")?old.GetTexture("_EmissionMap"):null);bool glow=mat.GetTexture("_EmissionMap");
  mat.SetColor("_BaseColor",new Color(.30f,.26f,.26f));mat.SetFloat("_Metallic",.25f);mat.SetFloat("_Smoothness",.3f);if(glow){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(3.2f,.75f,.12f));}else{mat.DisableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);}mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(mat);
  string shardPath=Dir+"Terremotos_Obsidiana.mat";var shard=AssetDatabase.LoadAssetAtPath<Material>(shardPath);if(!shard){shard=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(shard,shardPath);}
  shard.SetColor("_BaseColor",new Color(.05f,.045f,.06f));shard.SetFloat("_Metallic",.6f);shard.SetFloat("_Smoothness",.78f);shard.EnableKeyword("_EMISSION");shard.SetColor("_EmissionColor",new Color(.9f,.18f,.03f));EditorUtility.SetDirty(shard);
  string meshPath=Dir+"Terremotos_Esquirla.asset";var spike=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(!spike){spike=Spike();AssetDatabase.CreateAsset(spike,meshPath);}
  // 4. Prefab: size, look, name
  {var root=PrefabUtility.LoadPrefabContents(newPath);try{root.name="El Señor de los Terremotos";var b=root.GetComponent<BossCharacter>();var c=root.GetComponent<CapsuleCollider>();float target=15,k=1;
    if(false){b.visual.localScale*=k;b.visual.localPosition*=k;c.height=target;c.radius*=k;c.center=Vector3.up*target*.5f;var a=root.GetComponent<NavMeshAgent>();a.height=target;a.radius=Mathf.Min(c.radius,3.6f);}
    foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
    foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Esquirla")).ToArray())Object.DestroyImmediate(t.gameObject);
    var anim=root.GetComponentInChildren<Animator>(true);var idle=anim.runtimeAnimatorController.animationClips.FirstOrDefault(x=>x.name.Contains("Idle"))??anim.runtimeAnimatorController.animationClips[0];idle.SampleAnimation(anim.gameObject,0); // place on the standing pose
    // The capsule said 15 m but the hunched body only reached ~5 m at the neck: scale the body until the neck stands at 9 m (about 11.5 m to the crown).
    {var neck=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");float k2=9f/(neck.position.y-root.transform.position.y);if(Mathf.Abs(k2-1)>.05f){b.visual.localScale*=k2;b.visual.localPosition*=k2;idle.SampleAnimation(anim.gameObject,0);}
     c.height=12;c.radius=4f;c.center=Vector3.up*6;var ag=root.GetComponent<NavMeshAgent>();ag.height=12;ag.radius=3.6f;}
    float unit=12;var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
    Transform Bone(string n)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
    void Add(Transform bone,Vector3 worldOffset,Vector3 euler,float length,float width){var go=new GameObject("Esquirla de obsidiana");go.transform.SetParent(bone,true);go.transform.position=bone.position+worldOffset*unit;go.transform.rotation=root.transform.rotation*Quaternion.Euler(euler);var ls=bone.lossyScale.x;go.transform.localScale=new Vector3(width*unit/ls,length*unit/ls,width*unit/ls);go.AddComponent<MeshFilter>().sharedMesh=spike;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=shard;}
    var head=Bone("Head");for(int i=0;i<5;i++){float ang=-60+i*30;Add(head,new Vector3(Mathf.Sin(ang*Mathf.Deg2Rad)*.05f,.035f,-.01f),new Vector3(-10,0,-ang*.8f),i==2?.15f:.10f,.035f);} // crown
    foreach(var side in new[]{("Shoulder.L",-1f),("Shoulder.R",1f)}){var sh=Bone(side.Item1);for(int i=0;i<3;i++)Add(sh,new Vector3(side.Item2*(.02f+i*.045f),.04f,-.02f),new Vector3(-15,0,-side.Item2*(10+i*20)),.17f-i*.035f,.05f);}
    var torso=Bone("Torso");for(int i=0;i<4;i++)Add(torso,new Vector3(0,.0f+i*.045f,-.09f),new Vector3(-60,0,0),.13f-i*.015f,.045f); // dorsal ridge
    PrefabUtility.SaveAsPrefabAsset(root,newPath);sb.AppendLine($"El Señor de los Terremotos: {target} m, radius {c.radius:F2}, head y={head.position.y:F1} clip={idle.name} glowMap={glow}");}finally{PrefabUtility.UnloadPrefabContents(root);}}
  AssetDatabase.SaveAssets();return sb.ToString();
 }
 static Mesh Spike(){var m=new Mesh{name="Esquirla"};var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();Vector3 tip=Vector3.up;var ring=new Vector3[5];for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;ring[i]=new Vector3(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f);}
  for(int i=0;i<5;i++){int n=v.Count;v.Add(ring[i]);v.Add(tip);v.Add(ring[(i+1)%5]);t.AddRange(new[]{n,n+1,n+2});n=v.Count;v.Add(ring[(i+1)%5]);v.Add(Vector3.down*.15f);v.Add(ring[i]);t.AddRange(new[]{n,n+1,n+2});}
  m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
}
