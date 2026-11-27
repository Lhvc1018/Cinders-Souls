#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Codice.Mechanics;
namespace Codice.Multiplayer
{
    public sealed class CoopLatencyProbe : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        string report, room; bool host; int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-coop-latency");
            if (i < 0 || i + 3 >= a.Length) return;
            var p = new GameObject("Four player latency probe").AddComponent<CoopLatencyProbe>();
            p.host = a[i + 1] == "host"; p.room = a[i + 2]; p.report = a[i + 3]; DontDestroyOnLoad(p.gameObject);
        }
        void Write(string s) => File.AppendAllText(report, s + "\n");
        void Log(string m, string trace, LogType t) { if (t == LogType.Exception || t == LogType.Error) { errors++; Write("ERROR " + m); } }
        static T Field<T>(LanSession s, string name) => (T)typeof(LanSession).GetField(name, Fields).GetValue(s);
        static void CheckFragments()
        {
            var receiver=new CoopPoseFrames();
            var full=new byte[CoopPoseFrames.ChunkSize];
            var end=new byte[]{0x2A};
            if(receiver.Push(1,0,2,full,0)!=null||receiver.Push(2,1,2,end,.01f)!=null||
               receiver.Push(2,1,2,end,.02f)!=null)throw new InvalidDataException("Incomplete or duplicated fragment emitted a frame");
            var completed=receiver.Push(2,0,2,full,.03f);
            if(completed==null||completed.Length!=401||completed[400]!=0x2A||receiver.Push(1,1,2,end,.04f)!=null)
                throw new InvalidDataException("Reordered fragments or lost predecessor corrupted a frame");
            if(receiver.Push(3,0,2,full,.05f)!=null||receiver.Push(3,1,2,end,.7f)!=null||
               receiver.Push(4,0,1,end,.71f)==null)throw new InvalidDataException("Expired frame blocked a successor");
        }
        IEnumerator Start()
        {
            File.WriteAllText(report, "START " + (host ? "host" : "guest") + " delay=" + CoopNetworkSimulation.Arg("-probe-delay") + " loss=" + CoopNetworkSimulation.Arg("-probe-loss") + "\n");
            CheckFragments();
            Write("PASS_FRAGMENT_REASSEMBLY");
            Application.logMessageReceived += Log; Application.runInBackground = true;
            yield return new WaitForSecondsRealtime(host ? 1 : 8);
            LanSession.Open(); var s = LanSession.Instance; bool lan=Array.IndexOf(Environment.GetCommandLineArgs(),"-probe-lan")>=0; s.useFusion = !lan;
            typeof(LanSession).GetField("nickname", Fields).SetValue(s, Path.GetFileNameWithoutExtension(report));
            if (host) { LanSession.ForcedRoomCode = room; s.CreateHost(); }
            else { typeof(LanSession).GetField("address", Fields).SetValue(s, lan ? "127.0.0.1" : room); s.Join(); }
            float until = Time.realtimeSinceStartup + 100; bool started = false;
            while (Time.realtimeSinceStartup < until && s && !(RunDirector.Instance && RunDirector.Instance.Player))
            {
                if (host && !started && Field<LanMember[]>(s, "members").Length == 4) { started = true; Write("LOBBY 4"); s.StartMatch(); }
                yield return null;
            }
            if (!s || !RunDirector.Instance || !RunDirector.Instance.Player) { Write("FAIL connect " + (s ? Field<string>(s, "status") : "disconnected")); Application.Quit(); yield break; }
            Write("ENTERED seed=" + s.DungeonSeed + " floor=" + s.FloorNumber);
            float begin = Time.realtimeSinceStartup, sample = begin + 8, lastReceive = sample, maxGap = 0;
            int stallMs=Mathf.Clamp(CoopNetworkSimulation.Arg("-probe-hitch"),0,1000);
            float nextStall=begin+10;
            Transform observed=null;
            Vector3 previousPosition=default;
            float previousTime=0,peakSpeed=0;
            var speeds=new List<float>();
            int frames = 0, updates = 0, lastSeq = 0, maxPlayers = 0; var gaps = new List<float>();
            while (s && Time.realtimeSinceStartup - begin < 50)
            {
                float now = Time.realtimeSinceStartup;
                if(!host && stallMs>0 && now>=nextStall){nextStall=now+5;System.Threading.Thread.Sleep(stallMs);}
                var run = RunDirector.Instance;
                if (host) foreach (var player in s.Players()) player.State.LaboratoryImmortal = true;
                if (run && run.Player)
                {
                    var cc = run.Player.GetComponent<CharacterController>();
                    int leg = (int)((now - begin) / 2) % 4;
                    var direction = leg == 0 ? Vector3.right : leg == 1 ? Vector3.forward : leg == 2 ? Vector3.left : Vector3.back;
                    if (cc && cc.enabled) cc.Move(direction * (2 * Time.deltaTime));
                }
                if (now >= sample)
                {
                    frames++;
                    if(!observed)
                    {
                        if(host)observed=s.Players().FirstOrDefault(p=>p!=run.Player)?.transform;
                        else observed=FindObjectsByType<LanGhost>(FindObjectsSortMode.None).FirstOrDefault(g=>g.snapshot?.owner==0)?.transform;
                        previousTime=0;
                    }
                    if(observed)
                    {
                        var position=observed.position;
                        if(previousTime>0&&now>previousTime)
                        {
                            var delta=position-previousPosition;
                            delta.y=0;
                            float speed=delta.magnitude/(now-previousTime);
                            speeds.Add(speed);
                            peakSpeed=Mathf.Max(peakSpeed,speed);
                        }
                        previousPosition=position;
                        previousTime=now;
                    }
                    maxPlayers = Math.Max(maxPlayers, host ? s.Players().Count() : FindObjectsByType<LanGhost>(FindObjectsSortMode.None).Count(g => g.snapshot?.key?.StartsWith("Player:") == true));
                    if (!host)
                    {
                        int seq = s.LatestPoseSequence;
                        if (seq != lastSeq) { if (lastSeq > 0) { float gap = now - lastReceive; gaps.Add(gap); maxGap = Mathf.Max(maxGap, gap); } updates++; lastSeq = seq; lastReceive = now; }
                    }
                }
                yield return null;
            }
            speeds.Sort();
            if(speeds.Count>0)Write($"REMOTE planar speed p95={speeds[(int)(speeds.Count*.95f)]:F2} peak={peakSpeed:F2} m/s; commanded=2 m/s");
            gaps.Sort(); float seconds = Mathf.Max(1, Time.realtimeSinceStartup - sample);
            float p95 = gaps.Count > 0 ? gaps[Mathf.Min(gaps.Count - 1, (int)(gaps.Count * .95f))] * 1000 : 0;
            Write($"RESULT connected={(bool)s} players={maxPlayers} fps={frames / seconds:F1} snapshotsHz={updates / seconds:F1} gapP95Ms={p95:F0} maxGapMs={maxGap * 1000:F0} dropped={CoopNetworkSimulation.Dropped} errors={errors} ackMs={LanSession.AckRtt * 1000:F0}");
            Write(s && Field<bool>(s, host ? "host" : "client") && maxPlayers >= (host ? 4 : 3) && errors == 0 && (host || (updates / seconds >= 16 && updates / seconds <= 21 && p95 < 200)) ? "PASS" : "FAIL");
            yield return new WaitForSecondsRealtime(host ? 5 : 1); Application.Quit();
        }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
#endif
