using UnityEngine;
using UnityEngine.UI;

namespace Codice.UI
{
    public sealed class MenuModelShowcase : MonoBehaviour
    {
        static int stageNumber;
        public bool vigia;
        RawImage image;
        GameObject stage;
        GameObject model;
        RenderTexture texture;
        string firstResource, secondResource, currentResource;
        float nextChange;
        readonly System.Collections.Generic.Dictionary<string, GameObject> cache = new System.Collections.Generic.Dictionary<string, GameObject>();
        Coroutine preloading;
        /// Instantiates the portraits ahead of time (one per frame) so browsing classes never hitches.
        public void Preload(string[] resources){if(stage&&preloading==null&&isActiveAndEnabled)preloading=StartCoroutine(PreloadRoutine(resources));}
        System.Collections.IEnumerator PreloadRoutine(string[] resources){foreach(var r in resources){if(!cache.ContainsKey(r)){var m=Spawn(r);if(m&&m!=model)m.SetActive(false);}yield return null;}}

        public void Initialize(RawImage target, string resource)
        {
            image = target;
            firstResource = resource;
            image.raycastTarget = false;
            texture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
            {
                name = "Cinders menu portrait",
                antiAliasing = 4,
                filterMode = FilterMode.Bilinear
            };
            image.texture = texture;
            stage = new GameObject("Cinders portrait stage");
            stage.transform.SetParent(transform, false);
            stage.transform.position = new Vector3(1000 + (++stageNumber) * 20, 1000, 1000);
            stage.transform.localScale = Vector3.one;

            var camera = new GameObject("Portrait camera").AddComponent<Camera>();
            camera.transform.SetParent(stage.transform, false);
            camera.transform.localPosition = new Vector3(0, 1.15f, -3.4f);
            camera.transform.LookAt(stage.transform.TransformPoint(0, 1.05f, 0));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.cullingMask = 1 << 30;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 12;
            camera.fieldOfView = 38;
            camera.targetTexture = texture;

            AddLight("Amber key", new Vector3(-2, 3, -3), new Color(1f, .72f, .46f), 3.1f);
            AddLight("Blue rim", new Vector3(2, 2, 2), new Color(.38f, .55f, .8f), 2.1f);
            SetModel(resource);
        }

        public void AlternateWith(string resource)
        {
            secondResource = resource;
            nextChange = Time.unscaledTime + 7;
        }

        void AddLight(string name, Vector3 position, Color color, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(stage.transform, false);
            light.transform.localPosition = position;
            light.transform.LookAt(stage.transform.TransformPoint(0, 1, 0));
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << 30;
        }

        public void SetModel(string resource)
        {
            if (!stage) return;
            if (model && currentResource == resource) return;
            if (model) model.SetActive(false);
            model = Spawn(resource); if (!model) return;
            model.SetActive(true); currentResource = resource;
            model.transform.localPosition = Vector3.zero;
        }
        GameObject Spawn(string resource)
        {
            if (cache.TryGetValue(resource, out var cached) && cached) return cached;
            var prefab = resource.StartsWith("Class_") ? Resources.Load<GameObject>("Presentation/Classes/"+resource.Substring(6)) : null;
            if(!prefab)prefab = Resources.Load<GameObject>("CindersMenu/" + resource);
            if (!prefab) { Debug.LogWarning("Missing Cinders menu model: " + resource); return null; }
            var spawned = Instantiate(prefab, stage.transform, false);
            spawned.transform.localPosition = Vector3.zero;
            foreach (var child in spawned.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            if (vigia) { Codice.Presentation.VigiaLook.Dress(spawned); foreach (var light in spawned.GetComponentsInChildren<Light>(true)) light.cullingMask = 1 << 30; }
            cache[resource] = spawned; return spawned;
        }

        void Update()
        {
            if (model) model.transform.localRotation = Quaternion.Euler(0, 180 + Mathf.Sin(Time.unscaledTime * .22f) * 12, 0);
            if (!string.IsNullOrEmpty(secondResource) && Time.unscaledTime >= nextChange)
            {
                SetModel(currentResource == firstResource ? secondResource : firstResource);
                nextChange = Time.unscaledTime + 7;
            }
        }

        void OnDestroy()
        {
            if (stage) Destroy(stage);
            if (texture) { texture.Release(); Destroy(texture); }
        }
    }
}

