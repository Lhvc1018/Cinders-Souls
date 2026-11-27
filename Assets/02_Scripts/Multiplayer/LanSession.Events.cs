using System.Collections.Generic;
using Codice.Characters;
using Codice.Enemies;
using Codice.Bosses;
using Codice.Presentation;
using UnityEngine;

namespace Codice.Multiplayer
{
    public sealed partial class LanSession
    {
        /* Keep every newly launched effect through at least two outgoing frames, even on a point-blank hit. */
        readonly Dictionary<int,LanEntity> births=new Dictionary<int,LanEntity>();
        readonly List<int> expiredBirths=new List<int>();
        public static void RecordLaunch(Component source)
        {
            var session=Instance;
            if(!Host||!source||!session.context||session.peers.Count==0)return;
            var item=new LanEntity{id=session.Id(source),position=source.transform.position,rotation=source.transform.rotation,
                flightTime=Time.unscaledTime};
            switch(source){
                case EnemyProjectile enemy:
                    item.key="Shot";item.end=enemy.NetVelocity;
                    item.kind=(int)(enemy.GetComponent<SpellFlightVisual>()?.Style??ProjectileStyle.Arcane);break;
                case PlayerProjectile shot:item.key="Projectile:"+shot.NetworkVisual;item.end=shot.NetVelocity;break;
                case RunSpellProjectile spell:item.key="Projectile:Spell";item.end=spell.NetVelocity;
                    item.kind=(int)(spell.GetComponent<SpellFlightVisual>()?.Style??ProjectileStyle.Arcane);break;
                case InquisitorBolt bolt:item.key="BloodBolt";item.end=bolt.NetVelocity;break;
                case LineRenderer line:
                    if(!line.useWorldSpace||line.positionCount<2)return;
                    item.key="Line";item.points=SampleLine(line);item.color=line.startColor;
                    item.amount=Mathf.RoundToInt(line.startWidth*1000);item.available=line.loop;break;
                default:return;
            }
            session.births[item.id]=item;
        }
        public static void ForgetLocalRing(LineRenderer line)
        {
            if(Host&&Instance.objectIds.TryGetValue(line,out var id))Instance.births.Remove(id);
        }
        void IncludeBirths(List<LanEntity> entities)
        {
            if(births.Count==0)return;
            expiredBirths.Clear();
            foreach(var pair in births)
            {
                var birth=pair.Value;
                int index=entities.FindIndex(e=>e.id==pair.Key);
                if(index<0)entities.Add(birth);
                else if(birth.key!="Line"&&entities[index].end==birth.end)entities[index]=birth;
                if(Time.unscaledTime-birth.flightTime>=.12f)expiredBirths.Add(pair.Key);
            }
            foreach(int id in expiredBirths)births.Remove(id);
        }
    }
}
