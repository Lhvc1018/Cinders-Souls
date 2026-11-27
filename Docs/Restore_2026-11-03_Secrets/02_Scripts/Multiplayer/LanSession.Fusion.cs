using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Bosses;
using Codice.Characters;
using Codice.Mechanics;
using UnityEngine;
namespace Codice.Multiplayer
{
    public sealed partial class LanSession
    {
        public bool useFusion=true;
        FusionCoopTransport fusion;
        public int DungeonSeed {get;private set;}
        FusionCoopTransport NewFusion()
        {
            fusion=new GameObject("Photon Fusion 2 · transport").AddComponent<FusionCoopTransport>();DontDestroyOnLoad(fusion.gameObject);
            fusion.Connected+=wire=>{if(started||peers.Count>=3){wire.Dispose();return;}peers.Add(new Peer{wire=wire,id=nextId++,joined=Time.realtimeSinceStartup,lastSeen=Time.realtimeSinceStartup});};
            fusion.Closed+=message=>{if(this&&!leaving){status=message;if(client)Disconnect(message);}};return fusion;
        }
        async void CreateFusionHost()
        {
            if(connecting||host)return;connecting=true;status="Conectando con Photon…";address="CINDERS-"+Guid.NewGuid().ToString("N").Substring(0,6).ToUpperInvariant();
            try{await NewFusion().StartSession(true,address);if(!this||leaving)return;host=true;localId=0;RefreshMembers();status="Comparte el código de sala. Después inicia el descenso.";}
            catch(Exception error){status=error.Message;host=false;}
            finally{if(this)connecting=false;}
        }
        void AddExpeditionEntities(List<LanEntity> entities)
        {
            foreach(var entity in entities)if(entity.key=="Chest"){var chest=FindObjectsByType<TreasureChest>().FirstOrDefault(c=>(c.transform.position-entity.position).sqrMagnitude<.01f);if(chest)entity.kind=chest.expeditionRoom;}
            foreach(var pool in FindObjectsByType<InquisitorBloodPool>())entities.Add(new LanEntity{id=Id(pool),key="BloodPool",position=pool.transform.position,amount=Mathf.RoundToInt(pool.Radius*100),available=pool.Active});
            foreach(var bolt in FindObjectsByType<InquisitorBolt>())entities.Add(new LanEntity{id=Id(bolt),key="BloodBolt",position=bolt.transform.position,rotation=bolt.transform.rotation});
            foreach(var altar in FindObjectsByType<RelicAltar>())if(!altar.Claimed)entities.Add(new LanEntity{id=Id(altar),key="Altar",position=altar.transform.position,kind=altar.Seed,available=altar.ActiveReward});
        }
        public void ClaimRelic(int id,int choice,int slot){Send(new LanMessage{type="input",scene=loadedScene,yaw=clientYaw,pitch=clientPitch,altar=id,choice=choice,replace=slot});}
    }
}
