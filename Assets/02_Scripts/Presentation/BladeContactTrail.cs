using UnityEngine;
using UnityEngine.Rendering;

namespace Codice.Presentation
{
    // Follow the actual fitted blade, including its animation and the class-specific grip.
    public sealed class BladeContactTrail : MonoBehaviour
    {
        TrailRenderer ribbon;

        public static BladeContactTrail Begin(Transform grip, bool katana)
        {
            if (!grip || !grip.gameObject.activeInHierarchy) return null;
            var effect = grip.GetComponentInChildren<BladeContactTrail>(true);
            if (!effect)
            {
                var material = Resources.Load<Material>("Presentation/LordsVfx/Slash");
                if (!material) return null;
                var tip = new GameObject("Filo en movimiento");
                tip.transform.SetParent(grip, false);
                float length = 0;
                foreach (var mesh in grip.GetComponentsInChildren<MeshFilter>())
                {
                    if (!mesh.sharedMesh) continue;
                    var bounds = mesh.sharedMesh.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                        var corner = mesh.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
                        length = Mathf.Max(length, grip.InverseTransformPoint(corner).y);
                    }
                }
                tip.transform.localPosition = Vector3.up * (length > .1f ? length * .82f : .65f);
                effect = tip.AddComponent<BladeContactTrail>();
                effect.ribbon = tip.AddComponent<TrailRenderer>();
                effect.ribbon.sharedMaterial = material;
                effect.ribbon.emitting = false;
                effect.ribbon.time = .14f;
                effect.ribbon.minVertexDistance = .025f;
                effect.ribbon.shadowCastingMode = ShadowCastingMode.Off;
                effect.ribbon.receiveShadows = false;
                effect.ribbon.endWidth = 0;
                effect.ribbon.endColor = Color.clear;
            }
            effect.ribbon.startWidth = katana ? .12f : .19f;
            effect.ribbon.startColor = katana ? new Color(.8f,.94f,1f,.92f) : new Color(1,.78f,.53f,.9f);
            effect.ribbon.Clear();
            effect.ribbon.emitting = true;
            return effect;
        }

        public void End() { if (ribbon) ribbon.emitting = false; }
        void OnDisable() { End(); }
    }
}
