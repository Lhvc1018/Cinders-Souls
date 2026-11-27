using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
// Elaborate class silhouettes built from rigid pieces parented to bones (T-pose authoring, +Z forward, .L = -X).
public static class AgentCouture {
 const string Dir="Assets/Resources/Presentation/Classes/Couture/";const string Tag="Couture · ";
 static Transform root;static Mesh spike;
 static Material Mat(string name,Color c,float metal,float smooth,Color? glow=null){var path=Dir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
  m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);if(glow.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",glow.Value);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}EditorUtility.SetDirty(m);return m;}
 static Transform Bone(string n)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
 static Mesh Prim(PrimitiveType t){var g=GameObject.CreatePrimitive(t);var m=g.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(g);return m;}
 static Mesh cube,cyl,sph;
 // offset: metres from the bone in character space; euler: character space; scale: metres.
 static GameObject P(string bone,string name,Mesh mesh,Vector3 offset,Vector3 euler,Vector3 scale,Material mat){var b=Bone(bone);var go=new GameObject(Tag+name);go.transform.SetParent(b,true);go.transform.position=b.position+offset;go.transform.rotation=Quaternion.Euler(euler);go.transform.localScale=scale/Mathf.Max(.001f,b.lossyScale.x);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;return go;}
 static void Begin(string prefab){root=PrefabUtility.LoadPrefabContents("Assets/Resources/Presentation/Classes/"+prefab+".prefab").transform;foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t&&t.name.StartsWith(Tag)).ToArray())if(t)Object.DestroyImmediate(t.gameObject);}
 static int End(string prefab){int n=root.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith(Tag));PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Resources/Presentation/Classes/"+prefab+".prefab");PrefabUtility.UnloadPrefabContents(root.gameObject);return n;}
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
  cube=Prim(PrimitiveType.Cube);cyl=Prim(PrimitiveType.Cylinder);sph=Prim(PrimitiveType.Sphere);spike=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/ExpeditionEnemies/Terremotos_Esquirla.asset");
  var crimson=Mat("Seda carmesi",new Color(.42f,.05f,.06f),0,.25f);var crimsonDark=Mat("Seda granate",new Color(.22f,.03f,.05f),0,.2f);
  var brass=Mat("Laton viejo",new Color(.62f,.45f,.2f),.85f,.55f);var lacquer=Mat("Laca negra",new Color(.035f,.035f,.045f),.3f,.75f);
  var steel=Mat("Acero pavonado",new Color(.3f,.32f,.36f),.9f,.6f);var leather=Mat("Cuero viejo",new Color(.16f,.1f,.07f),0,.3f);
  var paper=Mat("Pergamino",new Color(.78f,.72f,.58f),0,.15f);var eye=Mat("Mirada de brasa",new Color(.3f,.02f,.01f),0,.4f,new Color(4f,.35f,.1f));
  var crystal=Mat("Cristal arcano",new Color(.15f,.35f,.6f),.2f,.9f,new Color(.5f,1.9f,3.4f));var rune=Mat("Runa viva",new Color(.1f,.2f,.3f),0,.5f,new Color(.3f,1.1f,2.0f));
  var indigo=Mat("Terciopelo indigo",new Color(.07f,.07f,.16f),0,.3f);var vial=Mat("Vial violeta",new Color(.3f,.1f,.4f),0,.9f,new Color(.5f,.12f,.75f));var seal=Mat("Lacre",new Color(.5f,.05f,.04f),0,.5f);
  var sb=new StringBuilder();
  // ---------------- SHINOBI: lobo de ceniza ----------------
  goto MageOnly; // Shinobi is now forged from scratch by AgentShinobiForge
  Begin("Shinobi");
  // scarf: wrap + two tattered tails down the back
  P("B-neck","Bufanda vuelta",cyl,new Vector3(0,.0f,0),Vector3.zero,new Vector3(.27f,.04f,.25f),crimson);
  P("B-neck","Bufanda nudo",sph,new Vector3(-.09f,-.02f,-.1f),Vector3.zero,new Vector3(.08f,.07f,.07f),crimson);
  for(int tail=0;tail<2;tail++){var go=new GameObject(Tag+"Bufanda cinta "+tail);var neck=Bone("B-neck");go.transform.SetParent(neck,false);go.transform.position=neck.position;
   var line=go.AddComponent<LineRenderer>();line.sharedMaterial=tail==0?crimson:crimsonDark;line.positionCount=0;
   var ribbon=go.AddComponent<Codice.Presentation.ScarfRibbon>();ribbon.body=root;ribbon.anchorOffset=new Vector3(-.07f+tail*.05f,-.01f,-.13f);ribbon.length=tail==0?.85f:.62f;ribbon.width=tail==0?.09f:.075f;ribbon.sway=tail==0?1:.7f;}
  // helmet crest and ember gaze
  foreach(int sx in new[]{-1,1})P("B-head","Cresta media luna "+sx,spike,new Vector3(sx*.035f,.2f,.1f),new Vector3(-12,0,-sx*38),new Vector3(.035f,.19f,.012f),brass);
  P("B-head","Cresta sello",cyl,new Vector3(0,.19f,.108f),new Vector3(90,0,0),new Vector3(.05f,.006f,.05f),brass);
  P("B-head","Mirada",cube,new Vector3(0,.085f,.112f),Vector3.zero,new Vector3(.1f,.012f,.012f),eye);
  foreach(int sx in new[]{-1,1})P("B-head","Colmillo menpo "+sx,spike,new Vector3(sx*.03f,.035f,.115f),new Vector3(180,0,0),new Vector3(.014f,.035f,.01f),brass);
  // shoulders: broad lacquered top plate with brass edge and cord
  foreach(int sx in new[]{-1,1}){string b=sx<0?"B-upperArm.L":"B-upperArm.R";
   string sh=sx<0?"B-shoulder.L":"B-shoulder.R";
   P(sh,"Sode mayor "+sx,cube,new Vector3(sx*.215f,.075f,-.01f),new Vector3(0,0,-sx*22),new Vector3(.15f,.016f,.17f),lacquer);
   P(sh,"Sode canto "+sx,cube,new Vector3(sx*.285f,.047f,-.01f),new Vector3(0,0,-sx*22),new Vector3(.01f,.02f,.175f),brass);
   P(sh,"Sode cordon "+sx,cube,new Vector3(sx*.215f,.086f,-.01f),new Vector3(0,0,-sx*22),new Vector3(.14f,.005f,.018f),crimson);
   string f=sx<0?"B-forearm.L":"B-forearm.R";
   P(f,"Kote placa "+sx,cube,new Vector3(sx*.1f,.035f,0),Vector3.zero,new Vector3(.17f,.014f,.065f),lacquer);
   for(int i=0;i<3;i++)P(f,"Kote remache "+sx+i,sph,new Vector3(sx*(.04f+i*.06f),.045f,0),Vector3.zero,Vector3.one*.016f,brass);
   P(f,"Kote banda "+sx,cyl,new Vector3(sx*.02f,0,0),new Vector3(0,0,90),new Vector3(.085f,.01f,.085f),crimsonDark);
   string h=sx<0?"B-hand.L":"B-hand.R";P(h,"Tekko "+sx,cube,new Vector3(sx*.05f,.022f,0),Vector3.zero,new Vector3(.08f,.01f,.07f),steel);
   string s=sx<0?"B-shin.L":"B-shin.R";
   P(s,"Suneate "+sx,cube,new Vector3(0,-.2f,.065f),new Vector3(-4,0,0),new Vector3(.075f,.3f,.016f),lacquer);
   for(int i=0;i<3;i++)P(s,"Suneate banda "+sx+i,cube,new Vector3(0,-.08f-i*.11f,.07f),new Vector3(-4,0,0),new Vector3(.085f,.012f,.02f),brass);
   P(s,"Rodillera "+sx,sph,new Vector3(0,-.01f,.07f),Vector3.zero,new Vector3(.09f,.09f,.04f),steel);}
  // kunai bandolier
  P("B-chest","Bandolera",cube,new Vector3(0,.08f,.135f),new Vector3(0,0,38),new Vector3(.42f,.035f,.012f),leather);
  for(int i=0;i<4;i++){float t=-.12f+i*.08f;P("B-chest","Kunai "+i,spike,new Vector3(t,.08f+t*.78f-.005f,.148f),new Vector3(180,0,0),new Vector3(.022f,.085f,.008f),steel);P("B-chest","Kunai anilla "+i,sph,new Vector3(t,.08f+t*.78f+.012f,.148f),Vector3.zero,Vector3.one*.018f,brass);}
  // katana scabbard at the left hip, short blade across the back
  P("B-hips","Saya",cyl,new Vector3(-.2f,-.02f,-.14f),new Vector3(68,12,0),new Vector3(.034f,.42f,.026f),lacquer);
  P("B-hips","Saya boca",cyl,new Vector3(-.165f,.135f,.245f),new Vector3(68,12,0),new Vector3(.042f,.012f,.034f),brass);
  P("B-hips","Saya contera",cyl,new Vector3(-.235f,-.175f,-.525f),new Vector3(68,12,0),new Vector3(.04f,.02f,.032f),brass);
  P("B-hips","Sageo",sph,new Vector3(-.2f,.02f,.0f),Vector3.zero,new Vector3(.05f,.045f,.05f),crimson);
  P("B-spine","Wakizashi vaina",cyl,new Vector3(-.02f,-.03f,-.15f),new Vector3(0,0,72),new Vector3(.03f,.2f,.024f),lacquer);
  P("B-spine","Wakizashi guarda",cyl,new Vector3(.172f,.032f,-.15f),new Vector3(0,0,72),new Vector3(.055f,.006f,.045f),brass);
  P("B-spine","Wakizashi empuñadura",cyl,new Vector3(.235f,.053f,-.15f),new Vector3(0,0,72),new Vector3(.028f,.065f,.024f),crimsonDark);
  P("B-spine","Wakizashi pomo",sph,new Vector3(.3f,.074f,-.15f),Vector3.zero,Vector3.one*.03f,brass);
  // obi knot with tassels, shuriken pouch
  P("B-hips","Obi",cyl,new Vector3(0,.06f,0),Vector3.zero,new Vector3(.36f,.03f,.27f),crimsonDark);
  P("B-hips","Obi nudo",sph,new Vector3(.06f,.06f,.15f),Vector3.zero,new Vector3(.08f,.06f,.05f),crimson);
  foreach(int k in new[]{0,1})P("B-hips","Obi borla "+k,spike,new Vector3(.04f+k*.045f,.03f,.16f),new Vector3(180,0,k*10-5),new Vector3(.03f,.2f+k*.05f,.012f),crimson);
  P("B-hips","Bolsa shuriken",cube,new Vector3(.2f,.0f,.05f),new Vector3(0,0,-6),new Vector3(.05f,.1f,.1f),leather);
  foreach(int k in new[]{0,1})P("B-hips","Shuriken "+k,cyl,new Vector3(.232f,.0f+k*.035f,.05f),new Vector3(0,0,90),new Vector3(.07f,.004f,.07f),steel);
  sb.AppendLine("Shinobi pieces="+End("Shinobi"));
  // ---------------- MAGO: astrólogo del eclipse ----------------
  MageOnly:
  Begin("Mage");
  // crown of floating shards
  for(int i=0;i<7;i++){float a=(i-3)*30*Mathf.Deg2Rad;P("B-head","Corona de esquirlas "+i,spike,new Vector3(Mathf.Sin(a)*.2f,.27f+Mathf.Cos(a)*.07f+(i==3?.05f:0),-.04f),new Vector3(0,0,-(i-3)*17),new Vector3(.026f,i==3?.2f:.13f-Mathf.Abs(i-3)*.012f,.02f),crystal);
   P("B-head","Corona engarce "+i,sph,new Vector3(Mathf.Sin(a)*.2f,.255f+Mathf.Cos(a)*.07f,-.04f),Vector3.zero,Vector3.one*.022f,brass);}
  // standing collar fan behind the neck
  for(int i=0;i<7;i++){float a=(i-3)*24*Mathf.Deg2Rad;P("B-chest","Cuello erguido "+i,cube,new Vector3(Mathf.Sin(a)*.17f,.3f+Mathf.Cos(a)*.03f,-.1f-Mathf.Cos(a)*.03f),new Vector3(-14,-(i-3)*20,-(i-3)*9),new Vector3(.062f,.2f,.008f),indigo);
   P("B-chest","Cuello ribete "+i,cube,new Vector3(Mathf.Sin(a)*.172f,.4f+Mathf.Cos(a)*.03f,-.125f-Mathf.Cos(a)*.03f),new Vector3(-14,-(i-3)*20,-(i-3)*9),new Vector3(.066f,.012f,.012f),brass);}
  // layered mantle pauldrons with tassels
  foreach(int sx in new[]{-1,1}){string b=sx<0?"B-upperArm.L":"B-upperArm.R";
   for(int i=0;i<3;i++){P(b,"Manto hombro "+sx+i,cube,new Vector3(sx*(-.02f+i*.045f),.085f-i*.03f,0),new Vector3(0,0,-sx*(8+i*13)),new Vector3(.17f,.016f,.24f-i*.02f),indigo);
    P(b,"Manto ribete "+sx+i,cube,new Vector3(sx*(.065f+i*.05f),.075f-i*.048f,0),new Vector3(0,0,-sx*(8+i*13)),new Vector3(.01f,.02f,.245f-i*.02f),brass);}
   P(b,"Manto broche "+sx,sph,new Vector3(-sx*.03f,.1f,.09f),Vector3.zero,Vector3.one*.04f,crystal);
   P(b,"Manto borla "+sx,spike,new Vector3(sx*.16f,-.02f,0),new Vector3(180,0,0),new Vector3(.025f,.12f,.025f),brass);
   string f=sx<0?"B-forearm.L":"B-forearm.R";
   for(int i=0;i<2;i++){P(f,"Brazalete "+sx+i,cyl,new Vector3(sx*(.06f+i*.09f),0,0),new Vector3(0,0,90),new Vector3(.1f,.012f,.1f),brass);}
   P(f,"Brazalete runa "+sx,cyl,new Vector3(sx*.105f,0,0),new Vector3(0,0,90),new Vector3(.092f,.014f,.092f),rune);}
  // chest focus: diamond crystal in a brass sun
  P("B-chest","Foco placa",cyl,new Vector3(0,.1f,.135f),new Vector3(90,0,0),new Vector3(.11f,.008f,.11f),brass);
  P("B-chest","Foco cristal sup",spike,new Vector3(0,.1f,.15f),Vector3.zero,new Vector3(.045f,.06f,.03f),crystal);
  P("B-chest","Foco cristal inf",spike,new Vector3(0,.1f,.15f),new Vector3(180,0,0),new Vector3(.045f,.06f,.03f),crystal);
  for(int i=0;i<8;i++){float a=i*45*Mathf.Deg2Rad;P("B-chest","Foco rayo "+i,spike,new Vector3(Mathf.Sin(a)*.06f,.1f+Mathf.Cos(a)*.06f,.14f),new Vector3(0,0,-i*45),new Vector3(.014f,.045f,.006f),brass);}
  // grimoire chained at the left hip
  P("B-hips","Grimorio",cube,new Vector3(-.22f,-.12f,.03f),new Vector3(0,0,8),new Vector3(.06f,.2f,.15f),leather);
  P("B-hips","Grimorio hojas",cube,new Vector3(-.222f,-.12f,.036f),new Vector3(0,0,8),new Vector3(.046f,.185f,.15f),paper);
  foreach(int a in new[]{-1,1})foreach(int c in new[]{-1,1})P("B-hips","Grimorio cantonera "+a+c,cube,new Vector3(-.252f,-.12f+a*.09f,.03f+c*.065f),new Vector3(0,0,8),new Vector3(.008f,.03f,.03f),brass);
  P("B-hips","Grimorio sello",sph,new Vector3(-.256f,-.12f,.03f),Vector3.zero,new Vector3(.012f,.045f,.045f),crystal);
  for(int i=0;i<3;i++)P("B-hips","Grimorio cadena "+i,cyl,new Vector3(-.2f,.04f-i*.035f,.03f),new Vector3(0,0,i%2*90),new Vector3(.022f,.012f,.022f),brass);
  // vials and talismans at the right hip
  foreach(int k in new[]{0,1}){P("B-hips","Vial "+k,cyl,new Vector3(.2f,-.06f,-.02f+k*.07f),Vector3.zero,new Vector3(.026f,.034f,.026f),k==0?vial:rune);P("B-hips","Vial corcho "+k,cyl,new Vector3(.2f,-.02f,-.02f+k*.07f),Vector3.zero,new Vector3(.016f,.01f,.016f),leather);}
  for(int i=0;i<4;i++){float x=.08f+i*.035f;P("B-hips","Talisman "+i,cube,new Vector3(x,-.14f-(i%2)*.03f,.14f-i*.02f),new Vector3(0,-i*14,i*3),new Vector3(.03f,.16f+(i%2)*.05f,.004f),paper);P("B-hips","Talisman lacre "+i,sph,new Vector3(x,-.1f,.144f-i*.02f),Vector3.zero,new Vector3(.016f,.016f,.008f),seal);}
  // belt with buckle; scroll cases on the back
  P("B-hips","Cinto",cyl,new Vector3(0,.05f,.01f),Vector3.zero,new Vector3(.38f,.022f,.3f),leather);
  P("B-hips","Cinto hebilla",cube,new Vector3(0,.05f,.155f),new Vector3(0,0,45),new Vector3(.05f,.05f,.012f),brass);
  foreach(int k in new[]{0,1}){P("B-chest","Portarrollos "+k,cyl,new Vector3(-.04f+k*.08f,-.02f,-.17f-k*.03f),new Vector3(0,0,28-k*14),new Vector3(.05f,.19f,.05f),leather);
   foreach(int e in new[]{-1,1})P("B-chest","Portarrollos tapa "+k+e,cyl,new Vector3(-.04f+k*.08f-e*Mathf.Sin((28-k*14)*Mathf.Deg2Rad)*.19f,-.02f+e*Mathf.Cos((28-k*14)*Mathf.Deg2Rad)*.19f,-.17f-k*.03f),new Vector3(0,0,28-k*14),new Vector3(.058f,.012f,.058f),brass);}
  sb.AppendLine("Mage pieces="+End("Mage"));
  AssetDatabase.SaveAssets();return sb.ToString();
 }
}
