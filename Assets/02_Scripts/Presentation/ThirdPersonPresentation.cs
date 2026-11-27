using UnityEngine;
using Codice.Characters;
namespace Codice.Presentation
{
    [DefaultExecutionOrder(160)]
    public sealed class ThirdPersonPresentation : MonoBehaviour
    {
        PlayerCharacter player;ActorPresentation presentation;HumanPreviewCamera cameraMode;
        float facing;bool initialized;
        public void AttackOffhand(string state,float duration){if(!presentation)presentation=GetComponent<ActorPresentation>();var body=presentation.Body;if(body&&body.layerCount>1)body.SetLayerWeight(1,0);}
        void Start(){player=GetComponent<PlayerCharacter>();presentation=GetComponent<ActorPresentation>();cameraMode=GetComponent<HumanPreviewCamera>();}
        void LateUpdate()
        {
            if(!player||!presentation.Body||!cameraMode)return;
            if(presentation.LaboratoryPreviewActive)return;
            var body=presentation.Body;var visual=body.transform;
            if(!player.State.IsAlive)return;
            // Full-body native left/right attacks already include their torso and shoulder motion.
            if(body.layerCount>1)body.SetLayerWeight(1,0);
            if(!initialized){facing=transform.eulerAngles.y;initialized=true;}
            if(!cameraMode.ThirdPerson){visual.localRotation=Quaternion.identity;facing=transform.eulerAngles.y;return;}
            var combat=GetComponent<PlayerCombat>();
            int carry=body.GetLayerIndex("Weapon carry");
            if(carry>=0){
                bool locomotion=(presentation.CurrentState=="Walk"||presentation.CurrentState=="Movement")&&!combat.IsSwinging&&!player.GuardMovementLocked&&!player.IsDashing&&player.Grounded;
                var w=player.State.ActiveWeapon;
                string stance=player.State.DualWield?"DualIdle":w?.katana==true||w?.kind==WeaponKind.Hammer?"KatanaIdle":w?.kind==WeaponKind.Polearm?"SpearIdle":w?.kind==WeaponKind.Bow&&!w.shuriken?"BowIdle":w?.kind==WeaponKind.Staff?"StaffIdle":"SwordIdle";
                body.SetLayerWeight(carry,locomotion?1:0);
                if(locomotion&&!body.GetCurrentAnimatorStateInfo(carry).IsName(stance))body.CrossFadeInFixedTime(stance,.12f,carry);
            }
            Vector3 heading=player.IsDashing?player.DodgeDirection:player.MotorVelocity;
            if(player.GetComponent<PlayerLockOn>()?.Target||combat.IsSwinging||combat.Blocking||GetComponent<RunSpellbook>()?.Casting==true||GetComponent<StaffSpells>()?.Casting==true||GetComponent<ShinobiGuard>()?.Guarding==true)heading=transform.forward;
            if(GetComponent<Codice.Mechanics.PlayerMechanics>()?.UltimateActive==true)heading=transform.forward;
            // Iaido: the body snaps to the cut direction (the dash moves the controller directly, so MotorVelocity is zero).
            var iaido=GetComponent<ChargedAttack>();if(iaido&&iaido.IaidoActive){facing=Mathf.Atan2(iaido.IaidoDirection.x,iaido.IaidoDirection.z)*Mathf.Rad2Deg;heading=Vector3.zero;}
            if(heading.sqrMagnitude>.02f){float target=Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg;facing=Mathf.MoveTowardsAngle(facing,target,(player.IsDashing?1800:900)*Time.deltaTime);}
            visual.rotation=Quaternion.Euler(0,facing,0);
            Vector3 relative=visual.InverseTransformDirection(player.MotorVelocity);
            body.SetFloat("MoveX",Mathf.Clamp(relative.x/(player.Sprinting?4:2),-1,1),.07f,Time.deltaTime);
            body.SetFloat("MoveY",Mathf.Clamp(relative.z/(player.Sprinting?4:2),-1,1),.07f,Time.deltaTime);
            body.SetFloat("LocomotionRate",Mathf.Clamp(player.MotorVelocity.magnitude/(player.Sprinting?4:2),.35f,1.6f));
        }
    }
}



