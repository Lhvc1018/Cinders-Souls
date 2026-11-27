using System.Collections.Generic;
using UnityEngine;

namespace Codice.Expedition
{
    public sealed partial class ExpeditionFloor
    {
        readonly Dictionary<string,GameObject> architecture=new Dictionary<string,GameObject>();
        string WorldTwoName=>FloorNumber==4?"Cisternas del Osario":FloorNumber==5?"Ciudad de los Sepultados":"Catedral del Fondo";
        GameObject Module(string name,Vector3 position,float yaw,Transform parent)
        {
            if(!architecture.TryGetValue(name,out var asset)){
                asset=Resources.Load<GameObject>("GothicArchitecture/"+name);
                if(!asset)throw new System.InvalidOperationException("Missing architecture: "+name);
                architecture.Add(name,asset);
            }
            return Instantiate(asset,position,Quaternion.Euler(0,yaw,0),parent);
        }
        void WorldTwoBoundary(Vector3 position,int side,Transform parent)
        {
            float yaw=side*90;
            if(FloorNumber==4){
                Module("Arcade",position,yaw,parent);
                Module("Tracery",position+Quaternion.Euler(0,yaw,0)*Vector3.forward*.48f,yaw,parent);
            }else if(FloorNumber==5){
                bool scaffold=((Mathf.RoundToInt(position.x/4)+Mathf.RoundToInt(position.z/4)+Seed)&3)==0;
                Module(scaffold?"ScaffoldWindow":"Facade",position,yaw,parent);
            }else{
                Module("Arcade",position,yaw,parent);
                Module("Arcade",position+Vector3.up*5,yaw,parent);
                Module("Tracery",position+Vector3.up*5,yaw,parent);
            }
        }
        void WorldTwoRoom(int room,Transform parent)
        {
            var plan=Layout.Plans[room];var center=Center(room);
            if(room==Layout.SecretRoom){
                Module("VotiveShelf",center+new Vector3(-5,0,4),90,parent);
                Module("WakeTable",center+new Vector3(4,0,4),-25,parent);
                Glow(center+new Vector3(-4,2,4),new Color(1,.59f,.24f),2,7,parent);
            }
            if(room==0||room==Layout.Boss||(plan.shape!=ChamberShape.Cross&&plan.depth>=3&&room!=Layout.StatueRoom&&room!=Layout.MerchantRoom&&room!=Layout.SecretRoom)){
                int side=((Seed^room)&1)==0?-1:1;
                if(GalleryBlocksDoor(room,side))side=-side;
                if(GalleryBlocksDoor(room,side))side=0;
                if(side!=0){
                var deck=center+Vector3.right*(side*(plan.width*4-.5f));
                BuildGallery(deck,parent);
                }
            }
            if(FloorNumber==6 && plan.width>=2){
                for(int bay=-1;bay<=1;bay++){
                    var rib=Module("Arcade",center+Vector3.forward*(bay*plan.depth*2.2f),0,parent);
                    rib.name="Bóveda de la nave";
                    rib.transform.localScale=new Vector3(plan.width*1.85f,2.05f,1);
                    rib.GetComponent<BoxCollider>().enabled=false;
                }
            }
            // Keep axial doors, merchant stock and boss approaches clear.
            if(plan.width<2||plan.depth<2)return;
            foreach(int side in new[]{-1,1}){
                var corner=new Vector2Int(side*(plan.width-1),plan.depth-1);
                if(!plan.Contains(corner.x,corner.y))continue;
                var p=center+new Vector3(corner.x*4,0,corner.y*4);
                string prop=FloorNumber==4?"Ossuary":FloorNumber==5?"BareTree":room==Layout.Boss?"Monument":"Shrine";
                Module(prop,p,-side*30,parent);
                if(FloorNumber==4||FloorNumber==6)Module("BrokenPillar",p-Vector3.forward*3,0,parent);
            }
        }
        // World 2 readability pass. The Trilight ambient set at runtime never reached the ambient probe
        // (measured 0.04), so these floors rendered almost black. A custom probe is written directly.
        void WorldTwoLighting(Transform root)
        {
            int lightingStage=FloorNumber<=3?FloorNumber+3:FloorNumber>=7?4:FloorNumber;
            // sky, horizon, ground, fog, room fill, moon   (4 cisterns: cold green-blue · 5 city: ember dusk · 6 cathedral: lunar violet)
            Color sky=lightingStage==4?new Color(.30f,.40f,.44f):lightingStage==5?new Color(.42f,.36f,.34f):new Color(.36f,.36f,.50f);
            Color horizon=lightingStage==4?new Color(.20f,.27f,.29f):lightingStage==5?new Color(.30f,.24f,.21f):new Color(.24f,.23f,.33f);
            Color fog=lightingStage==4?new Color(.035f,.055f,.06f):lightingStage==5?new Color(.065f,.045f,.04f):new Color(.045f,.04f,.07f);
            Color fill=lightingStage==4?new Color(.55f,.78f,.82f):lightingStage==5?new Color(1f,.70f,.48f):new Color(.70f,.68f,1f);
            Color moon=lightingStage==4?new Color(.62f,.80f,.90f):lightingStage==5?new Color(1f,.78f,.62f):new Color(.74f,.76f,1f);
            var sh=new UnityEngine.Rendering.SphericalHarmonicsL2();
            sh.AddAmbientLight(horizon);sh.AddDirectionalLight(Vector3.up,sky,.55f);sh.AddDirectionalLight(Vector3.down,horizon*.5f,.25f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Custom;RenderSettings.ambientProbe=sh;RenderSettings.ambientIntensity=1;
            RenderSettings.fogColor=fog;RenderSettings.fogDensity=.014f;context.player.view.backgroundColor=fog;
            // One shadowed key gives bodies and architecture a readable form; attack telegraphs stay brighter than it.
            var key=new GameObject("World two moonlight").AddComponent<Light>();key.transform.SetParent(root);
            key.type=LightType.Directional;key.color=moon;key.intensity=.55f;key.shadows=LightShadows.Soft;key.shadowStrength=.7f;
            key.transform.rotation=Quaternion.Euler(52,lightingStage==5?-35:28,0);
            // Fill lights on a grid sized to each chamber, so the giant arena is lit like the small rooms.
            for(int room=0;room<RoomCount;room++){
                var plan=Layout.Plans[room];var c=Center(room);float w=plan.width*4,d=plan.depth*4;
                int nx=Mathf.Max(1,Mathf.RoundToInt(w*2/14f)),nz=Mathf.Max(1,Mathf.RoundToInt(d*2/14f));
                for(int x=0;x<nx;x++)for(int z=0;z<nz;z++){
                    var p=c+new Vector3((x+.5f)/nx*2*w-w,5.5f,(z+.5f)/nz*2*d-d);
                    Glow(p,room==Layout.Boss?Color.Lerp(fill,new Color(1,.6f,.4f),.35f):fill,room==Layout.Boss?4.2f:3.4f,17,root).name="World two room fill";
                }
            }
            if(profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var grade)){grade.postExposure.value=.75f;grade.saturation.value=-14;grade.contrast.value=8;}
            if(profile.TryGet<UnityEngine.Rendering.Universal.Vignette>(out var vignette))vignette.intensity.value=.18f;
            if(profile.TryGet<UnityEngine.Rendering.Universal.WhiteBalance>(out var balance))balance.temperature.value=lightingStage==5?4:-5;
        }
        // The raised deck hugs one side wall; never put it across a doorway (a hidden vault door counts too).
        bool GalleryBlocksDoor(int room,int side)
        {
            var c=Center(room);
            bool Toward(int other){if(other<0)return false;var d=Center(other)-c;return Mathf.Abs(d.x)>Mathf.Abs(d.z)&&Mathf.Sign(d.x)==side&&Mathf.Abs(d.z)<6;}
            foreach(var link in Layout.Links){if(link.x==room&&Toward(link.y))return true;if(link.y==room&&Toward(link.x))return true;}
            return room==Layout.VaultHost&&Toward(Layout.VaultRoom);
        }
        void BuildGallery(Vector3 center,Transform parent)
        {
            const float rise=1.6f;
            string surface=FloorNumber>=6?"Paving":"TimberDeck";
            float thickness=surface=="Paving"?.35f:.25f;
            var deck=Module(surface,center+Vector3.up*(rise-thickness),0,parent);
            deck.name="Galería transitable";
            for(int side=-1;side<=1;side+=2){
                // Eight broad treads climb 1.6 metres over four metres; both ends meet the existing floor.
                for(int i=0;i<8;i++){
                    float height=rise*(i+1)/8f;
                    float z=side*(6-(i+.5f)*.5f);
                    var step=Module(surface,center+Vector3.forward*z,0,parent);
                    step.transform.localScale=new Vector3(1,height/thickness,.125f);
                }
            }
            var landmark=center+new Vector3(-1.15f,rise,0);
            if(FloorNumber==4)Module("DockCargo",landmark,0,parent);
            else if(FloorNumber==5)Module("WakeTable",landmark,90,parent);
            else Module("ChoirBanner",landmark+Vector3.up*1.8f,90,parent);
            Glow(center+Vector3.up*3.8f,new Color(.85f,.62f,.38f),2,9,parent);
        }
    }
}

