using System;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;
using Codice.Expedition;
namespace Codice.Mechanics
{
    public sealed class ProceduralContentWorkbench : MonoBehaviour
    {
        public ExpeditionContentCatalog catalog;
        public Transform editableContent;
        public Transform previewRoot;
        public int seed = 12345;
        [Range(0,24)] public int propCount = 8;
        public Vector2 area = new Vector2(20,20);
        [ContextMenu("Generar decoración de prueba")]
        public void GeneratePreview()
        {
            if(!previewRoot) { var root=new GameObject("VISTA PROCEDURAL · regenerable");root.transform.SetParent(transform,false);previewRoot=root.transform; }
            foreach(Transform child in previewRoot.Cast<Transform>().ToArray())
            { if(Application.isPlaying){child.gameObject.SetActive(false);Destroy(child.gameObject);}else DestroyImmediate(child.gameObject); }
            var candidates=catalog?catalog.additionalProps.Where(p=>p).ToArray():Array.Empty<GameObject>();
            if(candidates.Length==0)return;
            var rng=new System.Random(seed);
            for(int i=0;i<propCount;i++)
            {
                float side=i%2==0?-1:1;
                var position=transform.position+new Vector3(side*area.x*.4f,0,(float)(rng.NextDouble()-.5)*area.y);
                var go=Instantiate(candidates[rng.Next(candidates.Length)],position,Quaternion.Euler(0,rng.Next(4)*90,0),previewRoot);
                go.name="Muestra "+i+" · "+go.name;
            }
            RebuildNavigation();
            Debug.Log($"[ContentLab] seed={seed}; props={propCount}; manual content preserved");
        }
        [ContextMenu("Reconstruir navegación")]
        public void RebuildNavigation(){if(Application.isPlaying)FindAnyObjectByType<NavMeshSurface>()?.BuildNavMesh();}
        void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireCube(transform.position+Vector3.up*.05f,new Vector3(area.x,.1f,area.y));}
    }
}
