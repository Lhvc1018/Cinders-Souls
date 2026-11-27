using System.IO;using System.Linq;using System.Text;using UnityEditor;using UnityEngine;
// Imports the KayKit dungeon pack into a runtime kit for World 3 (Resources/WorldThree/Kit), re-materialised
// in three grave-dark tints so the bright palette blends with the cemetery instead of reading as a fresh download.
public static class AgentWorldThreeKit{
 const string Src="Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
 const string Out="Assets/Resources/WorldThree/Kit/";
 static readonly string[] Solid={"pillar","pillar_decorated","column","wall_arched","wall_broken","wall_half","wall_pillar","wall_archedwindow_gated","rubble_large","barrier","barrier_column","barrier_half","table_long_broken","table_medium_broken","shelves","shelf_large","crates_stacked","barrel_large","barrel_small_stack","trunk_large_A","trunk_medium_B","keg_decorated","box_stacked","stairs_walled","stairs_wide"};
 static readonly string[] Loose={"candle_triple","candle_lit","candle_melted","candle_thin_lit","torch_mounted","torch_lit","banner_thin_red","banner_patternA_red","banner_triple_red","banner_shield_red","banner_patternC_brown","sword_shield_broken","rubble_half","bottle_A_green","bottle_C_brown","plate_stack","keyring_hanging","shelf_small_candles","floor_tile_small_broken_A","floor_tile_big_grate","floor_dirt_small_weeds","floor_tile_small_weeds_A","coin_stack_small","chest_gold"};
 public static string Run(){
  if(EditorApplication.isPlaying)return "stop play";Directory.CreateDirectory(Out);AssetDatabase.Refresh();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Models/StructuresItemsInMap/KayKit_Dungeon_Pack_1.1_FREE/Assets/textures/dungeon_texture.png");
  Material Mat(string name,Color tint,float smooth){var path=Out+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;}
  var bone=Mat("Kit · Piedra de osario",new Color(.50f,.47f,.44f),.12f);
  var moss=Mat("Kit · Piedra con musgo",new Color(.40f,.47f,.39f),.08f);
  var soot=Mat("Kit · Hollin funerario",new Color(.30f,.27f,.27f),.2f);
  var sb=new StringBuilder();int made=0;
  foreach(var name in Solid.Concat(Loose)){
   var src=AssetDatabase.LoadAssetAtPath<GameObject>(Src+name+".fbx");if(!src){sb.AppendLine("missing "+name);continue;}
   var go=(GameObject)PrefabUtility.InstantiatePrefab(src);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name=name;
   var mat=name.StartsWith("banner")||name.StartsWith("candle")||name.StartsWith("torch")||name.StartsWith("bottle")?null:name.Contains("rubble")||name.StartsWith("floor")||name.StartsWith("barrier")?moss:name.StartsWith("table")||name.StartsWith("shel")||name.StartsWith("crate")||name.StartsWith("barrel")||name.StartsWith("trunk")||name.StartsWith("keg")||name.StartsWith("box")?soot:bone;
   foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterials=Enumerable.Repeat(mat??Mat("Kit · Tela y cera",new Color(.62f,.52f,.50f),.25f),r.sharedMaterials.Length).ToArray();}
   foreach(var c in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
   if(Solid.Contains(name))foreach(var f in go.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)f.gameObject.AddComponent<BoxCollider>();
   var b=new Bounds(go.transform.position,Vector3.zero);foreach(var r in go.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);
   PrefabUtility.SaveAsPrefabAsset(go,Out+name+".prefab");Object.DestroyImmediate(go);made++;
   sb.AppendLine(name+" size="+b.size.ToString("F2")+" min="+b.min.ToString("F2"));
  }
  AssetDatabase.SaveAssets();return "kit="+made+"\n"+sb;
 }
}
