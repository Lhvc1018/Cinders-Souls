from pathlib import Path
p=Path('D:/67/Last67Seconds-Maps/AgentScripts/BuildClassModels.cs');s=p.read_text(encoding='utf-8-sig');needle='   rig.enabled=true;rig.runtimeAnimatorController='
addition='''   foreach(bool left in new[]{true,false}){var arm=left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm;var fore=left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm;var thigh=left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg;var shin=left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg;string side=left?"L":"R";
    Shell("Manga "+side,arm,Vector3.Lerp(Pos(arm),Pos(fore),-.10f),Vector3.Lerp(Pos(arm),Pos(fore),.72f),heavy?.14f:.115f,.085f,.95f,heavy?steel:cloth);
    Plate("Rodillera "+side,shin,Pos(shin)+new Vector3(0,.01f,.09f),new Vector3(.13f,.14f,.05f),heavy?steel:dark);
    if(role==PlayerClass.Mage)Plate("Tunica frontal "+side,thigh,Vector3.Lerp(Pos(thigh),Pos(shin),.5f)+new Vector3(0,-.04f,.14f),new Vector3(.22f,.52f,.025f),cloth);
   }
   if(role==PlayerClass.Warrior||role==PlayerClass.Tank)Plate("Tabardo",HumanBodyBones.Hips,Pos(HumanBodyBones.Hips)+new Vector3(0,-.08f,.19f),new Vector3(.20f,.34f,.025f),cloth);
   if(role==PlayerClass.Archer){var back=Pos(HumanBodyBones.Chest)+new Vector3(.16f,.05f,-.22f);Shell("Carcaj",HumanBodyBones.Chest,back-Vector3.up*.28f,back+Vector3.up*.25f,.07f,.085f,1,dark);for(int i=0;i<4;i++){var top=back+new Vector3((i-1.5f)*.025f,.38f,0);Shell("Flecha "+i,HumanBodyBones.Chest,top-Vector3.up*.25f,top,.008f,.008f,1,trim,5);Plate("Pluma "+i,HumanBodyBones.Chest,top,new Vector3(.035f,.08f,.01f),cloth);}}
'''
assert needle in s;s=s.replace(needle,addition+needle);p.write_text(s,encoding='utf-8')
