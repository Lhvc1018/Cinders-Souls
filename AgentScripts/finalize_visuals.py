from pathlib import Path
p=Path('Assets/02_Scripts/Presentation/ActorPresentation.cs');s=p.read_text(encoding='utf-8').replace('new Vector3(0,-1.45f,.12f)','new Vector3(0,-1.3f,.5f)');s=s.replace('            weaponsCalibrated=true;', '''            if(Body&&Body.isHuman&&Body.name=="Humanoid pilgrim")
            {
                Arms.Play("Ready",0,0);Arms.Update(0);
                foreach(var model in combat.weaponModels)if(model&&model.transform.IsChildOf(armsRoot.transform))model.transform.rotation=player.view.transform.rotation;
            }
            weaponsCalibrated=true;''');p.write_text(s,encoding='utf-8')
p=Path('AgentScripts/RepairFireMaterials.cs');s=p.read_text(encoding='utf-8');s=s.replace('var source=materials[i];if(!source)continue;', 'var source=materials[i];if(!source){materials[i]=new Material(shader);materials[i].SetColor("_Color",name=="Fire"?new Color(1,.4f,.05f):new Color(.6f,.3f,1));string fallback="Assets/Resources/CindersVfx/Materials/"+name+"_Fallback.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(fallback);if(existing){Object.DestroyImmediate(materials[i]);materials[i]=existing;}else AssetDatabase.CreateAsset(materials[i],fallback);continue;}');p.write_text(s,encoding='utf-8')
p=Path('AgentScripts/BuildHumanPlayer.cs');s=p.read_text(encoding='utf-8').replace('HumanBodyBones.RightUpperArm','HumanBodyBones.RightLowerArm').replace('HumanBodyBones.LeftUpperArm','HumanBodyBones.LeftLowerArm');p.write_text(s,encoding='utf-8')
