using UnityEngine;
namespace Codice.Presentation
{
    // Cloth-like scarf tail: a short verlet chain pinned to a bone and drawn as a tapered ribbon.
    // It trails behind movement, hangs with gravity and is kept behind the wearer's back.
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ScarfRibbon : MonoBehaviour
    {
        public Transform body;            // character root, defines "behind"
        public Vector3 anchorOffset;      // from this transform, in body space
        public float length = .75f, width = .085f, sway = 1;
        public int points = 9;
        Vector3[] now, old; LineRenderer line; bool ready;
        void Init()
        {
            line = GetComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = points; line.numCapVertices = 2; line.alignment = LineAlignment.View;
            line.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(.7f, .75f), new Keyframe(1, .15f)); line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            now = new Vector3[points]; old = new Vector3[points]; var a = Anchor(); var back = body ? -body.forward : Vector3.back;
            for (int i = 0; i < points; i++) now[i] = old[i] = a + (Vector3.down * .8f + back * .2f).normalized * (length / (points - 1) * i);
            ready = true; line.SetPositions(now);
        }
        Vector3 Anchor() => transform.position + (body ? body.rotation * anchorOffset : anchorOffset);
        void OnEnable() { ready = false; }
        void LateUpdate() { Step(Mathf.Min(Time.deltaTime, .033f)); }
        /// Advances the simulation; also used by editor previews.
        public void Step(float dt)
        {
            if (!ready) Init(); if (dt <= 0) { line.SetPositions(now); return; }
            float seg = length / (points - 1); var back = body ? -body.forward : Vector3.back; var a = Anchor();
            // After a teleport or scene change the chain snaps instead of stretching across the map.
            if ((now[0] - a).sqrMagnitude > 4) { for (int i = 0; i < points; i++) now[i] = old[i] = a + Vector3.down * seg * i; }
            var wind = back * (.9f * sway) + (body ? body.right : Vector3.right) * Mathf.Sin(Time.time * 2.3f + length * 9) * .35f * sway;
            for (int i = 1; i < points; i++)
            {
                var v = (now[i] - old[i]) * .92f; old[i] = now[i];
                now[i] += v + (Vector3.down * 7.5f + wind) * dt * dt;
            }
            now[0] = a;
            for (int pass = 0; pass < 4; pass++)
                for (int i = 1; i < points; i++)
                {
                    var d = now[i] - now[i - 1]; float m = d.magnitude; if (m > .0001f) now[i] = now[i - 1] + d / m * seg;
                    if (body)
                    {   // keep the cloth behind the back plane and above the floor
                        var local = body.InverseTransformPoint(now[i]); float limit = -.13f - i * .004f;
                        if (local.z > limit && local.y > .75f) { local.z = limit; now[i] = body.TransformPoint(local); }
                        if (now[i].y < body.position.y + .04f) now[i].y = body.position.y + .04f;
                    }
                }
            line.SetPositions(now);
        }
    }
}
