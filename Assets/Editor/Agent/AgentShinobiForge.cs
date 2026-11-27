using System;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
// "Ceniza Silente": a Shinobi body forged from scratch as ONE skinned mesh on the class rig.
// Every vertex is generated here (lofts, shells, curved plates, tattered cloth) and weighted to the
// humanoid bones with smooth key blends, so cloth and plates follow the animation instead of being glued props.
public static class AgentShinobiForge{
 const string Prefab="Assets/Resources/Presentation/Classes/Shinobi.prefab";
 const string Dir="Assets/Resources/Presentation/Classes/ShinobiForja/";
 const string Legacy="Assets/Presentation/Legacy/Shinobi_Legado.prefab";
 const string Tag="Forja · ";
 // ---------------------------------------------------------------- builder
 enum M{Cloth,Lacquer,Iron,Leather,Linen,Silk,Ember,Brass,Count}
 static List<Vector3> V;static List<Vector2> UV;static List<BoneWeight> W;static List<int>[] T;
 static Dictionary<string,int> boneIndex;static Transform[] bones;
 struct BW{public int[] b;public float[] w;}
 static BW One(string bone)=>new BW{b=new[]{Idx(bone)},w=new[]{1f}};
 static int Idx(string n){if(!boneIndex.TryGetValue(n,out var i))throw new Exception("bone "+n);return i;}
 static BW Mix(params (string bone,float w)[] parts){var d=new Dictionary<int,float>();foreach(var p in parts){if(p.w<=0)continue;int i=Idx(p.bone);d[i]=(d.TryGetValue(i,out var x)?x:0)+p.w;}var top=d.OrderByDescending(k=>k.Value).Take(4).ToArray();float s=top.Sum(k=>k.Value);return new BW{b=top.Select(k=>k.Key).ToArray(),w=top.Select(k=>k.Value/s).ToArray()};}
 // keys: ascending values; smooth blend between consecutive keys.
 static BW Keys(float v,params (float at,string bone)[] k){if(v<=k[0].at)return One(k[0].bone);for(int i=0;i<k.Length-1;i++)if(v<=k[i+1].at){float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(k[i].at,k[i+1].at,v));return Mix((k[i].bone,1-t),(k[i+1].bone,t));}return One(k[k.Length-1].bone);}
 static BoneWeight ToUnity(BW x){var r=new BoneWeight();for(int i=0;i<x.b.Length;i++){switch(i){case 0:r.boneIndex0=x.b[i];r.weight0=x.w[i];break;case 1:r.boneIndex1=x.b[i];r.weight1=x.w[i];break;case 2:r.boneIndex2=x.b[i];r.weight2=x.w[i];break;case 3:r.boneIndex3=x.b[i];r.weight3=x.w[i];break;}}return r;}
 class Geo{public List<Vector3> v=new List<Vector3>();public List<int> t=new List<int>();
  public int Add(Vector3 p){v.Add(p);return v.Count-1;}
  public void Quad(int a,int b,int c,int d){t.Add(a);t.Add(b);t.Add(c);t.Add(a);t.Add(c);t.Add(d);}
  // copies the surface with reversed winding so thin cloth reads from both sides.
  public Geo TwoSided(float push=.0015f){int n=v.Count,m=t.Count;var c=Center();for(int i=0;i<n;i++){var p=v[i];v.Add(p);} for(int i=0;i<m;i+=3){t.Add(t[i]+n);t.Add(t[i+2]+n);t.Add(t[i+1]+n);}return this;}
  Vector3 Center(){var s=Vector3.zero;foreach(var p in v)s+=p;return s/Mathf.Max(1,v.Count);}
  public Geo Xf(Matrix4x4 m){for(int i=0;i<v.Count;i++)v[i]=m.MultiplyPoint3x4(v[i]);return this;}}
 static void Emit(Geo g,M mat,Func<Vector3,BW> weight){int b=V.Count;foreach(var p in g.v){V.Add(p);UV.Add(new Vector2(p.x+p.z,p.y));W.Add(ToUnity(weight(p)));}foreach(var i in g.t)T[(int)mat].Add(b+i);}
 static void Emit(Geo g,M mat,string bone)=>Emit(g,mat,_=>One(bone));
 // ---------------------------------------------------------------- shapes
 struct Ring{public Vector3 c,a1,a2;public float r1,r2;public Ring(Vector3 c,Vector3 a1,Vector3 a2,float r1,float r2){this.c=c;this.a1=a1;this.a2=a2;this.r1=r1;this.r2=r2;}}
 static Geo Loft(IList<Ring> rings,int seg,bool capA,bool capB,Func<int,float,float> wobble=null){var g=new Geo();for(int k=0;k<rings.Count;k++){var r=rings[k];for(int s=0;s<seg;s++){float a=s*Mathf.PI*2/seg;float f=wobble!=null?wobble(k,a):1;g.Add(r.c+(r.a1*Mathf.Cos(a)*r.r1+r.a2*Mathf.Sin(a)*r.r2)*f);}}
  // winding chosen so normals face outward when a1 x a2 points along the loft
  for(int k=0;k<rings.Count-1;k++)for(int s=0;s<seg;s++){int s2=(s+1)%seg;g.Quad(k*seg+s,k*seg+s2,(k+1)*seg+s2,(k+1)*seg+s);}
  if(capA){int c=g.Add(rings[0].c);for(int s=0;s<seg;s++){g.t.Add(c);g.t.Add((s+1)%seg);g.t.Add(s);}}
  if(capB){int o=(rings.Count-1)*seg;int c=g.Add(rings[rings.Count-1].c);for(int s=0;s<seg;s++){g.t.Add(c);g.t.Add(o+s);g.t.Add(o+(s+1)%seg);}}
  FixWinding(g,rings);return g;}
 // Ensure outward normals regardless of frame handedness.
 static void FixWinding(Geo g,IList<Ring> rings){var mid=rings[rings.Count/2];int seg=(g.v.Count-1)/Math.Max(1,rings.Count);if(g.t.Count<3)return;var a=g.v[g.t[0]];var b=g.v[g.t[1]];var c=g.v[g.t[2]];var n=Vector3.Cross(b-a,c-a);var axis=(a+b+c)/3-ClosestCenter(rings,(a+b+c)/3);if(Vector3.Dot(n,axis)<0)Flip(g);}
 static Vector3 ClosestCenter(IList<Ring> rings,Vector3 p){var best=rings[0].c;float d=1e9f;foreach(var r in rings){float x=(r.c-p).sqrMagnitude;if(x<d){d=x;best=r.c;}}return best;}
 static void Flip(Geo g){for(int i=0;i<g.t.Count;i+=3){int x=g.t[i+1];g.t[i+1]=g.t[i+2];g.t[i+2]=x;}}
 // Ellipsoidal shell, azimuth measured from +Z toward +X, elevation from the equator; cut() removes cells.
 static Geo Shell(Vector3 r,float az0,float az1,float el0,float el1,int na,int ne,Func<float,float,bool> cut=null,Func<float,float,Vector3,Vector3> shape=null){var g=new Geo();var idx=new int[na+1,ne+1];
  for(int i=0;i<=na;i++)for(int j=0;j<=ne;j++){float az=Mathf.Lerp(az0,az1,i/(float)na)*Mathf.Deg2Rad,el=Mathf.Lerp(el0,el1,j/(float)ne)*Mathf.Deg2Rad;var p=new Vector3(Mathf.Sin(az)*Mathf.Cos(el)*r.x,Mathf.Sin(el)*r.y,Mathf.Cos(az)*Mathf.Cos(el)*r.z);if(shape!=null)p=shape(az*Mathf.Rad2Deg,el*Mathf.Rad2Deg,p);idx[i,j]=g.Add(p);}
  for(int i=0;i<na;i++)for(int j=0;j<ne;j++){float caz=Mathf.Lerp(az0,az1,(i+.5f)/na),cel=Mathf.Lerp(el0,el1,(j+.5f)/ne);if(cut!=null&&cut(caz,cel))continue;g.Quad(idx[i,j],idx[i,j+1],idx[i+1,j+1],idx[i+1,j]);}
  // outward check
  if(g.t.Count>=3){var a=g.v[g.t[0]];var b=g.v[g.t[1]];var c=g.v[g.t[2]];if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3)<0)Flip(g);}return g;}
 // Closed curved plate around the Y axis: arc of an ellipse (rx,rz) centred on azimuth az (deg from +Z toward +X).
 static Geo Plate(float rx,float rz,float az,float half,float y0,float y1,float thick,int seg=14,float flare=0){var g=new Geo();int n=seg+1;
  for(int layer=0;layer<2;layer++)for(int row=0;row<2;row++)for(int s=0;s<n;s++){float a=(az-half+2*half*s/seg)*Mathf.Deg2Rad;float y=row==0?y0:y1;float grow=(layer==0?thick:0)+(row==0?flare:0);g.Add(new Vector3(Mathf.Sin(a)*(rx+grow),y,Mathf.Cos(a)*(rz+grow)));}
  int O(int layer,int row,int s)=>layer*2*n+row*n+s;
  for(int s=0;s<seg;s++){g.Quad(O(0,0,s),O(0,1,s),O(0,1,s+1),O(0,0,s+1));g.Quad(O(1,0,s+1),O(1,1,s+1),O(1,1,s),O(1,0,s));g.Quad(O(0,1,s),O(1,1,s),O(1,1,s+1),O(0,1,s+1));g.Quad(O(0,0,s+1),O(1,0,s+1),O(1,0,s),O(0,0,s));}
  g.Quad(O(0,0,0),O(1,0,0),O(1,1,0),O(0,1,0));g.Quad(O(0,1,seg),O(1,1,seg),O(1,0,seg),O(0,0,seg));
  if(g.t.Count>=3){var a=g.v[g.t[0]];var b=g.v[g.t[1]];var c=g.v[g.t[2]];var mid=(a+b+c)/3;mid.y=0;if(Vector3.Dot(Vector3.Cross(b-a,c-a),mid)<0)Flip(g);}return g;}
 static Geo Torus(float R,float r,int su=20,int sv=8){var g=new Geo();for(int u=0;u<su;u++)for(int v=0;v<sv;v++){float a=u*Mathf.PI*2/su,b=v*Mathf.PI*2/sv;g.Add(new Vector3(Mathf.Cos(a)*(R+r*Mathf.Cos(b)),r*Mathf.Sin(b),Mathf.Sin(a)*(R+r*Mathf.Cos(b))));}
  for(int u=0;u<su;u++)for(int v=0;v<sv;v++){int u2=(u+1)%su,v2=(v+1)%sv;g.Quad(u*sv+v,u*sv+v2,u2*sv+v2,u2*sv+v);}var a0=g.v[g.t[0]];var b0=g.v[g.t[1]];var c0=g.v[g.t[2]];var m=(a0+b0+c0)/3;var ring=new Vector3(m.x,0,m.z).normalized*R;if(Vector3.Dot(Vector3.Cross(b0-a0,c0-a0),m-ring)<0)Flip(g);return g;}
 static Geo Box(Vector3 size){var g=new Geo();var h=size/2;Vector3[] c={new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(-h.x,-h.y,h.z),new Vector3(h.x,-h.y,h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z)};
  int[][] f={new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{3,7,6,2},new[]{0,4,7,3},new[]{1,2,6,5}};foreach(var q in f){int b=g.v.Count;foreach(var i in q)g.Add(c[i]);g.Quad(b,b+1,b+2,b+3);}return g;}
 static Geo Sphere(Vector3 r,int na=16,int ne=10)=>Shell(r,0,360,-90,90,na,ne);
 // Tattered hanging cloth: top edge along the given curve points, hanging down by len with jagged hem.
 static Geo Tatter(IList<Vector3> top,IList<Vector3> outward,float len,int seed,float rag=.35f,int rows=6,float flare=.04f){var g=new Geo();var rnd=new System.Random(seed);int n=top.Count;var hem=new float[n];for(int i=0;i<n;i++)hem[i]=len*(1-rag*(float)rnd.NextDouble()*((i%2==0)?1f:.45f));
  for(int i=0;i<n;i++)for(int r=0;r<=rows;r++){float f=r/(float)rows;g.Add(top[i]+Vector3.down*hem[i]*f+outward[i]*flare*f*f);}
  for(int i=0;i<n-1;i++)for(int r=0;r<rows;r++)g.Quad(i*(rows+1)+r,(i+1)*(rows+1)+r,(i+1)*(rows+1)+r+1,i*(rows+1)+r+1);
  var a=g.v[g.t[0]];var b=g.v[g.t[1]];var c=g.v[g.t[2]];if(Vector3.Dot(Vector3.Cross(b-a,c-a),outward[0])<0)Flip(g);return g.TwoSided();}
 static Vector3 Pos(string b)=>bones[Idx(b)].position;
 static Material Mat(string name,Color c,float metal,float smooth,Color? glow=null){var path=Dir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
  m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Cull",2);if(glow.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",glow.Value);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}else{m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}EditorUtility.SetDirty(m);return m;}
 static Matrix4x4 TRS(Vector3 p,Vector3 e,Vector3? s=null)=>Matrix4x4.TRS(p,Quaternion.Euler(e),s??Vector3.one);
 // ---------------------------------------------------------------- the figure
 static void Body(){
  // TORSO: one loft hips→head with smooth key weights by height.
  float[,] torso={{.84f,.10f,.08f,.02f},{.88f,.14f,.10f,.02f},{.95f,.155f,.11f,.02f},{1.02f,.15f,.105f,.02f},{1.10f,.135f,.095f,.025f},{1.18f,.145f,.10f,.03f},{1.27f,.165f,.115f,.035f},{1.36f,.175f,.12f,.035f},{1.43f,.17f,.11f,.02f},{1.48f,.13f,.09f,0f},{1.52f,.065f,.06f,.005f},{1.58f,.055f,.055f,.015f},{1.64f,.05f,.05f,.02f}};
  var rings=new List<Ring>();for(int i=0;i<torso.GetLength(0);i++)rings.Add(new Ring(new Vector3(0,torso[i,0],torso[i,3]),Vector3.right,Vector3.forward,torso[i,1],torso[i,2]));
  Emit(Loft(rings,24,true,true),M.Cloth,p=>TorsoW(p.y));
  // HEAD (hidden mostly under hood and mask)
  Emit(Sphere(new Vector3(.08f,.105f,.095f)).Xf(TRS(new Vector3(0,1.705f,.03f),Vector3.zero)),M.Cloth,"B-head");
  foreach(int s in new[]{-1,1}){string L=s<0?".L":".R";
   // ARM: shoulder → fingertips mitten, weights by |x|
   float[,] arm={{.11f,.08f,.085f},{.16f,.07f,.075f},{.22f,.062f,.064f},{.30f,.056f,.056f},{.38f,.048f,.05f},{.434f,.043f,.045f},{.48f,.047f,.05f},{.55f,.042f,.045f},{.615f,.031f,.036f},{.65f,.026f,.046f},{.72f,.024f,.05f},{.77f,.021f,.048f},{.82f,.017f,.044f},{.86f,.013f,.036f},{.885f,.006f,.02f}};
   var path=new[]{new Vector3(.12f*s,1.45f,0),Pos("B-upperArm"+L),Pos("B-forearm"+L),Pos("B-hand"+L),Pos("B-middleFinger01"+L),Pos("B-middleFinger03"+L)+new Vector3(.03f*s,0,0)};
   rings=new List<Ring>();for(int i=0;i<arm.GetLength(0);i++){float x=arm[i,0];rings.Add(new Ring(OnPath(path,x),Vector3.up,Vector3.forward,arm[i,1],arm[i,2]));}
   Emit(Loft(rings,14,true,true),M.Cloth,p=>ArmW(Mathf.Abs(p.x),L));
   // THUMB
   var tp=new[]{Vector3.Lerp(Pos("B-hand"+L),Pos("B-thumb01"+L),.4f),Pos("B-thumb01"+L),Pos("B-thumb02"+L),Pos("B-thumb03"+L),Pos("B-thumb03"+L)+(Pos("B-thumb03"+L)-Pos("B-thumb02"+L)).normalized*.025f};float[] tr={.02f,.017f,.015f,.013f,.006f};
   rings=new List<Ring>();for(int i=0;i<tp.Length;i++){var d=(i<tp.Length-1?tp[i+1]-tp[i]:tp[i]-tp[i-1]).normalized;var a1=Vector3.Cross(d,Vector3.up).normalized;var a2=Vector3.Cross(a1,d);rings.Add(new Ring(tp[i],a1,a2,tr[i],tr[i]*1.1f));}
   Emit(Loft(rings,8,true,true),M.Leather,p=>{var a=Pos("B-thumb01"+L);var b=Pos("B-thumb03"+L);float t=Vector3.Dot(p-a,(b-a).normalized)/(b-a).magnitude;return Keys(t,(-.4f,"B-hand"+L),(-.1f,"B-thumb01"+L),(.35f,"B-thumb01"+L),(.55f,"B-thumb02"+L),(.85f,"B-thumb03"+L));});
   // LEG
   float[,] leg={{.96f,.088f,.09f},{.92f,.09f,.094f},{.85f,.088f,.092f},{.75f,.082f,.085f},{.65f,.072f,.075f},{.56f,.06f,.063f},{.515f,.056f,.06f},{.47f,.058f,.062f},{.40f,.063f,.066f},{.32f,.058f,.06f},{.24f,.048f,.05f},{.17f,.042f,.044f},{.12f,.042f,.045f},{.08f,.044f,.046f}};
   var hip=Pos("B-thigh"+L);var knee=Pos("B-shin"+L);var ankle=Pos("B-foot"+L);
   rings=new List<Ring>();for(int i=0;i<leg.GetLength(0);i++){float y=leg[i,0];Vector3 c=y>knee.y?Vector3.Lerp(knee,hip,Mathf.InverseLerp(knee.y,hip.y,y)):Vector3.Lerp(ankle,knee,Mathf.InverseLerp(ankle.y,knee.y,y));c.y=y;if(y>hip.y)c.x=hip.x;rings.Add(new Ring(c,Vector3.right,Vector3.forward,leg[i,1],leg[i,2]));}
   Emit(Loft(rings,16,false,true),M.Cloth,p=>Keys(p.y,(.13f,"B-foot"+L),(.19f,"B-shin"+L),(.47f,"B-shin"+L),(.56f,"B-thigh"+L),(.88f,"B-thigh"+L),(.97f,"B-hips")));
   // TABI BOOT (split toe hinted with a groove ridge)
   float[,] foot={{-.075f,.075f,.035f,.05f},{-.04f,.068f,.045f,.058f},{0f,.062f,.048f,.06f},{.05f,.05f,.05f,.046f},{.10f,.04f,.05f,.034f},{.15f,.032f,.046f,.026f},{.19f,.026f,.038f,.02f},{.215f,.022f,.022f,.012f}};
   rings=new List<Ring>();for(int i=0;i<foot.GetLength(0);i++)rings.Add(new Ring(new Vector3(ankle.x+.004f*s,foot[i,1],foot[i,0]),Vector3.right,Vector3.up,foot[i,2],foot[i,3]));
   Emit(Loft(rings,14,true,true),M.Leather,p=>Keys(p.z,(.09f,"B-foot"+L),(.15f,"B-toe"+L)));
   Emit(Box(new Vector3(.006f,.03f,.06f)).Xf(TRS(new Vector3(ankle.x+.012f*s,.035f,.175f),Vector3.zero)),M.Cloth,"B-toe"+L);
   // sole
   Emit(Box(new Vector3(.085f,.012f,.29f)).Xf(TRS(new Vector3(ankle.x+.004f*s,.006f,.07f),Vector3.zero)),M.Lacquer,p=>Keys(p.z,(.09f,"B-foot"+L),(.15f,"B-toe"+L)));
  }}
 static Vector3 OnPath(Vector3[] path,float ax){for(int i=0;i<path.Length-1;i++){float a=Mathf.Abs(path[i].x),b=Mathf.Abs(path[i+1].x);if(ax<=b||i==path.Length-2){var p=Vector3.Lerp(path[i],path[i+1],Mathf.InverseLerp(a,b,ax));return p;}}return path[path.Length-1];}
 static BW TorsoW(float y)=>Keys(y,(.98f,"B-hips"),(1.07f,"B-spine"),(1.18f,"B-spine"),(1.30f,"B-chest"),(1.48f,"B-chest"),(1.55f,"B-neck"),(1.62f,"B-neck"),(1.67f,"B-head"));
 static BW ArmW(float ax,string L)=>Keys(ax,(.11f,"B-chest"),(.16f,"B-shoulder"+L),(.23f,"B-upperArm"+L),(.40f,"B-upperArm"+L),(.47f,"B-forearm"+L),(.60f,"B-forearm"+L),(.655f,"B-hand"+L),(.76f,"B-hand"+L),(.79f,"B-middleFinger01"+L),(.82f,"B-middleFinger01"+L),(.845f,"B-middleFinger02"+L),(.86f,"B-middleFinger02"+L),(.875f,"B-middleFinger03"+L));
 static void Garments(){
  var head=new Vector3(0,1.70f,.01f);
  // HOOD (zukin): deep cowl with face window, drawn back into a point; two-sided.
  Emit(Shell(new Vector3(.118f,.137f,.128f),-180,180,-72,90,32,18,(az,el)=>Mathf.Abs(az)<46&&el>-38&&el<24,(az,el,p)=>{float back=Mathf.Clamp01((Mathf.Abs(az)-100)/80f);if(el>10)p+=new Vector3(0,.018f,-.035f)*back*Mathf.InverseLerp(10,90,el);if(Mathf.Abs(az)<62&&el>14){float k=Mathf.InverseLerp(14,40,el)*(1-Mathf.Abs(az)/62f);p.z+=.035f*k;p.y-=.012f*k;}return p;}).TwoSided().Xf(TRS(head,Vector3.zero)),M.Cloth,p=>Keys(p.y,(1.50f,"B-chest"),(1.57f,"B-neck"),(1.64f,"B-head")));
  // hood tail: the cowl drops into a ragged point between the shoulder blades
  { var t5=new List<Vector3>();var o5=new List<Vector3>();for(int i=0;i<=8;i++){float az=(150+i*60/8f)*Mathf.Deg2Rad;t5.Add(head+new Vector3(Mathf.Sin(az)*.1f,-.03f,Mathf.Cos(az)*.12f));o5.Add(new Vector3(Mathf.Sin(az)*.4f,0,Mathf.Cos(az)).normalized);}
    Emit(Tatter(t5,o5,.3f,5,.45f,6,.06f),M.Cloth,p=>Keys(p.y,(1.42f,"B-chest"),(1.55f,"B-neck"),(1.64f,"B-head")));}
  // brow band of burnt iron across the hood opening (hachigane)
  Emit(Shell(new Vector3(.124f,.03f,.134f),-62,62,-45,45,12,2).Xf(TRS(head+new Vector3(0,.052f,0),Vector3.zero)),M.Iron,"B-head");
  Emit(Box(new Vector3(.03f,.026f,.006f)).Xf(TRS(head+new Vector3(0,.052f,.136f),new Vector3(0,0,45))),M.Brass,"B-head");
  // MASK (menpo): lower-face shell + ember breath slits + ember eyes.
  Emit(Shell(new Vector3(.094f,.1f,.112f),-78,78,-52,-6,14,6,null,(az,el,p)=>{if(Mathf.Abs(az)<14)p.z+=.012f*(1-Mathf.Abs(az)/14);return p;}).TwoSided().Xf(TRS(new Vector3(0,1.71f,.03f),Vector3.zero)),M.Iron,"B-head");
  // 2026-11-20: the glowing ovals + three glowing bars read as a cartoon face (";)"). Menpo now: brass breathing grille
  // (no glow), a nose ridge, cheek guards and thin, angled eye slits with only a faint ember inside a dark recess.
  for(int i=0;i<4;i++)Emit(Box(new Vector3(.006f,.022f-.003f*i,.004f)).Xf(TRS(new Vector3(-.0135f+.009f*i,1.668f,.148f),Vector3.zero)),M.Brass,"B-head");
  Emit(Box(new Vector3(.01f,.03f,.012f)).Xf(TRS(new Vector3(0,1.697f,.142f),new Vector3(-12,0,0))),M.Lacquer,"B-head");
  foreach(int s in new[]{-1,1})Emit(Box(new Vector3(.03f,.04f,.006f)).Xf(TRS(new Vector3(.055f*s,1.672f,.122f),new Vector3(0,-34*s,6*s))),M.Lacquer,"B-head");
  foreach(int s in new[]{-1,1}){Emit(Box(new Vector3(.03f,.007f,.004f)).Xf(TRS(new Vector3(.031f*s,1.722f,.127f),new Vector3(0,-10*s,-14*s))),M.Iron,"B-head");
   Emit(Box(new Vector3(.018f,.0018f,.002f)).Xf(TRS(new Vector3(.031f*s,1.7215f,.1295f),new Vector3(0,-10*s,-14*s))),M.Ember,"B-head");}
  // CAPELET under the hood: tattered collar over the shoulders.
  var top=new List<Vector3>();var outw=new List<Vector3>();for(int i=0;i<=28;i++){float a=i*Mathf.PI*2/28;top.Add(new Vector3(Mathf.Sin(a)*.085f,1.565f,.01f+Mathf.Cos(a)*.08f));outw.Add(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)));}
  var cap=new Geo();{int n=top.Count,rows=5;var rnd=new System.Random(7);var hem=new float[n];for(int i=0;i<n;i++)hem[i]=(i%2==0?1:.8f)*(1-.25f*(float)rnd.NextDouble());hem[n-1]=hem[0];
   for(int i=0;i<n;i++)for(int r=0;r<=rows;r++){float f=r/(float)rows*hem[i];var o=outw[i];cap.Add(new Vector3(o.x*Mathf.Lerp(.085f,.205f,f),Mathf.Lerp(1.565f,1.415f+.125f*o.x*o.x,f),.01f+o.z*Mathf.Lerp(.08f,.155f,f)));}
   for(int i=0;i<n-1;i++)for(int r=0;r<rows;r++)cap.Quad(i*(rows+1)+r,i*(rows+1)+r+1,(i+1)*(rows+1)+r+1,(i+1)*(rows+1)+r);var a=cap.v[cap.t[0]];var b=cap.v[cap.t[1]];var c=cap.v[cap.t[2]];var m=(a+b+c)/3;if(Vector3.Dot(Vector3.Cross(b-a,c-a),new Vector3(m.x,0,m.z-.01f))<0)Flip(cap);cap.TwoSided();}
  Emit(cap,M.Cloth,p=>{var w=Keys(p.y,(1.46f,"B-chest"),(1.55f,"B-neck"));if(Mathf.Abs(p.x)>.15f){string L=p.x<0?".L":".R";return Mix((w.b.Length>0?NameOf(w.b[0]):"B-chest",.6f),("B-shoulder"+L,.4f));}return w;});
  // SCARF wrap at the throat (crimson) – ribbons added as ScarfRibbon components.
  Emit(Torus(.068f,.022f,20,8).Xf(TRS(new Vector3(0,1.535f,.012f),new Vector3(8,0,0),new Vector3(1,1,.92f))),M.Silk,p=>Keys(p.y,(1.51f,"B-chest"),(1.56f,"B-neck")));
  // CHEST PLATE (do): lacquer, with a burning fissure.
  var lift=TRS(new Vector3(0,0,.03f),Vector3.zero);Emit(Plate(.185f,.128f,0,72,1.215f,1.425f,.014f,16).Xf(lift),M.Lacquer,"B-chest");
  Emit(Plate(.18f,.124f,180,62,1.24f,1.43f,.012f,14).Xf(lift),M.Lacquer,"B-chest");
  // brass lacing rows across the plate
  foreach(float y in new[]{1.255f,1.335f})Emit(Plate(.2f,.143f,0,66,y,y+.008f,.004f,16).Xf(lift),M.Brass,"B-chest");
  { var pts=new[]{new Vector2(.05f,1.41f),new Vector2(.035f,1.38f),new Vector2(.055f,1.35f),new Vector2(.03f,1.31f),new Vector2(.05f,1.27f),new Vector2(.038f,1.235f)};
    for(int i=0;i<pts.Length-1;i++){var a=pts[i];var b=pts[i+1];var mid=(a+b)/2;float len=(b-a).magnitude;float ang=Mathf.Atan2(b.x-a.x,a.y-b.y)*Mathf.Rad2Deg;float z=.03f+(.128f+.016f)*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow(mid.x/.199f,2)));
     Emit(Box(new Vector3(.006f,len+.004f,.006f)).Xf(TRS(new Vector3(mid.x,mid.y,z),new Vector3(0,-Mathf.Asin(mid.x/.199f)*Mathf.Rad2Deg*.5f,-ang))),M.Ember,"B-chest");}}
  // ABDOMEN LAMELLAE: three lacquer bands over the cloth.
  for(int i=0;i<3;i++){float y=1.085f+i*.045f;Emit(Plate(.152f+.004f*i,.106f+.004f*i,0,150,y,y+.036f,.01f,22).Xf(TRS(new Vector3(0,0,.027f),Vector3.zero)),M.Lacquer,p=>TorsoW(p.y));}
  // OBI sash + knot + trailing ends
  Emit(Plate(.162f,.118f,0,180,.955f,1.045f,.02f,28).Xf(TRS(new Vector3(0,0,.02f),Vector3.zero)),M.Silk,p=>Mix(("B-hips",.6f),("B-spine",.4f)));
  Emit(Box(new Vector3(.07f,.06f,.04f)).Xf(TRS(new Vector3(.02f,1.0f,-.115f),new Vector3(0,0,25))),M.Silk,"B-hips");
  foreach(int k in new[]{0,1}){var t2=new List<Vector3>();var o2=new List<Vector3>();for(int i=0;i<=3;i++){t2.Add(new Vector3(.005f+k*.04f+i*.012f,.985f,-.135f-k*.004f));o2.Add(Vector3.back);}
   Emit(Tatter(t2,o2,k==0?.36f:.27f,11+k,.3f,6,.03f),M.Silk,p=>Keys(p.y,(.75f,"B-hips"),(.9f,"B-hips")));}
  // KUSAZURI: four tattered skirt panels with lacquer top plates, blending hips→thigh.
  foreach(float az in new[]{-28f,28f,-82f,82f,-140f,140f}){int s=az<0?-1:1;string L=s<0?".L":".R";var t3=new List<Vector3>();var o3=new List<Vector3>();float half=Mathf.Abs(az)>120?20:16;
   for(int i=0;i<=6;i++){float a=(az-half+2*half*i/6f)*Mathf.Deg2Rad;t3.Add(new Vector3(Mathf.Sin(a)*.19f,.975f,.02f+Mathf.Cos(a)*.145f));o3.Add(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)));}
   float len=Mathf.Abs(az)>120?.30f:.36f;
   Emit(Tatter(t3,o3,len,(int)az+40,.32f,7,.085f),M.Cloth,p=>{float f=Mathf.InverseLerp(.975f,.975f-len,p.y);return Mix(("B-hips",1-.65f*f),("B-thigh"+L,.65f*f));});
   for(int k=0;k<2;k++){float y=.93f-k*.06f;float r=.012f*k;Emit(Plate(.196f+r,.151f+r,az,half-1,y-.05f,y,.008f,8,.012f).Xf(TRS(new Vector3(0,0,.02f),Vector3.zero)),M.Lacquer,p=>{float f=Mathf.InverseLerp(.975f,.975f-len,p.y);return Mix(("B-hips",1-.65f*f),("B-thigh"+L,.65f*f));});}}
  // BACK MANTLE: tattered half-cape from the shoulder blades to the hips.
  { var t4=new List<Vector3>();var o4=new List<Vector3>();for(int i=0;i<=14;i++){float a=(120+i*120/14f)*Mathf.Deg2Rad;t4.Add(new Vector3(Mathf.Sin(a)*.21f,1.44f,.02f+Mathf.Cos(a)*.16f));o4.Add(new Vector3(Mathf.Sin(a)*.3f,0,Mathf.Cos(a)).normalized);}
    Emit(Tatter(t4,o4,.66f,99,.3f,10,.1f),M.Cloth,p=>Keys(p.y,(.92f,"B-hips"),(1.12f,"B-spine"),(1.3f,"B-chest")));}
  // SWORD CORD (sageo) across the back, left shoulder → right hip.
  { var a=new Vector3(-.15f,1.45f,-.1f);var b=new Vector3(.16f,1.0f,-.13f);var rings=new List<Ring>();for(int i=0;i<=10;i++){float t=i/10f;var c=Vector3.Lerp(a,b,t);c.z-=Mathf.Sin(t*Mathf.PI)*.035f;var d=(b-a).normalized;var a1=Vector3.Cross(d,Vector3.forward).normalized;rings.Add(new Ring(c,a1,Vector3.Cross(a1,d),.011f,.011f));}
    Emit(Loft(rings,6,true,true),M.Silk,p=>TorsoW(p.y));}
  // TANTO across the small of the back.
  Emit(Loft(new[]{new Ring(new Vector3(-.17f,1.06f,-.15f),Vector3.up,Vector3.forward,.016f,.012f),new Ring(new Vector3(.12f,1.0f,-.15f),Vector3.up,Vector3.forward,.016f,.012f)},8,true,true),M.Lacquer,"B-hips");
  Emit(Loft(new[]{new Ring(new Vector3(.13f,.998f,-.15f),Vector3.up,Vector3.forward,.013f,.012f),new Ring(new Vector3(.23f,.978f,-.15f),Vector3.up,Vector3.forward,.013f,.012f)},8,true,true),M.Linen,"B-hips");
  Emit(Box(new Vector3(.008f,.045f,.045f)).Xf(TRS(new Vector3(.125f,1.0f,-.15f),new Vector3(0,0,-11))),M.Brass,"B-hips");
  foreach(int s in new[]{-1,1}){string L=s<0?".L":".R";var up=Pos("B-upperArm"+L);var fo=Pos("B-forearm"+L);var ha=Pos("B-hand"+L);
   var armRot=new Vector3(0,0,90);// maps local Y axis onto X
   if(s<0){// LEFT: heavy layered sode
    for(int k=0;k<3;k++){var g=Plate(.085f+.012f*k,.09f+.012f*k,0,62,0,.12f,.01f,12);// around Y, then lay along the arm
     // rotate so the plate wraps the arm axis (X) covering its top (+Y)
     g.Xf(Matrix4x4.TRS(new Vector3(-.19f-.045f*k,1.47f-.012f*k,-.012f),Quaternion.LookRotation(Vector3.up,Vector3.left),Vector3.one));
     // tilt plates outward/downward like hanging lames
     Emit(g,M.Lacquer,p=>Mix(("B-upperArm.L",.85f),("B-shoulder.L",.15f)));
     var trim=Plate(.086f+.012f*k,.091f+.012f*k,0,62,0,.008f,.012f,12);trim.Xf(Matrix4x4.TRS(new Vector3(-.19f-.045f*k-.118f,1.47f-.012f*k,-.012f),Quaternion.LookRotation(Vector3.up,Vector3.left),Vector3.one));Emit(trim,M.Brass,p=>One("B-upperArm.L"));}
    Emit(Sphere(new Vector3(.012f,.012f,.012f),8,6).Xf(TRS(new Vector3(-.30f,1.57f,-.01f),Vector3.zero)),M.Ember,"B-upperArm.L");
   }else{// RIGHT: sword arm left free – linen wraps and a single small cop
    for(int k=0;k<6;k++){float x=.22f+k*.028f;Emit(Torus(.061f-.0015f*k,.0055f,16,6).Xf(TRS(new Vector3(x,Mathf.Lerp(up.y,fo.y,(x-up.x)/(fo.x-up.x)),-.016f),new Vector3(0,0,90+(k%2==0?8:-8)))),M.Linen,p=>ArmW(Mathf.Abs(p.x),".R"));}
    Emit(Shell(new Vector3(.07f,.07f,.075f),-90,90,0,90,10,5).TwoSided().Xf(TRS(new Vector3(.2f,1.46f,-.015f),new Vector3(0,0,-20))),M.Iron,p=>Mix(("B-upperArm.R",.7f),("B-shoulder.R",.3f)));}
   // KOTE forearm guard (both) + tekko hand plate + wraps
   var kote=Plate(.055f,.058f,0,75,0,.15f,.008f,10);kote.Xf(Matrix4x4.TRS(new Vector3(.455f*s,1.437f,-.013f),Quaternion.LookRotation(Vector3.up,Vector3.right*s),Vector3.one));
   // the plate's arc centre is along +Z of its local frame → after rotation it sits on top (+Y) of the forearm
   Emit(kote,M.Iron,p=>ArmW(Mathf.Abs(p.x),L));
   for(int k=0;k<4;k++){float x=.445f+k*.045f;Emit(Torus(.05f-.004f*k,.005f,16,6).Xf(TRS(new Vector3(x*s,Mathf.Lerp(fo.y,ha.y,(x-.434f)/.195f),-.012f),new Vector3(0,0,90+(k%2==0?10:-10)))),M.Linen,p=>ArmW(Mathf.Abs(p.x),L));}
   Emit(Box(new Vector3(.085f,.008f,.07f)).Xf(TRS(new Vector3(.705f*s,1.452f,0),new Vector3(0,0,-3*s))),M.Iron,"B-hand"+L);
   // SUNEATE shin guard + knee cop + wraps
   var knee=Pos("B-shin"+L);var ank=Pos("B-foot"+L);
   Emit(Plate(.066f,.068f,0,55,.23f,.45f,.007f,10).Xf(TRS(new Vector3(knee.x,0,(knee.z+ank.z)/2+.004f),Vector3.zero)),M.Lacquer,"B-shin"+L);
   for(int k=0;k<3;k++){float x=-.026f+k*.026f;Emit(Box(new Vector3(.006f,.24f,.006f)).Xf(TRS(new Vector3(knee.x+x,.33f,(knee.z+ank.z)/2+.004f+.077f-Mathf.Abs(x)*.6f),Vector3.zero)),M.Brass,"B-shin"+L);}
   Emit(Shell(new Vector3(.042f,.04f,.036f),-80,80,-60,70,10,6).TwoSided().Xf(TRS(new Vector3(knee.x,.525f,knee.z+.03f),Vector3.zero)),M.Iron,"B-shin"+L);
   for(int k=0;k<4;k++){float y=.13f+k*.02f;Emit(Torus(.047f,.005f,16,6).Xf(TRS(new Vector3(ank.x,y,Mathf.Lerp(ank.z,knee.z,(y-ank.y)/(knee.y-ank.y))),new Vector3(k%2==0?6:-6,0,0))),M.Linen,p=>Keys(p.y,(.13f,"B-foot"+L),(.17f,"B-shin"+L)));}
   for(int k=0;k<3;k++){float y=.75f+k*.05f;Emit(Torus(.092f,.005f,18,6).Xf(TRS(new Vector3(Mathf.Lerp(knee.x,Pos("B-thigh"+L).x,(y-knee.y)/(.925f-knee.y)),y,.02f),new Vector3(0,0,k%2==0?5:-5))),M.Linen,"B-thigh"+L);}
  }
  // KUNAI holster on the right thigh
  Emit(Box(new Vector3(.03f,.13f,.07f)).Xf(TRS(new Vector3(.19f,.73f,.0f),new Vector3(0,0,-4))),M.Leather,"B-thigh.R");
  for(int k=0;k<3;k++)Emit(Loft(new[]{new Ring(new Vector3(.19f,.79f,-.02f+k*.022f),Vector3.right,Vector3.forward,.005f,.005f),new Ring(new Vector3(.19f,.84f,-.02f+k*.022f),Vector3.right,Vector3.forward,.005f,.005f)},6,true,true),M.Iron,"B-thigh.R");
 }
 static string NameOf(int i)=>bones[i].name;
 // ---------------------------------------------------------------- entry
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";
  Directory.CreateDirectory(Dir);Directory.CreateDirectory(Path.GetDirectoryName(Legacy));AssetDatabase.Refresh();
  if(!File.Exists(Legacy))AssetDatabase.CopyAsset(Prefab,Legacy);
  var root=PrefabUtility.LoadPrefabContents(Prefab);
  try{
   // strip every old visual (skinned body, Identity armour, Couture pieces, previous forge)
   int removed=0;foreach(var r in root.GetComponentsInChildren<Renderer>(true).ToArray()){if(!r)continue;var go=r.gameObject;if(go.name.StartsWith("B-")||go.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("B-")))continue;UnityEngine.Object.DestroyImmediate(go);removed++;}
   foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t&&(t.name.StartsWith("Couture · ")||t.name.StartsWith(Tag))).ToArray())if(t){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;}
   bones=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("B-")).ToArray();boneIndex=new Dictionary<string,int>();for(int i=0;i<bones.Length;i++)boneIndex[bones[i].name]=i;
   V=new List<Vector3>();UV=new List<Vector2>();W=new List<BoneWeight>();T=new List<int>[(int)M.Count];for(int i=0;i<T.Length;i++)T[i]=new List<int>();
   Body();Garments();
   var meshPath=Dir+"Ceniza Silente.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);bool fresh=!mesh;if(fresh)mesh=new Mesh();mesh.Clear();mesh.name="Shinobi · Ceniza Silente";mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(V);mesh.SetUVs(0,UV);mesh.subMeshCount=(int)M.Count;for(int i=0;i<T.Length;i++)mesh.SetTriangles(T[i],i);mesh.boneWeights=W.ToArray();
   var host=new GameObject(Tag+"Ceniza Silente");host.transform.SetParent(root.transform,false);host.transform.SetSiblingIndex(1);
   mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*host.transform.localToWorldMatrix).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
   if(fresh)AssetDatabase.CreateAsset(mesh,meshPath);else EditorUtility.SetDirty(mesh);
   var mats=new Material[(int)M.Count];
   mats[(int)M.Cloth]=Mat("Paño ceniza",new Color(.085f,.072f,.066f),0,.12f);
   mats[(int)M.Lacquer]=Mat("Laca obsidiana",new Color(.035f,.03f,.034f),.35f,.8f);
   mats[(int)M.Iron]=Mat("Hierro quemado",new Color(.17f,.13f,.11f),.85f,.36f);
   mats[(int)M.Leather]=Mat("Cuero ahumado",new Color(.10f,.07f,.055f),0,.32f);
   mats[(int)M.Linen]=Mat("Vendas de ceniza",new Color(.30f,.26f,.22f),0,.08f);
   mats[(int)M.Silk]=Mat("Seda de sangre",new Color(.32f,.03f,.045f),0,.38f);
   mats[(int)M.Ember]=Mat("Brasa viva",new Color(1f,.35f,.08f),0,.5f,new Color(5f,1.25f,.18f));
   mats[(int)M.Brass]=Mat("Laton oscuro",new Color(.42f,.3f,.14f),.9f,.5f);
   var smr=host.AddComponent<SkinnedMeshRenderer>();smr.sharedMesh=mesh;smr.bones=bones;smr.rootBone=bones.First(b=>b.name=="B-hips");smr.sharedMaterials=mats;smr.updateWhenOffscreen=false;smr.quality=SkinQuality.Bone4;
   smr.localBounds=new Bounds(smr.rootBone.InverseTransformPoint(new Vector3(0,.9f,0)),new Vector3(2f,2.2f,1.2f));
   // scarf tails: dynamic ribbons from the throat wrap
   var neck=bones.First(b=>b.name=="B-neck");
   for(int tail=0;tail<2;tail++){var go=new GameObject(Tag+"Cola de bufanda "+tail);go.transform.SetParent(neck,false);go.transform.position=neck.position;var line=go.AddComponent<LineRenderer>();line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Presentation/Classes/Couture/"+(tail==0?"Seda carmesi":"Seda granate")+".mat")??mats[(int)M.Silk];line.positionCount=0;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
    var rib=go.AddComponent<Codice.Presentation.ScarfRibbon>();rib.body=root.transform;rib.anchorOffset=new Vector3(-.06f+tail*.06f,-.015f,-.11f);rib.length=tail==0?.95f:.7f;rib.width=tail==0?.085f:.07f;rib.sway=tail==0?1:.75f;}
   PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
   return $"forged verts={V.Count} tris={T.Sum(t=>t.Count)/3} submeshes={T.Count(t=>t.Count>0)} removed={removed} bones={bones.Length}";
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}
