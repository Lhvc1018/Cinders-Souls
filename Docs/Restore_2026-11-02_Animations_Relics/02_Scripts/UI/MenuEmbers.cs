using UnityEngine;
using Coffee.UIExtensions;
namespace Codice.UI
{
    public sealed class MenuEmbers : MonoBehaviour
    {
        Material material;
        void Start()
        {
            var particles=new GameObject("Candle embers",typeof(RectTransform)).AddComponent<ParticleSystem>();
            particles.transform.SetParent(transform,false);
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=true;main.startLifetime=12;main.startSpeed=9;main.startSize=new ParticleSystem.MinMaxCurve(1,3);
            main.startColor=new Color(.65f,.3f,.12f,.4f);main.maxParticles=60;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.useUnscaledTime=true;
            var emission=particles.emission;emission.rateOverTime=4;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(1800,800,0);
            var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;velocity.y=8;
            var colors=particles.colorOverLifetime;colors.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.8f,.4f,.18f),0),new GradientColorKey(new Color(.4f,.2f,.1f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.35f,.2f),new GradientAlphaKey(0,1)});colors.color=gradient;
            material=new Material(Shader.Find("UI/Default"));particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            var ui=gameObject.AddComponent<UIParticle>();ui.scale=1;ui.RefreshParticles();particles.Play();
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
}
