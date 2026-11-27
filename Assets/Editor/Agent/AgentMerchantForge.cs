using System;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;
// "Nereo el Tasador": a Shinobi body forged from scratch as ONE skinned mesh on the class rig.
// Every vertex is generated here (lofts, shells, curved plates, tattered cloth) and weighted to the
// humanoid bones with smooth key blends, so cloth and plates follow the animation instead of being glued props.
public static class AgentMerchantForge{
 const string Prefab="Assets/Resources/WorldProgression/Nereo.prefab";
 const string Dir="Assets/Resources/WorldProgression/NereoForge/";
 const string Legacy="Assets/Presentation/Legacy/Merchant_Legado.prefab";
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
  // Heavy travel coat: continuous pleated cloth, weighted from hips to chest.
  var rings=new List<Ring>();
  for(int i=0;i<16;i++){float y=Mathf.Lerp(.12f,1.48f,i/15f);float r=Mathf.Lerp(.34f,.2f,i/15f);rings.Add(new Ring(new Vector3(0,y,-.015f),Vector3.right,Vector3.forward,r,r*.72f));}
  Emit(Loft(rings,40,false,false,(k,a)=>1+.065f*Mathf.Cos(a*12)),M.Leather,p=>TorsoW(Mathf.Max(.88f,p.y)));
  // Broad weathered shoulder cape and deep hood; the face is a sealed brass death-mask.
  Emit(Shell(new Vector3(.34f,.22f,.25f),0,360,0,85,32,8).Xf(TRS(new Vector3(0,1.39f,0),Vector3.zero)),M.Cloth,"B-spine");
  Emit(Shell(new Vector3(.145f,.2f,.16f),35,325,-65,90,32,18).TwoSided().Xf(TRS(new Vector3(0,1.72f,.015f),Vector3.zero)),M.Cloth,"B-head");
  Emit(Shell(new Vector3(.092f,.13f,.115f),-70,70,-70,75,20,12).Xf(TRS(new Vector3(0,1.70f,.055f),Vector3.zero)),M.Brass,"B-head");
  foreach(int side in new[]{-1,1}){
   Emit(Box(new Vector3(.04f,.012f,.008f)).Xf(TRS(new Vector3(side*.043f,1.733f,.166f),new Vector3(0,0,-side*9))),M.Cloth,"B-head");
   Emit(Box(new Vector3(.024f,.005f,.008f)).Xf(TRS(new Vector3(side*.043f,1.734f,.171f),Vector3.zero)),M.Ember,"B-head");
  }
  Emit(Plate(.225f,.17f,0,180,.96f,1.025f,.02f,32),M.Iron,"B-hips");
  Emit(Box(new Vector3(.10f,.09f,.025f)).Xf(TRS(new Vector3(0,1,.205f),Vector3.zero)),M.Brass,"B-hips");
  // Asymmetrical ledger case, pilgrim pack, rolled blanket, coin purses and votive seals.
  Emit(Box(new Vector3(.40f,.48f,.24f)).Xf(TRS(new Vector3(0,1.32f,-.28f),new Vector3(8,0,0))),M.Leather,"B-spine");
  Emit(Loft(new[]{new Ring(new Vector3(-.27f,1.60f,-.27f),Vector3.up,Vector3.forward,.10f,.10f),new Ring(new Vector3(.27f,1.60f,-.27f),Vector3.up,Vector3.forward,.10f,.10f)},20,true,true),M.Linen,"B-spine");
  for(int i=0;i<3;i++){Emit(Box(new Vector3(.11f,.24f,.15f)).Xf(TRS(new Vector3(-.13f+i*.13f,1.18f,-.42f),Vector3.zero)),i==1?M.Silk:M.Cloth,"B-spine");}
  for(int i=0;i<5;i++){float x=-.23f+i*.115f;Emit(Sphere(new Vector3(.052f,.07f,.045f)).Xf(TRS(new Vector3(x,.89f,.18f),Vector3.zero)),M.Leather,"B-hips");Emit(Torus(.023f,.006f).Xf(TRS(new Vector3(x,.98f,.205f),new Vector3(90,0,0))),M.Brass,"B-hips");}
  for(int i=0;i<9;i++){float y=1.1f+i*.033f;Emit(Torus(.009f,.003f,10,5).Xf(TRS(new Vector3(.13f,y,.19f),new Vector3(90,i%2*90,0))),M.Brass,p=>TorsoW(p.y));}
  Emit(Box(new Vector3(.11f,.17f,.028f)).Xf(TRS(new Vector3(-.13f,1.21f,.21f),new Vector3(0,0,-12))),M.Linen,"B-spine");
 }
 static string NameOf(int i)=>bones[i].name;
 // ---------------------------------------------------------------- entry
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";
  Directory.CreateDirectory(Dir);Directory.CreateDirectory(Path.GetDirectoryName(Legacy));AssetDatabase.Refresh();
  if(!File.Exists(Prefab))AssetDatabase.CopyAsset("Assets/Presentation/Legacy/Shinobi_Legado.prefab",Prefab);
  var root=PrefabUtility.LoadPrefabContents(Prefab);
  try{
   // strip every old visual (skinned body, Identity armour, Couture pieces, previous forge)
   int removed=0;foreach(var r in root.GetComponentsInChildren<Renderer>(true).ToArray()){if(!r)continue;var go=r.gameObject;if(go.name.StartsWith("B-")||go.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("B-")))continue;UnityEngine.Object.DestroyImmediate(go);removed++;}
   foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t&&(t.name.StartsWith("Couture · ")||t.name.StartsWith(Tag))).ToArray())if(t){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;}
   bones=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("B-")).ToArray();boneIndex=new Dictionary<string,int>();for(int i=0;i<bones.Length;i++)boneIndex[bones[i].name]=i;
   V=new List<Vector3>();UV=new List<Vector2>();W=new List<BoneWeight>();T=new List<int>[(int)M.Count];for(int i=0;i<T.Length;i++)T[i]=new List<int>();
   Body();Garments();
   var meshPath=Dir+"Nereo el Tasador.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);bool fresh=!mesh;if(fresh)mesh=new Mesh();mesh.Clear();mesh.name="Mercader · Nereo el Tasador";mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(V);mesh.SetUVs(0,UV);mesh.subMeshCount=(int)M.Count;for(int i=0;i<T.Length;i++)mesh.SetTriangles(T[i],i);mesh.boneWeights=W.ToArray();
   var host=new GameObject(Tag+"Nereo el Tasador");host.transform.SetParent(root.transform,false);host.transform.SetSiblingIndex(1);
   mesh.bindposes=bones.Select(b=>b.worldToLocalMatrix*host.transform.localToWorldMatrix).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
   if(fresh)AssetDatabase.CreateAsset(mesh,meshPath);else EditorUtility.SetDirty(mesh);
   var mats=new Material[(int)M.Count];
   mats[(int)M.Cloth]=Mat("Paño ceniza",new Color(.085f,.072f,.066f),0,.12f);
   mats[(int)M.Lacquer]=Mat("Laca obsidiana",new Color(.035f,.03f,.034f),.35f,.8f);
   mats[(int)M.Iron]=Mat("Hierro quemado",new Color(.17f,.13f,.11f),.85f,.36f);
   mats[(int)M.Leather]=Mat("Cuero ahumado",new Color(.15f,.095f,.065f),0,.32f);
   mats[(int)M.Linen]=Mat("Vendas de ceniza",new Color(.30f,.26f,.22f),0,.08f);
   mats[(int)M.Silk]=Mat("Seda de sangre",new Color(.32f,.03f,.045f),0,.38f);
   mats[(int)M.Ember]=Mat("Brasa viva",new Color(1f,.35f,.08f),0,.5f,new Color(.6f,.23f,.05f));
   mats[(int)M.Brass]=Mat("Laton oscuro",new Color(.42f,.3f,.14f),.9f,.5f);
   var smr=host.AddComponent<SkinnedMeshRenderer>();smr.sharedMesh=mesh;smr.bones=bones;smr.rootBone=bones.First(b=>b.name=="B-hips");smr.sharedMaterials=mats;smr.updateWhenOffscreen=false;smr.quality=SkinQuality.Bone4;
   smr.localBounds=new Bounds(smr.rootBone.InverseTransformPoint(new Vector3(0,.9f,0)),new Vector3(2f,2.2f,1.2f));
   var animator=root.GetComponentInChildren<Animator>();animator.applyRootMotion=false;
   var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(Dir+"NereoIdle.controller")??UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(Dir+"NereoIdle.controller");
   foreach(var oldState in controller.layers[0].stateMachine.states)controller.layers[0].stateMachine.RemoveState(oldState.state);
   var source=Resources.Load<UnityEditor.Animations.AnimatorController>("Presentation/Human/ClassicHuman");
   var motion=AssetDatabase.LoadAllAssetsAtPath("Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx").OfType<AnimationClip>().First(x=>!x.name.StartsWith("__"));
   var state=controller.layers[0].stateMachine.AddState("Idle");state.motion=motion;state.speed=.65f;controller.layers[0].stateMachine.defaultState=state;animator.runtimeAnimatorController=controller;
   PrefabUtility.SaveAsPrefabAsset(root,Prefab);AssetDatabase.SaveAssets();
   return $"forged verts={V.Count} tris={T.Sum(t=>t.Count)/3} submeshes={T.Count(t=>t.Count>0)} removed={removed} bones={bones.Length}";
  }finally{PrefabUtility.UnloadPrefabContents(root);}
 }
}
