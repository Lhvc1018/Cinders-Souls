using Codice.Characters;
using UnityEngine;

namespace Codice.Enemies
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        BasicEnemy owner;
        public BasicEnemy Owner => owner;
        PlayerCharacter target;
        Vector3 velocity;public Vector3 NetVelocity=>pendingContact?Vector3.zero:velocity;
        bool pendingContact;Vector3 contactPoint;
        float damage, remaining = 5; bool reflected;
        /// Parried: flies back at the shooter (or straight back), faster, and now hurts enemies only.
        public void Reflect(Vector3 point, PlayerCharacter by)
        {
            reflected = true; target = by; remaining = 3; damage *= 2.2f; float speed = velocity.magnitude * 1.6f;
            var aim = owner && owner.IsAlive ? owner.transform.position + Vector3.up * 1.2f - point : -velocity;
            velocity = aim.normalized * speed; transform.position = point + velocity.normalized * .4f;Codice.Multiplayer.LanSession.RecordLaunch(this);
            foreach (var r in GetComponentsInChildren<Renderer>()) if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", new Color(1f, .85f, .4f));
        }
        public void Launch(BasicEnemy source, PlayerCharacter player, Vector3 direction, float speed, float amount)
        { owner = source; target = player; velocity = direction.normalized * speed; damage = amount; Codice.Multiplayer.LanSession.RecordLaunch(this); }
        internal void ResolveContact(PlayerCharacter player,DamageInfo hit,Codice.Multiplayer.DefenseTiming timing)
        {
            pendingContact=false;
            if(player.State.IsAlive&&timing>=Codice.Multiplayer.DefenseTiming.Deflect){
                var guard=player.GetComponent<ShinobiGuard>();
                if(player.State.Class==PlayerClass.Shinobi&&guard){
                    guard.SetContactTiming(timing);guard.ConsumePerfectForProjectile();
                }
                Parry.Feedback(player,contactPoint,velocity.normalized,true);
                Reflect(contactPoint,player);return;
            }
            player.ResolveOwnedDefense(hit,timing==Codice.Multiplayer.DefenseTiming.Roll?timing:Codice.Multiplayer.DefenseTiming.Open);
            GetComponent<Codice.Presentation.SpellFlightVisual>()?.Impact(contactPoint);
            Destroy(gameObject);
        }
        void Update()
        {
            if(pendingContact)return;
            if (!reflected && (!owner||!owner.IsAlive||!target || !target.State.IsAlive)) { Destroy(gameObject); return; }
            if (!reflected && owner && owner.Paused) return;
            float dt = Time.deltaTime;
            remaining -= dt;
            if (remaining <= 0) { Destroy(gameObject); return; }
            Vector3 step = velocity * dt;
            if (reflected)
            {
                // Enemy bodies use trigger colliders: look for any enemy along this step (triggers included).
                foreach (var col in Physics.OverlapCapsule(transform.position, transform.position + step, .45f, ~0, QueryTriggerInteraction.Collide))
                {
                    var victim = col.GetComponentInParent<IDamageReceiver>() as Component;
                    if (!Codice.Mechanics.CombatTargets.IsEnemy(victim)) continue;
                    if (target) Codice.Mechanics.CombatTargets.Hit(victim, target, damage); Codice.Mechanics.CombatTargets.Stun(victim, .6f); Codice.Mechanics.EnemyPosture.Add(victim, 25);
                    var point = col.ClosestPoint(transform.position); Codice.Presentation.PresentationVfx.Impact(point, -velocity.normalized); GetComponent<Codice.Presentation.SpellFlightVisual>()?.Impact(point);
                    Destroy(gameObject); return;
                }
            }
            var hits = Physics.SphereCastAll(transform.position, .08f, velocity.normalized, step.magnitude, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform) || (!reflected && owner && hit.transform.IsChildOf(owner.transform))) continue;
                var player = hit.collider.GetComponentInParent<PlayerCharacter>();
                if (reflected)
                {
                    if (player) continue;   // a returned bolt never hurts players
                    if (hit.collider.isTrigger) continue;
                    var victim = hit.collider.GetComponentInParent<IDamageReceiver>() as Component;
                    if (Codice.Mechanics.CombatTargets.IsEnemy(victim)) { if (target) Codice.Mechanics.CombatTargets.Hit(victim, target, damage); Codice.Mechanics.CombatTargets.Stun(victim, .6f); Codice.Mechanics.EnemyPosture.Add(victim, 25); }
                    GetComponent<Codice.Presentation.SpellFlightVisual>()?.Impact(hit.point); Codice.Presentation.PresentationVfx.Impact(hit.point, hit.normal); Destroy(gameObject); return;
                }
                if(player&&Codice.Multiplayer.LanSession.Host&&Codice.Multiplayer.LanAvatar.IsRemote(player)){
                    var damageEvent=new DamageInfo{amount=damage,element=gameObject.name.Contains("flame")?Element.Burn:Element.Shock,
                        buildup=35,direction=velocity.normalized,source=owner?owner.gameObject:gameObject};
                    if(Codice.Multiplayer.LanSession.Instance.AskDefense(player,damageEvent,this)){
                        pendingContact=true;contactPoint=hit.point;transform.position=hit.point;return;
                    }
                }
                if (player && !Codice.Multiplayer.LanSession.Client && Parry.TryDeflect(player, velocity, hit.point)) { Reflect(hit.point, player); return; }
                if (player) player.ReceiveDamage(new DamageInfo { amount = damage, element=gameObject.name.Contains("flame")?Element.Burn:Element.Shock,buildup=35, direction = velocity.normalized, source = owner ? owner.gameObject : gameObject });
                var pillar = hit.collider.GetComponentInParent<Codice.Mechanics.DestructiblePillar>();
                if (pillar) pillar.ReceiveDamage(new DamageInfo { amount = damage });
                GetComponent<Codice.Presentation.SpellFlightVisual>()?.Impact(hit.point);
                Codice.Presentation.PresentationVfx.Impact(hit.point,hit.normal);
                Destroy(gameObject); return;
            }
            transform.position += step;
        }
    }
}
