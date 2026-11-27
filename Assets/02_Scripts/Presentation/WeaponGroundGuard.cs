using System.Linq;
using UnityEngine;
namespace Codice.Presentation
{
    // Visual-only: after animation, pivots a held weapon around its grip so neither end
    // sinks below the floor. Hit resolution never reads this transform.
    [DefaultExecutionOrder(200)]
    public sealed class WeaponGroundGuard : MonoBehaviour
    {
        public float margin = .07f; // covers shaft thickness; ends are measured on the axis
        Vector3[] ends;
        Transform owner;
        Quaternion restRotation;
        void Awake(){restRotation=transform.localRotation;}
        readonly RaycastHit[] hits = new RaycastHit[8];
        public static WeaponGroundGuard Add(GameObject weapon, Transform owner)
        {
            if (!weapon) return null;
            var guard = weapon.GetComponent<WeaponGroundGuard>(); if (!guard) guard = weapon.AddComponent<WeaponGroundGuard>();
            guard.owner = owner ? owner : weapon.transform.root; guard.ends = null; return guard;
        }
        void Measure()
        {
            bool first = true; var b = new Bounds();
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh) continue; var mb = filter.sharedMesh.bounds;
                for (int c = 0; c < 8; c++)
                {
                    var v = mb.center + Vector3.Scale(mb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                    var p = transform.InverseTransformPoint(filter.transform.TransformPoint(v));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                }
            }
            if (first) { ends = new Vector3[0]; return; }
            var s = b.size; var axis = s.x >= s.y && s.x >= s.z ? Vector3.right * b.extents.x : s.y >= s.z ? Vector3.up * b.extents.y : Vector3.forward * b.extents.z;
            ends = new[] { b.center + axis, b.center - axis };
        }
        float Floor(Vector3 point)
        {
            float floor = owner ? owner.position.y : float.NegativeInfinity;
            int count = Physics.RaycastNonAlloc(point + Vector3.up * 1.2f, Vector3.down, hits, 2.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (owner && h.transform.IsChildOf(owner)) continue;
                if (h.collider.attachedRigidbody && !h.collider.attachedRigidbody.isKinematic) continue;
                if (h.normal.y < .6f) continue; // walls and props do not count as floor
                if (h.point.y > best) best = h.point.y;
            }
            return best > float.NegativeInfinity ? best : floor;
        }
        void LateUpdate()
        {
            transform.localRotation=restRotation;
            if (ends == null) Measure();
            if (ends.Length == 0) return;
            var pivot = transform.position;
            for (int pass = 0; pass < 2; pass++)
            {
                Vector3 lowest = Vector3.zero; float depth = 0;
                foreach (var e in ends)
                {
                    var w = transform.TransformPoint(e);
                    float d = Floor(w) + margin - w.y;
                    if (d > depth) { depth = d; lowest = w; }
                }
                if (depth <= 0) return;
                var from = lowest - pivot;
                if (from.sqrMagnitude < .0001f) return;
                var to = from + Vector3.up * depth;
                to = to.normalized * from.magnitude; // keep the weapon rigid
                transform.rotation = Quaternion.FromToRotation(from, to) * transform.rotation;
                transform.position = pivot;
            }
        }
    }
}
