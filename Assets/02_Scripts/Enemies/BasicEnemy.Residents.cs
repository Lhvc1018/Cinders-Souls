using UnityEngine;
namespace Codice.Enemies
{
    public enum ResidentKind { Default, Hellhound, Doll, Skeleton, SootWolf, CryptRaider, FuneralGolem, CryptSlinger }
    public sealed partial class BasicEnemy
    {
        public ResidentKind resident;
        float fireCooldown=1.5f;bool fireAttack,comboNext;
        void ConfigureResident()
        {
            if(lungingHound)resident=ResidentKind.Hellhound;
            else if(name.Contains("CreepyDoll"))resident=ResidentKind.Doll;
            else if(name.Contains("CryptSkeleton"))resident=ResidentKind.Skeleton;
            // Muck residents: native rigs, Cinders behaviour.
            else if(variantName=="Lobo de hollin")resident=ResidentKind.SootWolf;
            else if(variantName=="Salteador de la cripta")resident=ResidentKind.CryptRaider;
            else if(variantName=="Golem funerario")resident=ResidentKind.FuneralGolem;
            else if(variantName=="Hondero de la cripta")resident=ResidentKind.CryptSlinger;
            if(resident==ResidentKind.Hellhound){kind=EnemyKind.Swordsman;speedMultiplier=1.12f;damageMultiplier=.95f;}
            if(resident==ResidentKind.Doll){kind=EnemyKind.Mage;speedMultiplier=.9f;damageMultiplier=.65f;nativeWindup=1.05f;healthMultiplier=.75f;}
            if(resident==ResidentKind.Skeleton){kind=EnemyKind.Swordsman;speedMultiplier=.65f;damageMultiplier=1.6f;healthMultiplier=1.5f;nativeWindup=.65f;}
            if(resident==ResidentKind.FuneralGolem){kind=EnemyKind.Heavy;nativeWindup=Mathf.Max(nativeWindup,1.05f);}
            if(resident==ResidentKind.CryptSlinger){kind=EnemyKind.Crossbowman;healthMultiplier=.6f;speedMultiplier=1.05f;damageMultiplier=.8f;nativeWindup=.75f;}
        }
        // Wolves approach from a seeded side and finish with a short leap; packs split left/right.
        bool Lunges=>lungingHound||resident==ResidentKind.SootWolf;
        float flankSign;static int flankCounter;
        Vector3 ApproachGoal(Vector3 targetPosition,Vector3 toTarget,float distance)
        {
            if(resident!=ResidentKind.SootWolf||distance<3.2f)return targetPosition;
            if(flankSign==0)flankSign=(flankCounter++&1)==0?1:-1;
            return targetPosition+Quaternion.Euler(0,flankSign*70,0)*(-toTarget.normalized)*2.6f;
        }
        GameObject slamWarning;
        void BeginResidentTelegraph()
        {
            if(resident==ResidentKind.FuneralGolem){
                // Ring is drawn on the floor for the whole windup so the slam stays readable under bodies.
                slamWarning=Codice.Bosses.BloodVisual.Ring(transform.position+Vector3.up*.08f,GolemSlamRadius,new Color(.35f,.6f,1f,.9f),.07f);
                if(slamWarning)Destroy(slamWarning,windup+.1f);
            }
        }
        const float GolemSlamRadius=2.6f;
        // Returns true when the resident resolved its own strike.
        bool ResidentStrike()
        {
            if(slamWarning)Destroy(slamWarning);
            if(resident==ResidentKind.FuneralGolem){
                var flat=Vector3.ProjectOnPlane(target.transform.position-transform.position,Vector3.up);
                if(Codice.Mechanics.MeleeContact.CanHit(transform,target,GolemSlamRadius,-1f))target.ReceiveDamage(new Codice.Characters.DamageInfo{amount=AttackDamage*1.15f,source=gameObject,direction=flat.normalized,melee=true,breaksGuard=true,knockback=4});
                Codice.Presentation.ImpactVfx.Slam(transform.position);Codice.Presentation.PresentationVfx.Explosion(transform.position+transform.forward*.6f);
                return true;
            }
            if(resident==ResidentKind.CryptSlinger){
                var origin=transform.position+Vector3.up*1.4f+transform.forward*.5f;var direction=(aim-origin).normalized;
                var go=new GameObject("Hondero · hueso arrojado");go.transform.position=origin;
                Codice.Presentation.SpellFlightVisual.Add(go,Codice.Presentation.ProjectileStyle.Blood);
                go.AddComponent<EnemyProjectile>().Launch(this,target,direction,15,AttackDamage);
                return true;
            }
            return false;
        }
        void ResidentRecovery()
        {
            if(resident==ResidentKind.CryptRaider){comboNext=!comboNext;cooldown=comboNext?.26f:1.25f;recovery=comboNext?.06f:.35f;}
            if(resident==ResidentKind.SootWolf){cooldown=1.1f;recovery=.3f;flankSign=-flankSign;}
            if(resident==ResidentKind.FuneralGolem){cooldown=2.2f;recovery=.75f;}
            if(resident==ResidentKind.CryptSlinger){cooldown=2.1f;recovery=.3f;}
        }
        void ResidentShot()
        {
            bool fire=resident==ResidentKind.Hellhound;Vector3 origin=transform.position+Vector3.up*(fire?.8f:1.1f)+transform.forward*.7f;
            var castFx=Codice.Presentation.ImportedAttackVfx.Burst(origin,fire,fire?.4f:.55f);
            int count=fire?1:2;var direction=(aim-origin).normalized;
            for(int i=0;i<count;i++)
            {
                var go=new GameObject(fire?"Hellhound flame lance":"Doll hex filament");go.transform.position=origin;
                Codice.Presentation.SpellFlightVisual.Add(go,fire?Codice.Presentation.ProjectileStyle.Flame:Codice.Presentation.ProjectileStyle.Curse);
                go.AddComponent<EnemyProjectile>().Launch(this,target,Quaternion.Euler(0,count==1?0:i==0?-7:7,0)*direction,fire?12:8,fire?16*DamageScale(Level):AttackDamage);
            }
            Debug.Log("[Resident attack] "+resident+" projectiles="+count);
        }
    }
}
