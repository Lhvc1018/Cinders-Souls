using System;
using UnityEngine;
namespace Codice.Multiplayer
{
    [Serializable] public sealed partial class LanMessage
    {
        public string type,version,name,scene,state,error;
        public int id,playerClass,level,phase,kills,defeated,total,slot=-1,spell=-1,passive=-1,buy=-1;
        public int seed,room,relic=-1,floor=1,worldRevision;public bool[] visited,cleared;public bool encounter,flask,secretRevealed;
        public int altar=-1,choice=-1,replace=-1;
        public int memories,forge=-1;
        /// Guest inventory commands (host is authoritative over CharacterState): equip talisman `talisman` (-1 = remove) in `talismanSlot`.
        public int talismanSlot=-1,talisman=-1;
        /// Learned scrolls (RunSpellbook): guest -> host selection + cast request; host -> guest list in its Player entity.
        public int spellPick=-1;public bool castSpell;
        /// Snapshot flow control: host numbers each world snapshot (seq), the guest echoes the last one it applied (ack).
        public int seq,ack;
        public int interactEntity=-1;
        public float sampleTime,elapsed,roundTime,yaw,pitch;
        public Vector3 point; public float scale,lifetime; public Color tint;
        /// Guests own their movement: they report where they are and the host follows (no rubber-banding on internet latency).
        public Vector3 pos; public bool hasPos;
        public Vector2 move; public bool attack,block,jump,dash,special,reload,interact,closeShop,continueRound,heavy;
        public LanEntity[] entities; public LanMember[] members;
        public Codice.Characters.MerchantOffer[] offers;public bool shop,portal;public int customer=-1;
    }
    [Serializable] public sealed class LanMember { public int id,playerClass; public string name; public int pingMs=-1; }
    [Serializable] public sealed class LanEntity
    {
        public Vector3Int vitalVersion;public float manaDelay;public int id,owner=-1;public string key,animation,state,name;public float animDur,animLeft;public Vector3[] points;public Color color;   /* key "Line": telegraph / warning lines */public bool forced;   /* host is moving this player itself (Iaido dash): the guest follows */
        public Vector3 end;
        public Vector3 position;public Quaternion rotation=Quaternion.identity;public float health,maxHealth;
        public Codice.Characters.WeaponInstance weapon;
        public int kind,amount,passive; public bool available;
        public LanEntity Copy()=>(LanEntity)MemberwiseClone(); public bool keepPose; public float armor,mana;public int flasks;public Vector3 hitCenter,hitSize;public float flightTime; public int[] spells; public int spellSel;
    }
}
