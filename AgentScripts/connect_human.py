from pathlib import Path
p=Path('Assets/02_Scripts/Presentation/ActorPresentation.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace('var visual=player.classVisuals[index<player.classVisuals.Length?index:0];if(!visual)return;', '''var visual=player.classVisuals[index<player.classVisuals.Length?index:0];if(!visual)return;
            var human=Resources.Load<GameObject>("Presentation/Human/HumanPlayer");
            if(human&&Codice.Mechanics.RunDirector.Instance&&Codice.Mechanics.RunDirector.Instance.Expedition)
            {
                visual.SetActive(false);visual=Instantiate(human,player.transform,false);visual.name="Humanoid pilgrim";
                var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);visual.transform.localScale*=1.8f/Mathf.Max(.1f,bounds.size.y);}
            }
            bool newHuman=visual.name=="Humanoid pilgrim";
            var playerController=newHuman?Resources.Load<RuntimeAnimatorController>("Presentation/Human/HumanPlayer"):PresentationLibrary.Load().medium;''');s=s.replace('Body=visual.GetComponentInChildren<Animator>(true);Configure(Body,PresentationLibrary.Load().medium);','Body=visual.GetComponentInChildren<Animator>(true);Configure(Body,playerController);')
s=s.replace('armsRoot.transform.localScale=Vector3.one*.55f;','armsRoot.transform.localScale=newHuman?visual.transform.localScale:Vector3.one*.55f;\n            if(newHuman){armsRoot.transform.localPosition=new Vector3(0,-1.45f,.12f);foreach(var r in visual.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;}')
s=s.replace('renderer.enabled=renderer.name.IndexOf("Arm",StringComparison.OrdinalIgnoreCase)>=0;', '''renderer.enabled=renderer.name.IndexOf("Arm",StringComparison.OrdinalIgnoreCase)>=0;
                if(newHuman&&renderer is SkinnedMeshRenderer skin){var mesh=Resources.Load<Mesh>("Presentation/Human/"+renderer.name+"_Arms");if(mesh){skin.sharedMesh=mesh;renderer.enabled=true;skin.updateWhenOffscreen=true;}}''')
s=s.replace('Arms=armsRoot.GetComponentInChildren<Animator>(true);Configure(Arms,PresentationLibrary.Load().medium);','Arms=armsRoot.GetComponentInChildren<Animator>(true);Configure(Arms,playerController);')
p.write_text(s,encoding='utf-8')
