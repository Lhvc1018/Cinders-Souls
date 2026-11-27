from pathlib import Path
import shutil
r=Path('Assets/02_Scripts/Multiplayer');backup=Path('D:/67/CindersBackups/NetLatency-20261121');backup.mkdir(parents=True,exist_ok=True)
for f in ['LanSession.cs','FusionCoopTransport.cs']:shutil.copy2(r/f,backup/f)
p=r/'LanSession.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('public LanWire wire;','public float snapshotRtt=.1f;\n            public LanWire wire;',1)
s=s.replace('if(sent>0)AckRtt=Mathf.Lerp(AckRtt,Time.realtimeSinceStartup-sent,.2f);','if(sent>0){peer.snapshotRtt=Mathf.Lerp(peer.snapshotRtt,Time.realtimeSinceStartup-sent,.35f);AckRtt=Mathf.Lerp(AckRtt,peer.snapshotRtt,.2f);}')
s=s.replace('            var director=RunDirector.Instance;var entities=new List<LanEntity>();','            float broadcastNow=Time.realtimeSinceStartup;bool receiverReady=false;foreach(var peer in peers)if(peer.accepted&&SnapshotReady(peer,broadcastNow)){receiverReady=true;break;}if(!receiverReady)return;\n            var director=RunDirector.Instance;var entities=new List<LanEntity>();')
s=s.replace('if(peer.sentSeq-peer.ackSeq>=2&&now-peer.sentAt<1.5f)continue;','if(!SnapshotReady(peer,now))continue;')
idx=s.index('        void Broadcast()');s=s[:idx]+'''        public static int SnapshotWindow(float roundTrip)=>Mathf.Clamp(Mathf.CeilToInt((Mathf.Clamp(roundTrip,0,1)+.034f)*30),2,8);
        static bool SnapshotReady(Peer peer,float now)=>peer.sentSeq-peer.ackSeq<SnapshotWindow(peer.snapshotRtt)||now-peer.sentAt>=1.5f;
'''+s[idx:];p.write_text(s,encoding='utf-8')
p=r/'FusionCoopTransport.cs';s=p.read_text(encoding='utf-8-sig').replace('        public string LinkInfo()','        float linkRefresh;string cachedLink;\n        public string LinkInfo()');s=s.replace('if(!Runner||!Runner.IsRunning||!IsHost)return null;var parts=new List<string>();','if(!Runner||!Runner.IsRunning||!IsHost)return null;if(Time.unscaledTime<linkRefresh)return cachedLink;linkRefresh=Time.unscaledTime+.5f;var parts=new List<string>();');s=s.replace('return parts.Count>0?string.Join(" · ",parts):null;','cachedLink=parts.Count>0?string.Join(" · ",parts):null;return cachedLink;');p.write_text(s,encoding='utf-8')
