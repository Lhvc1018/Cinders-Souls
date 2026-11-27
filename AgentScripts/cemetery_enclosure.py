from pathlib import Path
p=Path('Assets/02_Scripts/Expedition/ExpeditionFloor.Cemetery.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('void CemeteryBoundary(Vector3 p,int side,Transform root){Module("CemeteryFence",p,side*90,root);if((Mathf.RoundToInt(p.x+p.z)&7)==0)Module("BrokenPillar",p,side*90,root);}', '''void CemeteryBoundary(Vector3 p,int side,Transform root){
   Module("BurialWall",p,side*90,root);
   if((Mathf.RoundToInt(p.x+p.z)&7)==0){var l=Module("BurialLantern",p,side*90,root);Glow(l.transform.position+Vector3.up*3,new Color(.85f,.57f,.3f),2.6f,10,root);}
  }
  void CemeteryAtmosphere(Transform root){
   var fog=new Color(.085f,.105f,.12f);RenderSettings.fogColor=fog;RenderSettings.fogDensity=.021f;
   foreach(var cam in context.player.GetComponentsInChildren<Camera>(true)){if(cam.clearFlags!=CameraClearFlags.Depth&&cam.clearFlags!=CameraClearFlags.Nothing){cam.backgroundColor=fog;cam.clearFlags=CameraClearFlags.SolidColor;}}
   context.player.view.backgroundColor=fog;
  }''');s=s.replace('Module("Paving",c+new Vector3(0,-.3f,z*4)','Module("BurialPath",c+new Vector3(0,-.17f,z*4)');s=s.replace('Module("BareTree",pos+Vector3.right*1.3f','Module("BurialTree",pos+Vector3.right*1.3f');s=s.replace('if(room==Layout.StatueRoom||room==Layout.MerchantRoom)return;', '''if(room==Layout.SecretRoom){
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
   if(room==Layout.StatueRoom||room==Layout.MerchantRoom)return;''');s=s.replace('for(int n=0;n<12;n++)', '''foreach(int side in new[]{-1,1})foreach(int row in new[]{-1,1}){
     var tomb=c+new Vector3(side*21,0,row*18);Module("BurialCrypt",tomb,side>0?-90:90,root);
     Module("BurialTree",tomb+Vector3.right*side*5,0,root);Module("BurialBench",tomb-Vector3.right*side*4,90,root);
    }
    for(int n=0;n<12;n++)''');p.write_text(s,encoding='utf-8')
