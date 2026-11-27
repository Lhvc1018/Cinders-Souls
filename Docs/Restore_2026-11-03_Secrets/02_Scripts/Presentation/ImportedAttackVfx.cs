using UnityEngine;
namespace Codice.Presentation
{
    public static class ImportedAttackVfx
    {
        public static void Attach(Transform parent,bool fire)
        {
            var prefab=Resources.Load<GameObject>(fire?"CindersVfx/Fire":"CindersVfx/Lightning");if(!prefab)return;
            var fx=Object.Instantiate(prefab,parent,false);fx.transform.localScale=Vector3.one*(fire?1.2f:.8f);
            foreach(var light in fx.GetComponentsInChildren<Light>())light.enabled=false;
            foreach(var col in fx.GetComponentsInChildren<Collider>())col.enabled=false;
        }
    }
}
