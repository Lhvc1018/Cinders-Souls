using UnityEngine;
namespace Codice.Presentation
{
    public static class ImportedAttackVfx
    {
        public static GameObject Burst(Vector3 position,bool fire,float scale)
        {
            if(Codice.Multiplayer.LanSession.Host)Codice.Multiplayer.LanSession.Instance.EmitFx("IAB:"+(fire?1:0),position,Vector3.up,scale,0,Color.white);   /* guests see boss/enemy bursts too */
            var prefab=Resources.Load<GameObject>(fire?"CindersVfx/Fire":"CindersVfx/Lightning");if(!prefab)return null;
            var fx=Object.Instantiate(prefab,position,Quaternion.identity);fx.transform.localScale=Vector3.one*scale;
            foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>()){var main=ps.main;main.loop=false;main.stopAction=ParticleSystemStopAction.None;}
            foreach(var light in fx.GetComponentsInChildren<Light>())light.enabled=false;
            foreach(var col in fx.GetComponentsInChildren<Collider>())col.enabled=false;
            Object.Destroy(fx,2.5f);return fx;
        }
        public static void Attach(Transform parent,bool fire)
        {
            var prefab=Resources.Load<GameObject>(fire?"CindersVfx/Fire":"CindersVfx/Lightning");if(!prefab)return;
            var fx=Object.Instantiate(prefab,parent,false);fx.transform.localScale=Vector3.one*(fire?1.2f:.8f);
            foreach(var light in fx.GetComponentsInChildren<Light>())light.enabled=false;
            foreach(var col in fx.GetComponentsInChildren<Collider>())col.enabled=false;
        }
    }
}
