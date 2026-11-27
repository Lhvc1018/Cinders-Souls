from pathlib import Path
p=Path('Assets/02_Scripts/Expedition/ExpeditionFloor.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('if(!ready||Finished||InEncounter||','if(ready&&InEncounter&&Codice.Mechanics.JamDebug.FreeRoam&&PlayerRoom>=0&&PlayerRoom!=CurrentRoom)Codice.Mechanics.JamDebug.ClearEncounter();\n            if(!ready||Finished||InEncounter||');s=s.replace('FloorNumber>=4?WorldTwoName+','FloorNumber>=7?CemeteryName+" · Sala "+(room+1):FloorNumber>=4?WorldTwoName+');s=s.replace('WorldTwoLighting(root);','WorldTwoLighting(root);\n            if(FloorNumber>=7)CemeteryAtmosphere(root);');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Expedition/ExpeditionLayout.cs');s=p.read_text(encoding='utf-8-sig').replace('bool secret=beforeBoss>6 && random.Next(3)!=0;','bool secret=floor>=7 || beforeBoss>6 && random.Next(3)!=0;').replace('int spread=random.Next(12,17),step=random.Next(12,17);','int spread=floor>=7?11:random.Next(12,17),step=floor>=7?12:random.Next(12,17);').replace('plan.width=5;plan.depth=4+(i%2);plan.shape=ChamberShape.CutCorners;plan.pillars.Clear();','plan.width=4+(i%2);plan.depth=3+(i%3);plan.shape=i%2==0?ChamberShape.Rectangle:ChamberShape.CutCorners;plan.pillars.Clear();plan.traps.Clear();').replace('if(i==Boss&&(GiantBoss(seed,floor)||floor==9))positions[i]+=Vector2Int.up*14;','if(i==Boss&&GiantBoss(seed,floor))positions[i]+=Vector2Int.up*14;\n                else if(i==Boss&&floor==9)positions[i]+=Vector2Int.up*3;');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/Expedition/ExpeditionFloor.Contents.cs');s=p.read_text(encoding='utf-8-sig').replace('if(index>0&&index!=Layout.Boss','if(FloorNumber<7&&index>0&&index!=Layout.Boss').replace('for(int side=-1;side<=1;side+=2)\n            {\n                int x=plan.width','for(int side=-1;FloorNumber<7&&side<=1;side+=2)\n            {\n                int x=plan.width');p.write_text(s,encoding='utf-8')
p=Path('AgentScripts/BuildGothicArchitecture.cs');s=p.read_text(encoding='utf-8-sig');at=s.index('    static void Make(');s=s[:at]+'''    public static string CemeteryExpansion(){
        Make("BurialWall",Dungeon+"wall_cracked.fbx",4,6,.7f,true);
        Make("BurialCrypt",Grave+"crypt.fbx",6,6,5,true);
        Make("BurialBench",Grave+"bench_decorated.fbx",2.4f,1.2f,1,false);
        Make("BurialLantern",Grave+"post_lantern.fbx",.7f,3.3f,.7f,false);
        Make("BurialCoffin",Grave+"coffin.fbx",1.1f,.65f,2.5f,true);
        Make("BurialTree",Grave+"tree_dead_medium.fbx",4,7,4,false);
        Make("BurialCross",Grave+"gravemarker_B.fbx",1.2f,2,1,false);
        Make("BurialPath",Grave+"path_A.fbx",4,.18f,4,true,true);
        Make("BurialGate",Grave+"arch_gate.fbx",4,5.5f,.6f,false);
        AssetDatabase.SaveAssets();return "Nine cemetery modules imported";
    }
''' + s[at:];p.write_text(s,encoding='utf-8')
