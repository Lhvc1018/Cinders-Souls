using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class CaptureHoundMotion
{
    public static async Task<string> Run()
    {
        var root=new GameObject("Temporary animated hound review");root.transform.position=new Vector3(10000,0,10000);
        var dog=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("ExpeditionEnemies/Hellhound"),root.transform);
        dog.GetComponent<Codice.Enemies.BasicEnemy>().enabled=false;dog.GetComponent<Codice.Presentation.ActorPresentation>().enabled=false;dog.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
        var animator=dog.GetComponentInChildren<Animator>();animator.speed=0;
        var camera=new GameObject("Review camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);
        camera.transform.localPosition=new Vector3(3.2f,1.25f,2.4f);camera.transform.LookAt(root.transform.position+Vector3.up*.57f);
        camera.orthographic=true;camera.orthographicSize=1.23f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.035f,.045f);camera.nearClipPlane=.1f;camera.farClipPlane=15;
        var target=new RenderTexture(720,480,24);camera.targetTexture=target;
        var light=new GameObject("Review key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=2.2f;light.transform.rotation=Quaternion.Euler(40,-30,0);
        var fill=new GameObject("Review fill").AddComponent<Light>();fill.transform.SetParent(root.transform);fill.type=LightType.Point;fill.intensity=4;fill.range=6;fill.color=new Color(.52f,.65f,.9f);fill.transform.localPosition=new Vector3(-2,2,-1);
        var saved=RenderTexture.active;Directory.CreateDirectory("Docs/HoundMotionFrames");
        try
        {
            int index=0;
            foreach(string state in new[]{"Chase","Melee","Stun","Death"})
            for(int i=0;i<24;i++)
            {
                animator.Play(state,0,i/24f);animator.Update(0);await Task.Delay(35);camera.Render();RenderTexture.active=target;
                var image=new Texture2D(720,480,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,480),0,0);image.Apply();
                File.WriteAllBytes("Docs/HoundMotionFrames/"+(index++).ToString("D3")+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
            }
            return "96 Unity animation frames captured: Chase, Melee, Stun, Death.";
        }
        finally{RenderTexture.active=saved;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(root);}
    }
}
