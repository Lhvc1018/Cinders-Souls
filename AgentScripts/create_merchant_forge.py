from pathlib import Path
src=Path('Assets/Editor/Agent/AgentShinobiForge.cs').read_text(encoding='utf-8-sig')
s=src.replace('AgentShinobiForge','AgentMerchantForge').replace('Assets/Resources/Presentation/Classes/Shinobi.prefab','Assets/Resources/WorldProgression/Nereo.prefab').replace('Assets/Resources/Presentation/Classes/ShinobiForja/','Assets/Resources/WorldProgression/NereoForge/').replace('Assets/Presentation/Legacy/Shinobi_Legado.prefab','Assets/Presentation/Legacy/Merchant_Legado.prefab').replace('Ceniza Silente','Nereo el Tasador').replace('Shinobi ·','Mercader ·')
a=s.index(' static void Garments()');b=s.index(' static string NameOf',a)
s=s[:a]+''' static void Garments(){
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
''' + s[b:]
s=s.replace('if(!File.Exists(Legacy))AssetDatabase.CopyAsset(Prefab,Legacy);','if(!File.Exists(Prefab))AssetDatabase.CopyAsset("Assets/Presentation/Legacy/Shinobi_Legado.prefab",Prefab);')
a=s.index('   // scarf tails:');b=s.index('   PrefabUtility.SaveAsPrefabAsset',a);s=s[:a]+'''   var animator=root.GetComponentInChildren<Animator>();animator.applyRootMotion=false;
   var controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(Dir+"NereoIdle.controller");
   var source=Resources.Load<UnityEditor.Animations.AnimatorController>("Presentation/Human/ClassicHuman");
   var motion=source.layers[0].stateMachine.states.First(x=>x.state.name=="SwordIdle").state.motion;
   var state=controller.layers[0].stateMachine.AddState("Idle");state.motion=motion;state.speed=.65f;controller.layers[0].stateMachine.defaultState=state;animator.runtimeAnimatorController=controller;
''' +s[b:]
s=s.replace('new Color(.10f,.07f,.055f)','new Color(.15f,.095f,.065f)').replace('new Color(5f,1.25f,.18f)','new Color(.6f,.23f,.05f)')
Path('Assets/Editor/Agent/AgentMerchantForge.cs').write_text(s,encoding='utf-8')
