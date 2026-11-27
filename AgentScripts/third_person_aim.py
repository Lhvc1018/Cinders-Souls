from pathlib import Path
p=Path('Assets/02_Scripts/Presentation/HumanPreviewCamera.cs');s=p.read_text(encoding='utf-8').replace('  void Start(){','  public bool ThirdPerson=>preview;\n  public Vector3 CombatOrigin=>transform.TransformPoint(cameraHome);\n  void Start(){').replace('cameraHome=player.view.transform.localPosition;}','cameraHome=player.view.transform.localPosition;preview=PlayerPrefs.GetInt("Cinders.ThirdPerson",0)==1;Apply();}').replace('public void Toggle(){preview=!preview;Apply();}','public void Toggle(){preview=!preview;PlayerPrefs.SetInt("Cinders.ThirdPerson",preview?1:0);Apply();}')
s=s.replace('weapon=Instantiate(model,presentation.Body.GetBoneTransform(HumanBodyBones.RightHand),false);','weapon=Instantiate(model,presentation.Body.GetBoneTransform(shown==WeaponKind.Bow||shown==WeaponKind.Shield?HumanBodyBones.LeftHand:HumanBodyBones.RightHand),false);')
s=s.replace('  void LateUpdate()', '''  public static Vector3 Origin(PlayerCharacter p){var camera=p.GetComponent<HumanPreviewCamera>();return camera&&camera.ThirdPerson?camera.CombatOrigin:p.view.transform.position;}
  public static Vector3 Aim(PlayerCharacter p)
  {
   var camera=p.GetComponent<HumanPreviewCamera>();if(!camera||!camera.ThirdPerson)return p.view.transform.forward;
   Vector3 start=p.view.transform.position,forward=p.view.transform.forward,end=start+forward*80;float nearest=80;
   foreach(var hit in Physics.RaycastAll(start,forward,80,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(p.transform)&&hit.distance<nearest){nearest=hit.distance;end=hit.point;}
   return (end-Origin(p)).normalized;
  }
  void LateUpdate()''');p.write_text(s,encoding='utf-8')
for f in ['Assets/02_Scripts/Characters/PlayerCombat.cs','Assets/02_Scripts/Characters/StaffSpells.cs','Assets/02_Scripts/Characters/InteractionTarget.cs']:
 p=Path(f);s=p.read_text(encoding='utf-8-sig').replace('origin = player.view.transform.position','origin = Codice.Presentation.HumanPreviewCamera.Origin(player)').replace('origin=player.view.transform.position','origin=Codice.Presentation.HumanPreviewCamera.Origin(player)').replace('forward = player.view.transform.forward','forward = Codice.Presentation.HumanPreviewCamera.Aim(player)').replace('forward=player.view.transform.forward','forward=Codice.Presentation.HumanPreviewCamera.Aim(player)');p.write_text(s,encoding='utf-8')
p=Path('Assets/02_Scripts/UI/RunHUD.Inventory.cs');s=p.read_text(encoding='utf-8').replace('Selecciona un objeto para examinarlo.','[V] Primera / tercera persona · Selecciona un objeto.');p.write_text(s,encoding='utf-8')
