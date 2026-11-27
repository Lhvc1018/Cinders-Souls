using UnityEngine;
using Codice.Characters;
namespace Codice.Presentation
{
    // Anatomical socket frame: weapon +Y follows the knuckles toward the thumb, not the imported hand axes.
    public static class HumanoidWeaponGrip
    {
        public static Transform Socket(Animator body, bool left)
        {
            // A body can be mid-swap (no humanoid avatar yet, e.g. a coop guest being dressed): skip instead of throwing.
            if(!body||!body.avatar||!body.isHuman)return null;
            var hand=body.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);if(!hand)return null;
            var existing=hand.Find("Anatomical weapon socket");if(existing)return existing;
            var index=body.GetBoneTransform(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal);
            var little=body.GetBoneTransform(left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal);
            var middle=body.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
            Vector3 along=middle?(middle.position-hand.position).normalized:hand.right;
            Vector3 across=index&&little?(index.position-little.position).normalized:hand.up;
            Vector3 normal=Vector3.Cross(along,across).normalized;
            var socket=new GameObject("Anatomical weapon socket").transform;socket.SetParent(hand,false);
            Transform authored=null;
            foreach(var candidate in hand.GetComponentsInChildren<Transform>())if(candidate.name==(left?"B-handProp.L":"B-handProp.R")){authored=candidate;break;}
            if(authored){socket.position=authored.position;socket.rotation=authored.rotation;}
            else{socket.position=hand.position+along*.065f+normal*(left?-.012f:.012f);socket.rotation=Quaternion.LookRotation(normal,across);}
            return socket;
        }
        public static GameObject Attach(Animator body, WeaponInstance item, bool left)
        {
            var prefab=WeaponVisualCatalog.Load()?.Get(item);if(!prefab)return null;
            var socket=Socket(body,left);if(!socket)return null;var go=Object.Instantiate(prefab,socket,false);
            go.name=left?"Third person left grip":"Third person right grip";
            go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
            // Normalized catalog models have the handle at their origin and their length on +Y.
            if(item.kind==WeaponKind.Shield){go.transform.localRotation=Quaternion.Euler(0,0,75);go.transform.localPosition=new Vector3(-.025f,.015f,-.035f);}
            go.transform.localScale=Vector3.one/Mathf.Max(.001f,socket.lossyScale.x);
            foreach(var col in go.GetComponentsInChildren<Collider>())Object.Destroy(col);
            if(item.kind!=WeaponKind.Shield&&item.kind!=WeaponKind.Bow)WeaponGroundGuard.Add(go,body.transform.root);
            return go;
        }
    }
}
