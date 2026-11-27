using System.Linq;using UnityEngine;using Codice.Enemies;
namespace Codice.Expedition {
 public sealed partial class ExpeditionFloor {
  string WorldFourName=>FloorNumber==10?"Fundición de los Juramentos":FloorNumber==11?"Archivo de los Nombres Quemados":"Campanario del Último Alba";
  GameObject Forge(string name,Vector3 position,float yaw,Transform parent){var asset=Resources.Load<GameObject>("WorldFour/"+name);if(!asset)throw new System.InvalidOperationException("Missing WorldFour asset: "+name);return Instantiate(asset,position,Quaternion.Euler(0,yaw,0),parent);}
  void WorldFourBoundary(Vector3 p,int side,Transform root){Forge("Wall",p,side*90,root);if(((Mathf.RoundToInt(p.x+p.z)+Seed)&15)==0){var inner=Quaternion.Euler(0,side*90,0)*Vector3.back*.8f;Forge("Lamp",p+inner,0,root);Glow(p+inner+Vector3.up*2.8f,FloorNumber==11?new Color(.3f,.65f,.9f):new Color(1,.45f,.15f),3,10,root);}}
  void WorldFourRoom(int room,Transform root){var c=Center(room);var p=Layout.Plans[room];var rng=new System.Random(Seed^room*713);bool boss=room==Layout.Boss;
   // Keep cardinal door axes and the combat centre clear. Every room has distinct perimeter workstations.
   int type=room==Layout.SecretRoom?1:(room+FloorNumber)%3;
   string prop=type==0?"Furnace":type==1?"Archive":"Anvil";
   if(p.width>=3&&p.depth>=3){foreach(int side in new[]{-1,1})foreach(int row in new[]{-1,1}){
    var spot=c+new Vector3(side*(p.width*4-4),0,row*(p.depth*4-4));Forge(prop,spot,side>0?90:-90,root);
    if(type==0)Glow(spot+Vector3.up*2,new Color(1,.32f,.06f),4,10,root);
    else if(type==1)Glow(spot+Vector3.up*3,new Color(.2f,.55f,.9f),3,10,root);
   }}
   if(room==0){Forge("SealWheel",c+new Vector3(-5,0,3),90,root);Forge("Anvil",c+new Vector3(5,0,3),0,root);}
   if(room==Layout.SecretRoom||IsSideRoom(room)){Forge("Archive",c+new Vector3(-5,0,0),90,root);Forge("SealWheel",c+new Vector3(0,0,p.depth*4-1),180,root);}
   if(room==Layout.MerchantRoom||room==Layout.StatueRoom)return;
   if(p.width>=3){for(int row=-1;row<=1;row+=2){var a=c+new Vector3(0,0,row*(p.depth*4-2));Forge("RibArch",a,0,root);}}
   if(boss||FloorNumber==12){int count=boss?6:2;for(int i=0;i<count;i++){float angle=(i+.5f)*Mathf.PI*2/count;float radius=boss?Mathf.Min(p.width,p.depth)*4-6:8;var at=c+new Vector3(Mathf.Cos(angle)*radius,7.2f,Mathf.Sin(angle)*radius);Forge("Bell",at,i*35,root);Forge("Chain",at+Vector3.up*3.1f,0,root);
    var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);beam.name="Viga de suspensión";beam.transform.SetParent(root);beam.transform.position=new Vector3(c.x,c.y+13.8f,at.z);beam.transform.localScale=new Vector3(p.width*8+4,.35f,.4f);beam.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("WorldFour/Hierro azulado");Destroy(beam.GetComponent<Collider>());}}
   if(FloorNumber==11){foreach(int side in new[]{-1,1})for(int n=-1;n<=1;n++)Forge("Pipe",c+new Vector3(side*(p.width*4-.9f),4.7f,n*4),0,root);}
   if(boss){Forge("SealWheel",c+new Vector3(0,1,p.depth*4-2),180,root);Glow(c+Vector3.up*7,new Color(.7f,.6f,.95f),5,28,root);}
  }
  void WorldFourAtmosphere(Transform root){CemeteryAtmosphere(root);RenderSettings.fogColor=FloorNumber==11?new Color(.055f,.085f,.13f):new Color(.11f,.065f,.045f);RenderSettings.fogDensity=.018f;foreach(var light in root.GetComponentsInChildren<Light>())if(light.type==LightType.Directional){light.color=new Color(.65f,.71f,.86f);light.intensity=.65f;}
   if(profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var grade)){grade.saturation.value=-12;grade.postExposure.value=.8f;}
  }
  BasicEnemy[] WorldFourRoster(int room){string[] pool=FloorNumber==10?new[]{"Executioner","Watcher","StrawReaper"}:FloorNumber==11?new[]{"Watcher","StrawReaper","Executioner"}:new[]{"Executioner","Watcher","StrawReaper","Watcher"};return pool.Select(n=>Resources.Load<BasicEnemy>("WorldFour/Enemies/"+n)).Where(e=>e).ToArray();}
 }
}
