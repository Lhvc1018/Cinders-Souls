using UnityEngine;
using Codice.Characters;
namespace Codice.Presentation
{
    // Correct only visual clearance; never move or resize the gameplay capsule to match a pose.
    [DefaultExecutionOrder(150)]
    public sealed class HumanGroundClearance : MonoBehaviour
    {
        Transform visual;
        Animator animator;
        PlayerCharacter player;
        CharacterController capsule;
        Vector3 rest;
        public float Lift { get; private set; }
        readonly HumanBodyBones[] samples = { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
            HumanBodyBones.LeftToes, HumanBodyBones.RightToes, HumanBodyBones.Hips, HumanBodyBones.Head };
        public void Initialize(Transform root, Animator body)
        { visual=root;animator=body;rest=root.localPosition;player=GetComponent<PlayerCharacter>();capsule=GetComponent<CharacterController>(); }
        void LateUpdate() { Resolve(); }
        public void Resolve()
        {
            if(!visual||!animator||!animator.isHuman||!player.State.IsAlive)return;
            visual.localPosition=rest;Lift=0;
            if(!player.Grounded)return;
            float floor=transform.TransformPoint(capsule.center).y-capsule.height*transform.lossyScale.y*.5f+capsule.skinWidth;
            float lowest=float.PositiveInfinity;
            foreach(var bone in samples){var t=animator.GetBoneTransform(bone);if(t)lowest=Mathf.Min(lowest,t.position.y-(bone==HumanBodyBones.Head?.12f:.06f));}
            Lift=Mathf.Clamp(floor-lowest,0,.3f);
            visual.position+=Vector3.up*Lift;
        }
    }
}
