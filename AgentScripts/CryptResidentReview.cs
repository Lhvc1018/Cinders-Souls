using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Codice.Characters;
using Codice.Enemies;
using Codice.Presentation;
using Codice.Expedition;
using Codice.UI;
public static class CryptResidentReview
{
    public static async Task<string> Visuals()
    {
        var root=new GameObject("Temporary crypt review");root.transform.position=new Vector3(10000,0,10000);
        var camera=new GameObject("Review camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.localPosition=new Vector3(3,2.3f,4);camera.transform.LookAt(root.transform.position+Vector3.up);camera.orthographic=true;camera.orthographicSize=1.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.03f,.032f);camera.farClipPlane=15;
        var key=new GameObject("Key").AddComponent<Light>();key.transform.SetParent(root.transform);key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(35,-25,0);key.intensity=1.7f;
        var plane=GameObject.CreatePrimitive(PrimitiveType.Plane);plane.transform.SetParent(root.transform,false);plane.transform.localPosition=Vector3.down*.01f;plane.transform.localScale=Vector3.one;
        var floorMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));floorMaterial.SetColor("_BaseColor",new Color(.08f,.08f,.08f));plane.GetComponent<Renderer>().sharedMaterial=floorMaterial;
        var target=new RenderTexture(720,720,24);camera.targetTexture=target;var saved=RenderTexture.active;var report=new System.Text.StringBuilder();
        try
        {
            foreach(string name in new[]{"CreepyDoll","CryptSkeleton"})
            {
                var actor=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("ExpeditionEnemies/"+name),root.transform);actor.GetComponent<BasicEnemy>().enabled=false;actor.GetComponent<ActorPresentation>().enabled=false;actor.GetComponent<NavMeshAgent>().enabled=false;
                var animator=actor.GetComponentInChildren<Animator>();animator.speed=0;
                foreach(var pose in new[]{("Idle",.1f),("Chase",.25f),("Melee",.65f),("Stun",.4f),("Death",.95f)})
                {
                    animator.Play(pose.Item1,0,pose.Item2);animator.Update(0);await Task.Delay(50);camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(720,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,720),0,0);image.Apply();System.IO.File.WriteAllBytes("Docs/"+name+"_"+pose.Item1+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
                    var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,false);var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                    float min=mesh.vertices.Min(v=>matrix.MultiplyPoint3x4(v).y),max=mesh.vertices.Max(v=>matrix.MultiplyPoint3x4(v).y);report.AppendLine(name+" "+pose.Item1+" height="+min.ToString("F3")+".."+max.ToString("F3"));UnityEngine.Object.Destroy(mesh);
                }
                UnityEngine.Object.Destroy(actor);await Task.Delay(50);
            }
            System.IO.File.WriteAllText("Docs/CryptResidents_VisualValidation.txt",report.ToString());return report.ToString();
        }
        finally{RenderTexture.active=saved;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(floorMaterial);}
    }
    public static async Task<string> Combat()
    {
        var p=UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();var floor=UnityEngine.Object.FindAnyObjectByType<ExpeditionFloor>();RunHUD.Instance.SetPaused(false);var report=new System.Text.StringBuilder();
        foreach(string name in new[]{"CreepyDoll","CryptSkeleton"})
        {
            var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=floor.Center(0)+Vector3.up*.1f;cc.enabled=true;
            var actor=UnityEngine.Object.Instantiate(Resources.Load<BasicEnemy>("ExpeditionEnemies/"+name),p.transform.position+Vector3.forward*6,Quaternion.identity);actor.Initialize(p,1);
            var presentation=actor.GetComponent<ActorPresentation>();var states=new HashSet<string>();var animator=actor.animator;float movement=0,angles=0;var old=actor.transform.position;
            var bone=animator.GetComponentsInChildren<Transform>().First(t=>t.name==(name=="CreepyDoll"?"Bone.001_L.001":"Bip001 L Thigh"));var initial=bone.localRotation;
            for(int i=0;i<75;i++){await Task.Delay(100);p.State.Heal(1000);states.Add(presentation.CurrentState);movement+=Vector3.Distance(old,actor.transform.position);old=actor.transform.position;angles=Mathf.Max(angles,Quaternion.Angle(initial,bone.localRotation));}
            if(!states.Contains("Chase")||!states.Contains("Melee")||movement<2||angles<5)throw new Exception(name+" incomplete movement or attack: "+string.Join(",",states)+" movement="+movement+" angle="+angles);
            actor.ApplyStun(.6f);await Task.Delay(120);if(presentation.CurrentState!="Stun")throw new Exception(name+" stun missing");
            actor.ReceiveDamage(new DamageInfo{amount=10000,source=p.gameObject,ignoreGuard=true});await Task.Delay(120);
            if(!UnityEngine.Object.FindObjectsByType<Animator>().Any(a=>a.name=="Animated remains"))throw new Exception(name+" corpse missing");
            report.AppendLine(name+" moved="+movement.ToString("F2")+"m bone motion="+angles.ToString("F1")+"deg states="+string.Join(",",states)+" stun and death passed.");
        }
        System.IO.File.WriteAllText("Docs/CryptResidents_CombatValidation.txt",report.ToString());return report.ToString();
    }
}
