using UnityEngine;
namespace Codice.Expedition {
 public sealed partial class ExpeditionFloor {
  Material cemeterySky;
  string CemeteryName=>FloorNumber==7?"Campos del Último Suspiro":FloorNumber==8?"Jardín de los Sepultados":"Trono de las Mil Tumbas";
  void CemeteryBoundary(Vector3 p,int side,Transform root){
   Module("BurialWall",p,side*90,root);
   if((Mathf.RoundToInt(p.x+p.z)&7)==0){var l=Module("BurialLantern",p,side*90,root);Glow(l.transform.position+Vector3.up*3,new Color(.85f,.57f,.3f),2.6f,10,root);}
  }
  void CemeteryAtmosphere(Transform root){
   var skyShader=Resources.Load<Shader>("GothicArchitecture/CemeterySky");
   if(skyShader){cemeterySky=new Material(skyShader);RenderSettings.skybox=cemeterySky;}
   var fog=new Color(.085f,.105f,.12f);RenderSettings.fogColor=fog;RenderSettings.fogDensity=.021f;
   foreach(var cam in context.player.GetComponentsInChildren<Camera>(true)){if(cam.clearFlags!=CameraClearFlags.Depth&&cam.clearFlags!=CameraClearFlags.Nothing){cam.backgroundColor=fog;cam.clearFlags=CameraClearFlags.Skybox;}}
   context.player.view.backgroundColor=fog;context.player.view.clearFlags=CameraClearFlags.Skybox;
  }
  void CemeteryRoom(int room,Transform root){
   var c=Center(room);var plan=Layout.Plans[room];bool boss=room==Layout.Boss;
   var rng=new System.Random(Seed^room*2377);
   if(plan.width>=3&&plan.depth>=3){
    foreach(int side in new[]{-1,1}){
     var approach=c+new Vector3(side*6,0,-(plan.depth*4-3));
     Module("BrokenPillar",approach,0,root);Glow(approach+Vector3.up*2.5f,new Color(.7f,.62f,.43f),2.8f,11,root);
    }
    // Stone processional path defines the route through the burial courts.
    for(int z=-plan.depth+1;z<plan.depth;z++)Module("BurialPath",c+new Vector3(0,-.17f,z*4),0,root);
   }
   for(int x=-plan.width+1;x<plan.width;x++)for(int z=-plan.depth+1;z<plan.depth;z++){
    if(!plan.Contains(x,z)||Mathf.Abs(x)<=1||Mathf.Abs(z)<=1)continue;
    if(boss&&Mathf.Abs(x)<5&&Mathf.Abs(z)<5)continue;
    if(!boss&&plan.width>=3&&x>=plan.width-2)continue;
    if(rng.NextDouble()>.52)continue;var pos=c+new Vector3(x*4,0,z*4);
    Module(rng.Next(3)==0?"GraveBroken":"Grave",pos,rng.Next(-14,15),root);
    if(rng.Next(6)==0)Module("BurialTree",pos+Vector3.right*1.3f,0,root);
   }
   if(room==Layout.SecretRoom){
    Module("BurialCoffin",c+new Vector3(-4,0,3),90,root);Module("BurialCoffin",c+new Vector3(4,0,3),-90,root);
    Module("BurialGate",c+Vector3.forward*6,180,root);Glow(c+Vector3.up*3,new Color(.45f,.6f,.8f),3,12,root);
   }
   if(plan.width>=4&&plan.depth>=3&&!boss){
    // Full-depth mausoleums form burial courts, not flattened facades. Central axes stay clear.
    float edge=(plan.width-1)*4;
    for(int side=-1;side<=1;side+=2){
     var crypt=c+new Vector3(-edge,0,side*(plan.depth*4-4));Module("BurialCrypt",crypt,90,root);
     Module("BurialBench",crypt+Vector3.right*4,90,root);
     Module("BurialCross",c+new Vector3(-edge+5,0,side*6),0,root);
    }
    if(room%2==0){for(int z=-1;z<=1;z++){if(z==0)continue;Module("CemeteryFence",c+new Vector3(-5,0,z*4),90,root);}}
   }
   if(room==Layout.StatueRoom||room==Layout.MerchantRoom)return;
   if(!boss&&plan.width>=3&&plan.depth>=3&&plan.shape!=ChamberShape.Cross&&!GalleryBlocksDoor(room,1)){
    var gallery=c+Vector3.right*(plan.width*4-3f);BuildGallery(gallery,root);
    var marker=Module("Monument",gallery+new Vector3(-1.1f,1.6f,0),180,root);marker.name="Mausoleo de la galería";
    Glow(gallery+Vector3.up*3,new Color(.35f,.7f,.55f),2.5f,10,root);
   }
   if(boss&&FloorNumber==9){
    foreach(int side in new[]{-1,1})foreach(int row in new[]{-1,1}){
     var tomb=c+new Vector3(side*21,0,row*18);Module("BurialCrypt",tomb,side>0?-90:90,root);
     Module("BurialTree",tomb+Vector3.right*side*5,0,root);Module("BurialBench",tomb-Vector3.right*side*4,90,root);
    }
    for(int n=0;n<12;n++){float angle=n*30*Mathf.Deg2Rad;var p=c+new Vector3(Mathf.Sin(angle)*25,0,Mathf.Cos(angle)*25);Module("Monument",p,-n*30,root);Glow(p+Vector3.up*3,new Color(.22f,.85f,.58f),3,10,root);}
    Module("Arcade",c+Vector3.forward*28,0,root).transform.localScale=new Vector3(4,2.8f,2);
    Module("Ossuary",c+Vector3.forward*29,0,root);
    // Central combat disc stays flat and clear; the processional steps remain peripheral.
    for(int i=0;i<6;i++){var step=Module("Paving",c+new Vector3(0,i*.18f,24+i*.8f),0,root);step.transform.localScale=new Vector3(3,.5f,.3f);}
   }else{Module("Shrine",c+new Vector3((plan.width-1)*4,0,(plan.depth-1)*4),180,root);}
  }
 }
}
