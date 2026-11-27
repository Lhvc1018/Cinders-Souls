using UnityEngine;
namespace Codice.Mechanics
{
    public static class PickupScale
    {
        public static void Fit(GameObject visual,float longest)
        {
            foreach(var behaviour in visual.GetComponentsInChildren<MonoBehaviour>())behaviour.enabled=false;
            foreach(var collider in visual.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var body in visual.GetComponentsInChildren<Rigidbody>()){body.isKinematic=true;body.useGravity=false;}
            var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var pivot=visual.transform.position;float scale=longest/Mathf.Max(.001f,bounds.size.magnitude);
            visual.transform.localScale*=scale;visual.transform.position+= (pivot-bounds.center)*scale;
        }
    }
}
