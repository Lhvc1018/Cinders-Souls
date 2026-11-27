using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class EnemyVisualReview
{
    public static string Run()
    {
        var root=new GameObject("Temporary enemy review");root.transform.position=new Vector3(10000,0,10000);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ExpeditionEnemies/Hellhound.prefab");
        var dog=UnityEngine.Object.Instantiate(prefab,root.transform);dog.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
        var animator=dog.GetComponentInChildren<Animator>();animator.enabled=false;
        var skin=dog.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
        var camera=new GameObject("Review camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);
        camera.transform.localPosition=new Vector3(2.6f,1.65f,3.5f);camera.transform.LookAt(root.transform.position+Vector3.up*.53f);
        camera.orthographic=true;camera.orthographicSize=1.55f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.028f,.034f,.045f);
        camera.nearClipPlane=.1f;camera.farClipPlane=15;
        var target=new RenderTexture(960,640,24);camera.targetTexture=target;
        var light=new GameObject("Review key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=1.7f;light.transform.rotation=Quaternion.Euler(40,-30,0);
        var fill=new GameObject("Review fill").AddComponent<Light>();fill.transform.SetParent(root.transform);fill.type=LightType.Point;fill.intensity=3;fill.range=6;fill.color=new Color(.52f,.65f,.9f);fill.transform.localPosition=new Vector3(-2,2,-1);
        var saved=RenderTexture.active;var report=new System.Text.StringBuilder();
        try
        {
            foreach(var sample in new[]{("Idle",0f),("Chase",.12f),("Chase",.34f),("Melee",.43f),("Stun",.25f),("Death",1.24f)})
            {
                var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name.EndsWith(sample.Item1));clip.SampleAnimation(animator.gameObject,sample.Item2);
                skin.BakeMesh(mesh,false);var matrix=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,skin.transform.parent.lossyScale);var vertices=mesh.vertices.Select(v=>dog.transform.InverseTransformPoint(matrix.MultiplyPoint3x4(v))).ToArray();
                float minimum=vertices.Min(v=>v.y),maximum=vertices.Max(v=>v.y);
                if(minimum<-.12f||maximum>1.7f)throw new Exception("Deformed bounds out of range "+sample.Item1+" "+minimum+" / "+maximum);
                camera.Render();RenderTexture.active=target;var texture=new Texture2D(960,640,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,960,640),0,0);texture.Apply();
                var path="Docs/Hellhound_Unity_"+sample.Item1+"_"+Mathf.RoundToInt(sample.Item2*100)+".png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                report.AppendLine(sample.Item1+" "+sample.Item2+"s: vertical bounds "+minimum.ToString("F3")+".."+maximum.ToString("F3")+" m");
            }
            var bones=animator.GetComponentsInChildren<Transform>();var direction=(bones.First(b=>b.name=="head").position-bones.First(b=>b.name=="pelvis").position).normalized;
            report.AppendLine("Materials: "+string.Join(", ",skin.sharedMaterials.Select(m=>m.name)));report.AppendLine("Vertices: "+skin.sharedMesh.vertexCount);
            System.IO.File.WriteAllText("Docs/EnemyVisualValidation.txt",report.ToString());return report.ToString();
        }
        finally{RenderTexture.active=saved;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(root);}
    }
}
