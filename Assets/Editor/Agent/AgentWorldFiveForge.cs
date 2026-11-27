using System.Collections.Generic;using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;using Codice.Enemies;using Codice.Bosses;
// Mundo 5 · «La Horda de los Colmillos» (pisos 13-15): campamento de guerra orco. Módulos originales (empalizada,
// tiendas de piel, tótems de colmillos, braseros, tambores, barricadas de estacas), residentes de la horda y los tres
// jefes: Ogro del Foso (Monster Orc retematizado, 5-1), Rey Trasgo (Goblin King, 5-2) y Berserker Orco (Ork Berserker, 5-3).
public static class AgentWorldFiveForge{
 const string D="Assets/Resources/WorldFive/";static int serial;
 static Material wood,hide,bone,iron,mud,coal,rope,paint;
 static Material Mat(string name,Color c,float metal,float smooth,Color? glow=null,float noise=0){string p=D+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}
  m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
  if(glow.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",glow.Value);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
  if(noise>0){string tp=D+name+"-surface.png";var tex=new Texture2D(256,256);var px=new Color[65536];for(int y=0;y<256;y++)for(int x=0;x<256;x++){float n=Mathf.PerlinNoise(x*.09f,y*.09f)*noise+Mathf.PerlinNoise(x*.025f+7,y*.025f)*noise*.8f+Mathf.Pow(Mathf.PerlinNoise(x*.6f,y*.04f),6)*.3f;float v=.62f+n-noise*.7f;px[y*256+x]=new Color(v,v,v,1);}tex.SetPixels(px);tex.Apply();File.WriteAllBytes(tp,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(tp);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tp));m.SetTextureScale("_BaseMap",Vector2.one*2);}
  EditorUtility.SetDirty(m);return m;}
 static Mesh Lathe(Vector2[] profile,int seg=14){var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();for(int j=0;j<profile.Length;j++)for(int i=0;i<=seg;i++){float a=2*Mathf.PI*i/seg;v.Add(new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x));uv.Add(new Vector2(i/(float)seg,j/(float)(profile.Length-1)));}
  for(int j=0;j<profile.Length-1;j++)for(int i=0;i<seg;i++){int a=j*(seg+1)+i,b=a+seg+1;tr.AddRange(new[]{a,b,a+1,a+1,b,b+1});}var m=new Mesh();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
 static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
 static Mesh Shared(string key,Vector2[] profile,int seg=14){if(meshes.TryGetValue(key,out var m)&&m)return m;string p=D+"Mesh-"+key+".asset";m=AssetDatabase.LoadAssetAtPath<Mesh>(p);var fresh=Lathe(profile,seg);if(m){m.Clear();m.SetVertices(fresh.vertices);m.SetUVs(0,fresh.uv);m.SetTriangles(fresh.triangles,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);}else{m=fresh;AssetDatabase.CreateAsset(m,p);}meshes[key]=m;return m;}
 static GameObject Part(GameObject root,string name,Mesh mesh,Vector3 p,Vector3 e,Vector3 s,Material mat){var g=new GameObject(name);g.transform.SetParent(root.transform,false);g.transform.localPosition=p;g.transform.localEulerAngles=e;g.transform.localScale=s;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;return g;}
 static void Box(GameObject root,string name,Vector3 p,Vector3 s,Material mat,Vector3 e=default){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root.transform,false);g.transform.localPosition=p;g.transform.localScale=s;g.transform.localEulerAngles=e;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());}
 static void Solid(GameObject g,Vector3 size,Vector3 center){var c=g.AddComponent<BoxCollider>();c.size=size;c.center=center;}
 static void Save(GameObject g){PrefabUtility.SaveAsPrefabAsset(g,D+g.name+".prefab");Object.DestroyImmediate(g);}
 static Mesh Stake=>Shared("Estaca",new[]{new Vector2(.17f,0),new Vector2(.19f,.15f),new Vector2(.16f,.85f),new Vector2(.15f,.88f),new Vector2(0,1)},9);
 static Mesh Log=>Shared("Tronco",new[]{new Vector2(0,0),new Vector2(.12f,0),new Vector2(.12f,1),new Vector2(0,1)},9);
 static Mesh Tusk=>Shared("Colmillo",new[]{new Vector2(.09f,0),new Vector2(.07f,.4f),new Vector2(.035f,.8f),new Vector2(0,1)},8);
 static Mesh Skull=>Shared("Craneo",new[]{new Vector2(0,0),new Vector2(.12f,.03f),new Vector2(.17f,.14f),new Vector2(.16f,.26f),new Vector2(.09f,.34f),new Vector2(0,.36f)},10);
 static Mesh Bowl=>Shared("Cuenco",new[]{new Vector2(0,0),new Vector2(.22f,.05f),new Vector2(.62f,.32f),new Vector2(.72f,.55f),new Vector2(.66f,.58f),new Vector2(.55f,.4f),new Vector2(0,.38f)},16);
 static Mesh Drum=>Shared("Tambor",new[]{new Vector2(0,0),new Vector2(.62f,0),new Vector2(.72f,.45f),new Vector2(.62f,.9f),new Vector2(.64f,.92f),new Vector2(0,.93f)},18);
 static Mesh Cone=>Shared("Tienda",new[]{new Vector2(1.9f,0),new Vector2(1.55f,.9f),new Vector2(.75f,2.3f),new Vector2(.12f,3.2f),new Vector2(0,3.3f)},9);
 static void Architecture(StringBuilder sb){
  wood=Mat("Madera ahumada",new Color(.24f,.17f,.12f),0,.15f,null,.25f);hide=Mat("Piel curtida",new Color(.42f,.3f,.2f),0,.25f,null,.3f);bone=Mat("Hueso de presa",new Color(.74f,.68f,.55f),0,.3f);
  iron=Mat("Hierro de la horda",new Color(.2f,.17f,.15f),.85f,.35f);mud=Mat("Barro pisoteado",new Color(.27f,.2f,.15f),0,.18f,null,.35f);coal=Mat("Brasa de guerra",new Color(1f,.4f,.1f),0,.4f,new Color(4f,1.2f,.2f));
  rope=Mat("Cuerda de tripa",new Color(.36f,.28f,.18f),0,.1f);paint=Mat("Pintura de sangre",new Color(.45f,.05f,.04f),0,.35f);
  // Ground: 4x4 trampled mud slab (top at y=.35 so the floor places it at -.35 like the other worlds)
  var g=new GameObject("Ground");Box(g,"Barro",new Vector3(0,.175f,0),new Vector3(4,.35f,4),mud);Solid(g,new Vector3(4,.35f,4),new Vector3(0,.175f,0));Save(g);
  // Palisade: sharpened stakes lashed with two bands
  g=new GameObject("Palisade");var rnd=new System.Random(5);for(int i=0;i<9;i++){float h=3.7f+(float)rnd.NextDouble()*.9f;Part(g,"Estaca",Stake,new Vector3(-1.8f+i*.45f,0,(float)rnd.NextDouble()*.12f-.06f),new Vector3(0,rnd.Next(360),((float)rnd.NextDouble()-.5f)*5),new Vector3(1,h,1),wood);}
  foreach(float y in new[]{1.1f,2.6f})Part(g,"Atadura",Log,new Vector3(-2.05f,y,-.2f),new Vector3(0,0,-90),new Vector3(.7f,4.1f,.7f),rope);Solid(g,new Vector3(4,4.3f,.6f),new Vector3(0,2.15f,0));Save(g);
  // Totem of tusks and skulls
  g=new GameObject("Totem");Part(g,"Poste",Log,Vector3.zero,Vector3.zero,new Vector3(1.6f,5.2f,1.6f),wood);for(int i=0;i<3;i++){Part(g,"Craneo",Skull,new Vector3(0,2.2f+i*1.05f,.16f),new Vector3(0,i*40,0),Vector3.one*(1.4f-i*.15f),bone);foreach(int s in new[]{-1,1})Part(g,"Colmillo",Tusk,new Vector3(s*.2f,2.4f+i*1.05f,.18f),new Vector3(0,0,s*-55),new Vector3(1,.75f,1),bone);}
  Part(g,"Cuerno",Tusk,new Vector3(-.15f,5.1f,0),new Vector3(0,0,25),new Vector3(1.4f,1.2f,1.4f),bone);Part(g,"Cuerno",Tusk,new Vector3(.15f,5.1f,0),new Vector3(0,0,-25),new Vector3(1.4f,1.2f,1.4f),bone);
  Box(g,"Estandarte",new Vector3(.55f,4.2f,0),new Vector3(.9f,1.3f,.03f),paint);Solid(g,new Vector3(.6f,5.3f,.6f),new Vector3(0,2.65f,0));Save(g);
  // Hide tent
  g=new GameObject("Tent");Part(g,"Lona",Cone,Vector3.zero,new Vector3(0,20,0),Vector3.one,hide);for(int i=0;i<3;i++)Part(g,"Vara",Log,new Vector3(0,3,0),new Vector3(0,i*120,18),new Vector3(.45f,1.2f,.45f),wood);Box(g,"Entrada",new Vector3(0,.8f,1.52f),new Vector3(.9f,1.5f,.05f),Mat("Sombra de tienda",new Color(.03f,.025f,.02f),0,0),new Vector3(-12,0,0));
  Box(g,"Marca",new Vector3(0,1.9f,1.18f),new Vector3(.5f,.5f,.02f),paint,new Vector3(-30,0,45));Solid(g,new Vector3(3.2f,2.6f,3.2f),new Vector3(0,1.3f,0));Save(g);
  // Brazier (lit at runtime with a point light)
  g=new GameObject("Brazier");for(int i=0;i<3;i++)Part(g,"Pata",Log,new Vector3(0,0,0),new Vector3(0,i*120,14),new Vector3(.35f,1.25f,.35f),iron);Part(g,"Cuenco",Bowl,new Vector3(0,1.05f,0),Vector3.zero,Vector3.one*.8f,iron);Part(g,"Brasas",Skull,new Vector3(0,1.25f,0),Vector3.zero,new Vector3(2.8f,.8f,2.8f),coal);Solid(g,new Vector3(1.1f,1.6f,1.1f),new Vector3(0,.8f,0));Save(g);
  // War drum
  g=new GameObject("Drum");Part(g,"Tambor",Drum,Vector3.zero,Vector3.zero,new Vector3(1,1.1f,1),hide);for(int i=0;i<6;i++)Part(g,"Tensor",Log,new Vector3(Mathf.Cos(i*1.047f)*.66f,.05f,Mathf.Sin(i*1.047f)*.66f),new Vector3(0,0,0),new Vector3(.12f,.9f,.12f),rope);
  foreach(int s in new[]{-1,1})Part(g,"Maza",Tusk,new Vector3(s*.3f,1.05f,0),new Vector3(0,0,s*70),new Vector3(.8f,.8f,.8f),bone);Solid(g,new Vector3(1.5f,1.1f,1.5f),new Vector3(0,.55f,0));Save(g);
  // Spike barricade (cheval de frise)
  g=new GameObject("Barricade");Part(g,"Eje",Log,new Vector3(-1.5f,.6f,0),new Vector3(0,0,-90),new Vector3(1.2f,3,1.2f),wood);for(int i=0;i<5;i++)foreach(int s in new[]{-1,1})Part(g,"Pica",Stake,new Vector3(-1.2f+i*.6f,.6f,0),new Vector3(s*50,0,0),new Vector3(.75f,1.3f,.75f),wood);Solid(g,new Vector3(3.1f,1.3f,1.6f),new Vector3(0,.65f,0));Save(g);
  // Bone pile
  g=new GameObject("Bones");rnd=new System.Random(9);for(int i=0;i<9;i++)Part(g,i%3==0?"Craneo":"Hueso",i%3==0?Skull:Tusk,new Vector3((float)rnd.NextDouble()*1.4f-.7f,0,(float)rnd.NextDouble()*1.4f-.7f),new Vector3(rnd.Next(-80,80),rnd.Next(360),rnd.Next(-80,80)),Vector3.one*(.9f+(float)rnd.NextDouble()*.6f),bone);Save(g);
  // Captive cage
  g=new GameObject("Cage");for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;Part(g,"Barrote",Log,new Vector3(Mathf.Cos(a)*.95f,0,Mathf.Sin(a)*.95f),Vector3.zero,new Vector3(.3f,2.4f,.3f),iron);}Part(g,"Techo",Bowl,new Vector3(0,2.6f,0),new Vector3(180,0,0),new Vector3(1.6f,.8f,1.6f),iron);Box(g,"Base",new Vector3(0,.08f,0),new Vector3(2.1f,.16f,2.1f),wood);Solid(g,new Vector3(2.1f,2.6f,2.1f),new Vector3(0,1.3f,0));Save(g);
  sb.AppendLine("architecture: 10 modules");}
 // ------------------------------------------------------------------ actors
 static void Tint(GameObject go,string id,Color tint,Color? eyeGlow){Directory.CreateDirectory(D+"Materials");int i=0;
  foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>{if(!m)return m;string p=D+"Materials/"+id+"-"+(i++)+".mat";var c=AssetDatabase.LoadAssetAtPath<Material>(p);if(!c){c=new Material(m);AssetDatabase.CreateAsset(c,p);}else c.CopyPropertiesFromMaterial(m);
   var col=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white;if(c.HasProperty("_BaseColor"))c.SetColor("_BaseColor",new Color(col.r*tint.r,col.g*tint.g,col.b*tint.b,col.a));
   bool eye=m.name.IndexOf("eye",System.StringComparison.OrdinalIgnoreCase)>=0;if(c.HasProperty("_EmissionColor")){if(eye&&eyeGlow.HasValue){c.EnableKeyword("_EMISSION");c.SetColor("_EmissionColor",eyeGlow.Value);}else{c.DisableKeyword("_EMISSION");c.SetColor("_EmissionColor",Color.black);}}EditorUtility.SetDirty(c);return c;}).ToArray();}
 static Transform Find(GameObject go,params string[] names){foreach(var n in names){var t=go.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name==n||x.name.EndsWith(":"+n));if(t)return t;}return go.GetComponentsInChildren<Renderer>(true).OrderByDescending(r=>r.bounds.size.sqrMagnitude).First().transform;}
 static void Attach(Transform bone,string name,Mesh mesh,Vector3 worldOffset,Vector3 euler,float size,Material mat){var g=new GameObject("Horda · "+name);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.transform.position=bone.position+worldOffset;g.transform.rotation=Quaternion.Euler(euler);g.transform.localScale=Vector3.one*size;g.transform.SetParent(bone,true);}
 static void Tusks(GameObject go,float size){var head=Find(go,"Head","head");foreach(int s in new[]{-1,1})Attach(head,"colmillo",Tusk,new Vector3(s*.07f*size,-.02f,.12f*size),new Vector3(-60,0,s*20),.35f*size,bone);}
 static void Pauldron(GameObject go,float size){foreach(var side in new[]{"LeftShoulder","RightShoulder"}){var t=Find(go,side);int s=side.StartsWith("Left")?-1:1;for(int i=0;i<3;i++)Attach(t,"pua",Tusk,new Vector3(s*.06f,.1f,(i-1)*.07f),new Vector3(0,0,s*-30),.32f*size,iron);}}
 static string Resident(string src,string id,string title,Color skin,float scale,float hp,float spd,float dmg,bool tusks,bool spikes,string back=null){
  var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(src));PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name=title;
  Tint(go,id,skin,new Color(2.2f,.6f,.1f));var model=go.GetComponentInChildren<Animator>().transform;model.localScale*=scale;var cap=go.GetComponent<CapsuleCollider>();if(cap){cap.height*=scale;cap.radius*=scale;cap.center*=scale;}
  var e=go.GetComponent<BasicEnemy>();e.variantName=title;e.healthMultiplier*=hp;e.speedMultiplier*=spd;e.damageMultiplier*=dmg;
  if(tusks)Tusks(go,scale);if(spikes)Pauldron(go,scale);
  if(back!=null){var kit=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/WorldThree/Kit/"+back+".prefab");if(kit){var spine=Find(go,"Spine2","Spine1");var k=(GameObject)Object.Instantiate(kit);foreach(var c in k.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);k.name="Horda · "+back;k.transform.position=spine.position+new Vector3(0,0,-.25f*scale);k.transform.rotation=Quaternion.Euler(0,180,12);k.transform.localScale*=.55f*scale;k.transform.SetParent(spine,true);}}
  Directory.CreateDirectory(D+"Enemies");PrefabUtility.SaveAsPrefabAsset(go,D+"Enemies/"+id+".prefab");Object.DestroyImmediate(go);return $"{id} «{title}» hp x{hp} spd x{spd} dmg x{dmg}";}
 static string Boss(string src,string id,string title,float health,int gold,Color skin,System.Action<GameObject> dress,BasicEnemy[] summons=null){
  var go=PrefabUtility.LoadPrefabContents(src);try{go.name=title;var b=go.GetComponent<BossCharacter>();b.baseHealth=health;b.goldReward=gold;if(summons!=null)b.summons=summons;Tint(go,id,skin,new Color(3f,.7f,.1f));dress?.Invoke(go);
   Directory.CreateDirectory(D+"Bosses");PrefabUtility.SaveAsPrefabAsset(go,D+"Bosses/"+title+".prefab");}finally{PrefabUtility.UnloadPrefabContents(go);}return $"{title} ({b0(src)}) hp {health} oro {gold}";}
 static string b0(string p)=>Path.GetFileNameWithoutExtension(p);
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";Directory.CreateDirectory(D);AssetDatabase.Refresh();var sb=new StringBuilder();Architecture(sb);
  const string C="Assets/Resources/ExpeditionEnemies/Cemetery/";
  sb.AppendLine(Resident(C+"Executioner.prefab","Bruto","Bruto colmilludo",new Color(.55f,.78f,.45f),1.18f,1.5f,.92f,1.25f,true,true,"sword_shield_broken"));
  sb.AppendLine(Resident(C+"StrawReaper.prefab","Saqueador","Trasgo saqueador",new Color(.6f,.85f,.4f),.78f,.7f,1.3f,.95f,true,false,"trunk_medium_B"));
  sb.AppendLine(Resident(C+"Watcher.prefab","Chaman","Chamán de hueso",new Color(.7f,.66f,.5f),1f,1.1f,1f,1.15f,true,true,"banner_patternC_brown"));
  sb.AppendLine(Resident(C+"Executioner.prefab","Portaestandarte","Portaestandarte de la horda",new Color(.85f,.4f,.32f),1.05f,1.25f,1.05f,1.1f,true,false,"banner_triple_red"));
  var wolf=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ExpeditionEnemies/MuckResidents/Lobo de hollin.prefab");
  if(wolf)sb.AppendLine(Resident("Assets/Resources/ExpeditionEnemies/MuckResidents/Lobo de hollin.prefab","Huargo","Huargo de guerra",new Color(.62f,.5f,.4f),1.2f,1.3f,1.1f,1.15f,false,false));
  AssetDatabase.SaveAssets();
  var minions=new[]{"Saqueador","Saqueador","Chaman"}.Select(n=>AssetDatabase.LoadAssetAtPath<BasicEnemy>(D+"Enemies/"+n+".prefab")).Where(x=>x).ToArray();
  sb.AppendLine(Boss("Assets/01_Prefabs/Bosses/MonsterOrc.prefab","OgroFoso","Ogro del Foso",3300,820,new Color(.5f,.62f,.42f),go=>{var b=go.GetComponent<BossCharacter>();var t=b.visual?b.visual:go.transform;var r=go.GetComponentsInChildren<Renderer>().OrderByDescending(x=>x.bounds.size.sqrMagnitude).First();var top=r.bounds.center+Vector3.up*r.bounds.extents.y*.75f;
   for(int i=0;i<5;i++)Attach(t,"pua de collar",Tusk,top-go.transform.position+new Vector3((i-2)*.35f,-.2f,-.3f),new Vector3(-20,0,(i-2)*-18),1.1f,iron);for(int i=0;i<3;i++)Attach(t,"craneo trofeo",Skull,top-go.transform.position+new Vector3((i-1)*.5f,-1.1f,.55f),new Vector3(0,180,0),1.6f,bone);}));
  sb.AppendLine(Boss("Assets/01_Prefabs/Bosses/GoblinKing.prefab","ReyTrasgo","Rey Trasgo",3000,780,new Color(.75f,.9f,.55f),go=>{var b=go.GetComponent<BossCharacter>();var t=b.visual?b.visual:go.transform;var r=go.GetComponentsInChildren<Renderer>().OrderByDescending(x=>x.bounds.size.sqrMagnitude).First();var top=r.bounds.center+Vector3.up*r.bounds.extents.y;
   for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Attach(t,"punta de corona",Tusk,top-go.transform.position+new Vector3(Mathf.Cos(a)*.42f,-.08f,Mathf.Sin(a)*.42f),new Vector3(0,0,0),.9f,iron);}Attach(t,"aro de corona",Bowl,top-go.transform.position+new Vector3(0,-.16f,0),Vector3.zero,.62f,iron);},minions));
  sb.AppendLine(Boss("Assets/01_Prefabs/Bosses/OrkBerserker.prefab","Berserker","Berserker Orco",4300,1100,new Color(.82f,.62f,.55f),null));
  AssetDatabase.SaveAssets();return sb.ToString();}
}
