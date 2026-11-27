using UnityEngine;
namespace Codice.Enemies
{
    public enum ResidentKind { Default, Hellhound, Doll, Skeleton }
    public sealed partial class BasicEnemy
    {
        public ResidentKind resident;
        float fireCooldown=1.5f;bool fireAttack,comboNext;
        void ConfigureResident()
        {
            if(lungingHound)resident=ResidentKind.Hellhound;
            else if(name.Contains("CreepyDoll"))resident=ResidentKind.Doll;
            else if(name.Contains("CryptSkeleton"))resident=ResidentKind.Skeleton;
            if(resident==ResidentKind.Hellhound){kind=EnemyKind.Swordsman;speedMultiplier=1.12f;damageMultiplier=.95f;}
            if(resident==ResidentKind.Doll){kind=EnemyKind.Mage;speedMultiplier=.9f;damageMultiplier=.65f;nativeWindup=1.05f;healthMultiplier=.75f;}
            if(resident==ResidentKind.Skeleton){kind=EnemyKind.Swordsman;speedMultiplier=.65f;damageMultiplier=1.6f;healthMultiplier=1.5f;nativeWindup=.65f;}
        }
        void ResidentShot()
        {
            bool fire=resident==ResidentKind.Hellhound;Vector3 origin=transform.position+Vector3.up*(fire?.8f:1.1f)+transform.forward*.7f;
            int count=fire?1:2;var direction=(aim-origin).normalized;
            for(int i=0;i<count;i++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=fire?"Hellhound ember":"Doll curse";Destroy(go.GetComponent<Collider>());
                go.transform.position=origin;go.transform.localScale=Vector3.one*(fire?.25f:.18f);
                var color=fire?new Color(1,.22f,.015f):new Color(.6f,.12f,.9f);
                go.GetComponent<Renderer>().sharedMaterial=Codice.Bosses.BloodVisual.Material;
                var block=new MaterialPropertyBlock();block.SetColor("_Color",color);go.GetComponent<Renderer>().SetPropertyBlock(block);
                var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=Codice.Bosses.BloodVisual.Material;trail.time=.22f;trail.startWidth=.18f;trail.endWidth=0;trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0);
                Codice.Presentation.ImportedAttackVfx.Attach(go.transform,fire);
                go.AddComponent<EnemyProjectile>().Launch(this,target,Quaternion.Euler(0,count==1?0:i==0?-7:7,0)*direction,fire?12:8,fire?16*DamageScale(Level):AttackDamage);
            }
            Debug.Log("[Resident attack] "+resident+" projectiles="+count);
        }
    }
}
