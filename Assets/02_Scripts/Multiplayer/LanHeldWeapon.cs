using System.Linq;
using UnityEngine;
using Codice.Characters;
using Codice.Presentation;
namespace Codice.Multiplayer
{
    public sealed class LanHeldWeapon : MonoBehaviour
    {
        public PlayerCharacter player;
        GameObject model;string key;
        void LateUpdate(){if(!player)return;var cam=player.GetComponent<HumanPreviewCamera>();if(cam&&cam.ThirdPerson){if(model)Destroy(model);if(left)Destroy(left);key=null;return;}   /* its third-person rig already holds the weapon (was a 2nd blade) */
            Apply(player.State.ActiveWeapon);}
        GameObject left;
        public void Apply(WeaponInstance weapon)
        {
            PlayerClass cls=player?player.State.Class:ClassFromKey();
            WeaponInstance second=player?player.State.GetWeapon(1-player.State.ActiveSlot):null;
            bool dual=weapon!=null&&!weapon.katana&&(weapon.kind==WeaponKind.Sword||weapon.kind==WeaponKind.Dagger)&&(player?player.State.DualWield:cls==PlayerClass.Berserker);
            bool shield=weapon!=null&&cls==PlayerClass.Warrior&&!dual&&!weapon.katana&&(weapon.kind==WeaponKind.Sword||weapon.kind==WeaponKind.Dagger);
            string next=weapon==null?"":weapon.kind+":"+weapon.tier+":"+weapon.katana+":"+weapon.shuriken+":"+dual+":"+shield;
            if(next==key&&(model||weapon==null))return;
            var body=Body();if(weapon!=null&&!body)return;   /* model still being dressed: retry next frame */
            key=next;if(model)Destroy(model);if(left)Destroy(left);if(weapon==null)return;
            bool leftHand=weapon.kind==WeaponKind.Bow&&!weapon.shuriken||weapon.kind==WeaponKind.Shield||weapon.kind==WeaponKind.Staff;
            /* Same anatomical grip as the local third-person player: right size, right angle (was a stretched blade with no shield). */
            model=HumanoidWeaponGrip.Attach(body,weapon,leftHand);
            if(dual)left=HumanoidWeaponGrip.Attach(body,second??weapon,true);
            else if(shield)left=HumanoidWeaponGrip.Attach(body,new WeaponInstance(WeaponKind.Shield,weapon.tier),true);
        }
        Animator Body()
        {
            var root=player?player.transform.Find("Humanoid pilgrim"):transform;if(!root)return null;
            return root.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.avatar&&a.isHuman);
        }
        PlayerClass ClassFromKey(){var ghost=GetComponent<LanGhost>();var k=ghost&&ghost.snapshot!=null?ghost.snapshot.key:null;return k!=null&&k.StartsWith("Player:")&&int.TryParse(k.Substring(7),out int c)?(PlayerClass)c:PlayerClass.Warrior;}
    }
}
