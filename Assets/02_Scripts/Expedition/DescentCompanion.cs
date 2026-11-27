using UnityEngine;
using UnityEngine.AI;
using Codice.Characters;
using Codice.Mechanics;
using Codice.Enemies;
using Codice.Bosses;
namespace Codice.Expedition
{
    public sealed class DescentCompanion:MonoBehaviour
    {
        public PlayerCharacter Owner{get;private set;}
        public bool Wisp{get;private set;}
        public bool Moving=>agent&&agent.isOnNavMesh&&agent.velocity.sqrMagnitude>.1f;
        NavMeshAgent agent;Animator animator;float cooldown,repath;LineRenderer beam;float beamUntil,castUntil,releaseAt;Component pending;
        public static DescentCompanion Create(PlayerCharacter owner,bool wisp)
        {
            foreach(var existing in owner.gameObject.scene.GetRootGameObjects())
                if(existing.TryGetComponent<DescentCompanion>(out var found)&&found.Owner==owner&&found.Wisp==wisp)return found;
            var go=new GameObject(wisp?"Lumbre · Compañera":"Irel · Peregrino contratado");
            go.transform.position=owner.transform.position;var c=go.AddComponent<DescentCompanion>();c.Owner=owner;c.Wisp=wisp;
            if(wisp)FlameVisual(go.transform,new Color(.3f,.85f,1));
            else {c.animator=PilgrimVisual(go.transform).GetComponentInChildren<Animator>();c.agent=go.AddComponent<NavMeshAgent>();c.agent.radius=.25f;c.agent.height=1.8f;c.agent.speed=7;c.agent.acceleration=25;c.agent.angularSpeed=720;c.agent.stoppingDistance=.65f;
                if(NavMesh.SamplePosition(owner.transform.position+owner.transform.right*2,out var hit,2,-1))c.agent.Warp(hit.position);}
            c.cooldown=Time.time+3;return c;
        }
        public static GameObject PilgrimVisual(Transform parent)
        {
            var model=Resources.Load<GameObject>("WorldProgression/Nereo");
            var go=model?Instantiate(model,parent,false):new GameObject("Missing pilgrim visual");go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*.94f;
            var a=go.GetComponentInChildren<Animator>();var controller=Resources.Load<RuntimeAnimatorController>("WorldProgression/IrelController");if(a&&controller)a.runtimeAnimatorController=controller;
            // The pilgrim shares the merchant's rig, but wears a cold-grey mantle.
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())for(int i=0;i<renderer.sharedMaterials.Length;i++){var mat=renderer.sharedMaterials[i];if(mat&&(mat.name.Contains("Paño")||mat.name.Contains("Seda"))){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.12f,.17f,.2f));renderer.SetPropertyBlock(block,i);}}
            return go;
        }
        public static void FlameVisual(Transform parent,Color color)
        {
            var light=new GameObject("Lumbre");light.transform.SetParent(parent,false);light.transform.localPosition=Vector3.up*1.25f;
            var lamp=light.AddComponent<Light>();lamp.color=color;lamp.range=3;lamp.intensity=1.1f;
            var ps=light.AddComponent<ParticleSystem>();var main=ps.main;main.startLifetime=.65f;main.startSpeed=.25f;main.startSize=.07f;main.startColor=color;main.maxParticles=20;
            var emission=ps.emission;emission.rateOverTime=24;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.08f;
            var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetColor("_BaseColor",color);
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float a=Mathf.Clamp01(1-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f);pixels[y*32+x]=new Color(1,1,1,a*a);}tex.SetPixels(pixels);tex.Apply();material.SetTexture("_BaseMap",tex);material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",1);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            var cleanup=light.AddComponent<CompanionMaterialOwner>();cleanup.material=material;cleanup.texture=tex;
        }
        public void Regroup(ExpeditionFloor floor,int room)
        {
            if(!Owner||floor.Layout.RoomAt(transform.position)==room)return;
            if(NavMesh.SamplePosition(Owner.transform.position+Owner.transform.right*2,out var hit,2,-1)){if(agent&&agent.isOnNavMesh)agent.Warp(hit.position);else transform.position=hit.position;}
        }
        void Update()
        {
            var run=RunDirector.Instance;
            if(!Owner){Destroy(gameObject);return;}
            if(beam&&Time.time>beamUntil)beam.enabled=false;
            bool active=Owner&&Owner.State.IsAlive&&run&&run.Expedition&&run.Phase!=RunPhase.Defeated&&run.Phase!=RunPhase.Victory&&!Owner.ControlsBlocked;
            if(!active){pending=null;if(agent&&agent.isOnNavMesh)agent.isStopped=true;if(animator)animator.SetBool("Moving",false);return;}
            if(Wisp){var target=Owner.transform.position-Owner.transform.right*.8f-Owner.transform.forward*.7f;transform.position=Vector3.Lerp(transform.position,target,Time.deltaTime*9);}
            else if(agent&&agent.isOnNavMesh){agent.isStopped=Time.time<castUntil;if(Time.time>repath){repath=Time.time+.3f;var follow=Owner.transform.position+Owner.transform.right*2.2f-Owner.transform.forward;
                    if(NavMesh.SamplePosition(follow,out var destination,2,-1))agent.SetDestination(destination.position);}
                if(!run.Expedition.InEncounter&&Vector3.Distance(transform.position,Owner.transform.position)>18&&NavMesh.SamplePosition(Owner.transform.position,out var p,2,-1))agent.Warp(p.position);
                if(animator)animator.SetBool("Moving",agent.velocity.sqrMagnitude>.1f);}
            if(!run.Expedition.InEncounter){pending=null;return;}
            if(pending&&Time.time>=releaseAt){var shot=pending;pending=null;Component visible=null;float range=12;Choose(shot,ref visible,ref range);if(visible)Fire(shot);}
            if(Time.time<cooldown)return;
            cooldown=Time.time+(Wisp?6:2.4f);
            if(Wisp){Owner.State.RestoreMana(4);return;}
            Component targetEnemy=null;float distance=12;
            foreach(var e in FindObjectsByType<BasicEnemy>(FindObjectsSortMode.None))if(e.IsAlive&&!e.Paused)Choose(e,ref targetEnemy,ref distance);
            var boss=run.Expedition.context.arena.ActiveBoss;if(boss&&boss.IsAlive&&!boss.Paused)Choose(boss,ref targetEnemy,ref distance);
            if(!targetEnemy)return;
            pending=targetEnemy;releaseAt=Time.time+.35f;castUntil=Time.time+.85f;
            var forward=Vector3.ProjectOnPlane(targetEnemy.transform.position-transform.position,Vector3.up);if(forward.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(forward);
            if(animator)animator.SetTrigger("Cast");
        }
        void Fire(Component targetEnemy)
        {
            if(targetEnemy is BasicEnemy enemy&&!enemy.IsAlive||targetEnemy is BossCharacter boss&&!boss.IsAlive)return;
            var run=RunDirector.Instance;
            var origin=transform.position+Vector3.up*1.3f;var end=targetEnemy.transform.position+Vector3.up;
            (targetEnemy as IDamageReceiver)?.ReceiveDamage(new DamageInfo{amount=14+run.FloorNumber*2,source=Owner.gameObject,direction=(end-origin).normalized,secondary=true});
            if(!beam){beam=gameObject.AddComponent<LineRenderer>();beam.positionCount=2;beam.startWidth=.045f;beam.endWidth=.015f;beam.material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));beam.startColor=beam.endColor=new Color(.5f,.8f,1);}
            beam.SetPosition(0,origin);beam.SetPosition(1,end);beam.enabled=true;beamUntil=Time.time+.13f;
        }
        void Choose(Component candidate,ref Component best,ref float distance)
        {
            float d=Vector3.Distance(transform.position,candidate.transform.position);if(d>=distance)return;
            var start=transform.position+Vector3.up*1.3f;var delta=candidate.transform.position+Vector3.up-start;
            foreach(var hit in Physics.RaycastAll(start,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(candidate.transform)&&!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(Owner.transform))return;
            best=candidate;distance=d;
        }
        void OnDestroy(){if(beam&&beam.sharedMaterial)Destroy(beam.sharedMaterial);}
    }
    public sealed class CompanionMaterialOwner:MonoBehaviour {public Material material;public Texture2D texture;void OnDestroy(){if(material)Destroy(material);if(texture)Destroy(texture);}}
}
