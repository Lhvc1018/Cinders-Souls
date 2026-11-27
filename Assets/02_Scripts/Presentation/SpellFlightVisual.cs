using UnityEngine;
namespace Codice.Presentation
{
 public enum ProjectileStyle { Flame, Curse, Arcane, Blood }
 public sealed class SpellFlightVisual:MonoBehaviour
 {
  public ProjectileStyle Style=>style;
  ProjectileStyle style;LineRenderer spine,filament;Vector3 previous;float pulse;
  public static SpellFlightVisual Add(GameObject root,ProjectileStyle style)
  {
   var visual=root.AddComponent<SpellFlightVisual>();visual.style=style;visual.previous=root.transform.position;
   var tint=style==ProjectileStyle.Flame?new Color(1,.35f,.04f):style==ProjectileStyle.Curse?new Color(.72f,.22f,1):style==ProjectileStyle.Blood?new Color(1,.06f,.13f):new Color(.2f,.65f,1);
   var core=new GameObject("Elongated spell core");core.transform.SetParent(root.transform,false);visual.spine=Codice.Bosses.BloodVisual.Line(core,tint,.075f);visual.spine.positionCount=9;visual.spine.startWidth=.09f;visual.spine.endWidth=.005f;
   var sparks=new GameObject("Orbiting filament");sparks.transform.SetParent(root.transform,false);visual.filament=Codice.Bosses.BloodVisual.Line(sparks,Color.Lerp(tint,Color.white,.45f),.022f);visual.filament.positionCount=13;
   var trail=root.AddComponent<TrailRenderer>();trail.sharedMaterial=Codice.Bosses.BloodVisual.Material;trail.time=style==ProjectileStyle.Flame?.3f:.18f;trail.minVertexDistance=.04f;trail.startWidth=.13f;trail.endWidth=0;trail.startColor=tint;trail.endColor=new Color(tint.r,tint.g,tint.b,0);
   if(style==ProjectileStyle.Flame){var flame=new GameObject("Imported flame ribbon");flame.transform.SetParent(root.transform,false);flame.transform.localScale=Vector3.one*.28f;ImportedAttackVfx.Attach(flame.transform,true);}
   return visual;
  }
  void LateUpdate()
  {
   if(Time.timeScale==0)return;var delta=transform.position-previous;previous=transform.position;if(delta.sqrMagnitude>.000001f)transform.rotation=Quaternion.LookRotation(delta);pulse+=Time.deltaTime*19;
   for(int i=0;i<9;i++){float t=i/8f;float jitter=style==ProjectileStyle.Flame?Mathf.Sin(pulse+i*2)*.04f:(i%2==0?1:-1)*.055f;spine.SetPosition(i,transform.position-transform.forward*t*(style==ProjectileStyle.Blood?1.4f:.9f)+transform.right*jitter*t);}
   for(int i=0;i<13;i++){float t=i/12f;float angle=t*12+pulse;filament.SetPosition(i,transform.position-transform.forward*t*1.05f+(transform.right*Mathf.Cos(angle)+transform.up*Mathf.Sin(angle))*(.1f*(1-t)));}
  }
  public void Impact(Vector3 point){ImportedAttackVfx.Burst(point,style==ProjectileStyle.Flame,style==ProjectileStyle.Flame?.6f:.4f);}
 }
}
