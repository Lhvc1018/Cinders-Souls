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
        /// Development probes only: fixed room code so a second process can join without typing.
        public static string ForcedRoomCode;
        FusionCoopTransport fusion;
        /// The code players type is just 5 digits; the Photon session behind it is namespaced.
        public static string SessionFor(string code)
        {
            code=(code??"").Trim().ToUpperInvariant();
            if(code.StartsWith("PROBE-")||code.StartsWith("CINDERS-"))return code;
            return "CINDERS-"+code.Replace(" ","");
        }
        static string NewRoomCode()
        {
            const string consonants="BCDFGLMNPRST";
            const string vowels="AEIOU";
            var code=new char[6];
            for(int i=0;i<code.Length;i+=2)
            {
                code[i]=consonants[UnityEngine.Random.Range(0,consonants.Length)];
                code[i+1]=vowels[UnityEngine.Random.Range(0,vowels.Length)];
            }
            return new string(code);
        }
        public int DungeonSeed {get;private set;}
        FusionCoopTransport NewFusion()
        {
            fusion=new GameObject("Photon Fusion 2 · transport").AddComponent<FusionCoopTransport>();DontDestroyOnLoad(fusion.gameObject);
            fusion.Connected+=wire=>{if(peers.Count>=3){wire.Dispose();return;}peers.Add(new Peer{wire=wire,id=nextId++,joined=Time.realtimeSinceStartup,lastSeen=Time.realtimeSinceStartup});};
            fusion.Closed+=message=>{if(this&&!leaving){status=message;if(client)Disconnect(message);}};return fusion;
        }
        async void CreateFusionHost()
        {
            if(connecting||host)return;connecting=true;status="Conectando al servidor…";address=!string.IsNullOrEmpty(ForcedRoomCode)?ForcedRoomCode:NewRoomCode();
            try{await NewFusion().StartSession(true,SessionFor(address));if(!this||leaving)return;host=true;localId=0;RefreshMembers();status="Comparte el código de sala. Después inicia el descenso.";}
            catch(Exception error){status=error.Message;host=false;}
            finally{if(this)connecting=false;}
        }
        void AddExpeditionEntities(List<LanEntity> entities)
        {
            foreach(var pool in FindObjectsByType<InquisitorBloodPool>())entities.Add(new LanEntity{id=Id(pool),key="BloodPool",position=pool.transform.position,amount=Mathf.RoundToInt(pool.Radius*100),available=pool.Active});
            foreach(var bolt in FindObjectsByType<InquisitorBolt>())entities.Add(new LanEntity{id=Id(bolt),key="BloodBolt",end=bolt.NetVelocity,position=bolt.transform.position,rotation=bolt.transform.rotation});
            AddLivingEntities(entities);
            foreach(var altar in FindObjectsByType<RelicAltar>())if(!altar.Claimed)entities.Add(new LanEntity{id=Id(altar),key="Altar",position=altar.transform.position,kind=altar.Seed,available=altar.ActiveReward});
        }
        public void ClaimRelic(int id,int choice,int slot){Send(new LanMessage{type="input",scene=loadedScene,yaw=clientYaw,pitch=clientPitch,altar=id,choice=choice,replace=slot});}
    }
}
