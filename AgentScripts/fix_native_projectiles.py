from pathlib import Path
p=Path("Assets/02_Scripts/Bosses/BossCharacter.Wardens.cs")
s=p.read_text(encoding="utf-8-sig")
start=s.index("                        var source=visual.GetComponentsInChildren<MeshFilter>")
end=s.index("\n                    }",start)
s=s[:start]+'''                        var source=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Contains(kind==BossKind.SpearWarden?"ChiefSpear":"GoblinSword"));
                        if(source){
                            var steel=Instantiate(source.gameObject,projectile.transform,false);steel.name="Arma nativa arrojada";
                            steel.transform.localPosition=Vector3.zero;steel.transform.localRotation=Quaternion.identity;steel.transform.localScale=Vector3.one;steel.SetActive(true);
                            var bounds=new Bounds();bool first=true;
                            foreach(var filter in steel.GetComponentsInChildren<MeshFilter>(true))
                                foreach(var vertex in filter.sharedMesh.vertices){
                                    var point=steel.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                                    if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                                }
                            var size=bounds.size;float length=Mathf.Max(size.x,size.y,size.z);
                            steel.transform.localScale=Vector3.one*((kind==BossKind.SpearWarden?2.2f:1.6f)/Mathf.Max(.001f,length));
                            var axis=size.y>=size.x&&size.y>=size.z?Vector3.up:size.x>=size.z?Vector3.right:Vector3.forward;
                            steel.transform.localRotation=Quaternion.FromToRotation(axis,Vector3.forward);
                            steel.transform.localPosition=-(steel.transform.localRotation*Vector3.Scale(bounds.center,steel.transform.localScale));
                        }'''+s[end:]
p.write_text(s,encoding="utf-8-sig")
p=Path("AgentScripts/VerifyNativeBosses.cs")
s=p.read_text(encoding="utf-8-sig").replace('UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.name=="Arma nativa arrojada")','UnityEngine.Object.FindObjectsByType<Transform>().Where(m=>m.name=="Arma nativa arrojada")')
p.write_text(s,encoding="utf-8-sig")
