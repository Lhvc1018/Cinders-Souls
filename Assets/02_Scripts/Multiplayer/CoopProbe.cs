#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Codice.Mechanics;
using UnityEngine;
namespace Codice.Multiplayer
{
    // Development-build check of a real two-process Photon Fusion 2 session:
    //   Game.exe -coop-probe host <ROOM> <report.txt>     Game.exe -coop-probe client <ROOM> <report.txt>
    // The host walks and attacks; the client reports whether it sees that body, its movement and its animation.
    public sealed partial class CoopProbe : MonoBehaviour
    {
        Codice.Enemies.BasicEnemy contactTarget;
        bool confirmedMelee;
        string report, room; bool host; string error;
        static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "-coop-probe");
#if UNITY_EDITOR
            string editorRoom=UnityEditor.SessionState.GetString("Cinders.CoopProbe.Room","");
            if(flag<0&&!string.IsNullOrEmpty(editorRoom)){
                UnityEditor.SessionState.EraseString("Cinders.CoopProbe.Room");
                args=new[]{"-coop-probe","host",editorRoom,Path.GetFullPath("AgentScripts/_bridge/coop8-editor.txt")};flag=0;
            }
#endif
            if (flag < 0 || flag + 3 >= args.Length) return;
            var probe = new GameObject("Coop probe").AddComponent<CoopProbe>(); probe.host = args[flag + 1] == "host"; probe.room = args[flag + 2]; probe.report = args[flag + 3]; DontDestroyOnLoad(probe.gameObject);
        }
        void Write(string line) { File.AppendAllText(report, line + "\n"); }
        readonly Dictionary<string,int> logCounts = new Dictionary<string,int>(); int frames0; float fpsAt;
        string lastWarning = "";
        void Log(string message, string trace, LogType type) { if (type == LogType.Warning && message.StartsWith("LAN")) lastWarning = message + " @ " + (trace ?? "").Replace("\n", " | "); var k = type + ": " + (message.Length > 70 ? message.Substring(0, 70) : message); logCounts[k] = logCounts.TryGetValue(k, out var n) ? n + 1 : 1; if (type == LogType.Exception || type == LogType.Error) { error = message; Write("ERROR " + message + " @ " + (trace ?? "").Replace("\n", " | ").Substring(0, System.Math.Min(600, (trace ?? "").Length))); } }
        static bool Interacted(Codice.Mechanics.RelicAltar a, Codice.Characters.PlayerCharacter p) { a.Interact(p); Codice.UI.RunHUD.Instance?.SetPaused(false); return true; }
        static string Sig()
        {
            var parts = new List<string>();
            void Add<T>(string label) where T : Component { var all = FindObjectsByType<T>(FindObjectsSortMode.None); float h = 0; foreach (var c in all) h += c.transform.position.x * 1.3f + c.transform.position.z * .7f; parts.Add(label + "=" + all.Length + "@" + h.ToString("F0")); }
            Add<Codice.Mechanics.TreasureChest>("chest"); Add<Codice.Expedition.Blacksmith>("forge"); Add<Codice.Mechanics.EstusShrine>("shrine"); Add<Codice.Expedition.BlessingStatue>("statue"); Add<Codice.Expedition.DescentEncounter>("encounter"); Add<Codice.Mechanics.RelicAltar>("relic"); Add<Codice.Expedition.MerchantDisplay>("merchant"); Add<Codice.Expedition.DescentPortal>("portal");
            var floor = RunDirector.Instance && RunDirector.Instance.Expedition ? RunDirector.Instance.Expedition : null;
            return string.Join(" ", parts) + (floor ? " rooms=" + floor.RoomCount + " boss=" + Codice.Expedition.ExpeditionFloor.BossResource(LanSession.Instance.DungeonSeed, RunDirector.Instance.FloorNumber) : " noFloor");
        }
        IEnumerator Floors(LanSession session)
        {
            int[] floors = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            if (host)
            {
                yield return new WaitForSecondsRealtime(8);
                Write("HOST floor 1 " + Sig());
                foreach (int f in floors)
                {
                    RunDirector.Instance.DevJumpFloor(f); float until = Time.realtimeSinceStartup + 20;
                    yield return new WaitForSecondsRealtime(1);
                    while (Time.realtimeSinceStartup < until && !(RunDirector.Instance && RunDirector.Instance.Player && RunDirector.Instance.FloorNumber == f && RunDirector.Instance.Phase != RunPhase.Travelling)) yield return null;
                    yield return new WaitForSecondsRealtime(5);
                    var run = RunDirector.Instance; if (!run || !run.Player) { Write("HOST floor " + f + " NOPLAYER"); continue; }
                    foreach (var p in session.Players()) p.State.LaboratoryImmortal = true;
                    Write("HOST floor " + f + " " + Sig());
                    if (f == 2) { var gst = session.Players().FirstOrDefault(p => p != run.Player); if (gst) { string before = gst.State.Capture(); var altar = Codice.Mechanics.RelicAltar.Create(gst.transform.position + gst.transform.forward * 1.5f, 1234, true); yield return new WaitForSecondsRealtime(6); Write("HOST relic claimed=" + (!altar) + " stateChanged=" + (gst.State.Capture() != before) + " relic0=" + gst.State.GetRelic(0) + " passives=" + gst.State.PassiveCount); } }
                    var prefab = Resources.Load<Codice.Bosses.BossCharacter>(Codice.Expedition.ExpeditionFloor.BossResource(session.DungeonSeed, f));
                    Codice.Bosses.BossCharacter boss = null; string err = "";
                    try { if (prefab) { var at = run.Player.transform.position + run.Player.transform.forward * 7; if (UnityEngine.AI.NavMesh.SamplePosition(at, out var nh, 6, UnityEngine.AI.NavMesh.AllAreas)) at = nh.position; boss = Instantiate(prefab, at, Quaternion.LookRotation(-run.Player.transform.forward)); boss.Initialize(run.Player, 1); } } catch (Exception e) { err = e.GetType().Name + ":" + e.Message; }
                    var seen = new HashSet<string>(); int maxLines = 0; float stop = Time.realtimeSinceStartup + 9;
                    while (Time.realtimeSinceStartup < stop) { if (boss) { var ap = boss.GetComponent<Codice.Presentation.ActorPresentation>(); if (ap) seen.Add(ap.CurrentState); } Codice.Bosses.BloodVisual.Live.RemoveAll(l => !l); maxLines = Mathf.Max(maxLines, Codice.Bosses.BloodVisual.Live.Count); yield return null; }
                    Write($"HOST floor {f} boss={(prefab ? prefab.name : "MISSING")} alive={(boss ? boss.IsAlive : false)} err={err} states={string.Join(",", seen)} warnLines={maxLines}");
                    if (boss) Destroy(boss.gameObject); yield return new WaitForSecondsRealtime(1.5f);
                }
                Write("HOST_FLOORS_DONE");
            }
            else
            {
                int last = -1; float settleAt = 0; bool sigDone = false, relicTried = false; var seen = new HashSet<string>(); int maxLines = 0, maxBoss = 0; string bossModel = ""; float quitAt = Time.realtimeSinceStartup + 400;
                while (Time.realtimeSinceStartup < quitAt)
                {
                    if (!session) { Write("CLIENT DISCONNECTED"); yield break; }
                    var run = RunDirector.Instance; int f = session.FloorNumber;
                    if (f != last) { if (last > 0) Write($"CLIENT floor {last} bossGhosts={maxBoss} model={bossModel} states={string.Join(",", seen)} warnLines={maxLines}"); last = f; settleAt = Time.realtimeSinceStartup + 6; sigDone = false; seen.Clear(); maxLines = 0; maxBoss = 0; bossModel = ""; }
                    if (!sigDone && Time.realtimeSinceStartup > settleAt && run && run.Player) { sigDone = true; Write("CLIENT floor " + f + " " + Sig()); }
                    if (f == 2 && !relicTried) { var alt = FindObjectsByType<LanGhost>(FindObjectsSortMode.None).FirstOrDefault(g => g.snapshot != null && g.snapshot.key == "Altar"); if (alt && run && run.Player) { relicTried = true; var ra = alt.GetComponent<Codice.Mechanics.RelicAltar>(); Write("CLIENT relic altar seen dist=" + Vector3.Distance(alt.transform.position, run.Player.transform.position).ToString("F1") + " claimSent=" + (ra && Interacted(ra, run.Player) && ra.Claim(run.Player, 0, 0))); } }
                    foreach (var g in FindObjectsByType<LanGhost>(FindObjectsSortMode.None)) { if (g.snapshot == null || g.snapshot.key == null) continue; if (g.snapshot.key.StartsWith("Boss:")) { maxBoss = Mathf.Max(maxBoss, 1); bossModel = g.name + "/r" + g.GetComponentsInChildren<Renderer>().Length + "/src=" + (LanSession.VisualSource.TryGetValue(g.snapshot.id, out var src) ? src : "?"); var an = g.GetComponentInChildren<Animator>(); if (an && an.runtimeAnimatorController) seen.Add(g.snapshot.animation); } }
                    maxLines = Mathf.Max(maxLines, FindObjectsByType<LanGhost>(FindObjectsSortMode.None).Count(g => g.snapshot != null && g.snapshot.key == "Line"));
                    if (f == 15 && sigDone && Time.realtimeSinceStartup > settleAt + 12) { Write($"CLIENT floor {f} bossGhosts={maxBoss} model={bossModel} states={string.Join(",", seen)} warnLines={maxLines}"); Write("CLIENT_FLOORS_DONE"); foreach (var kv in logCounts.Where(k => !k.Key.StartsWith("Log")).Take(12)) Write("CLIENT log x" + kv.Value + " " + kv.Key); yield break; }
                    yield return null;
                }
            }
            foreach (var kv in logCounts.Where(k => !k.Key.StartsWith("Log")).Take(12)) Write((host ? "HOST" : "CLIENT") + " log x" + kv.Value + " " + kv.Key);
        }
        static int ProbeGuestClass() { var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-guest-class"); return i >= 0 && i + 1 < a.Length && int.TryParse(a[i + 1], out var c) ? c : 0; }
        readonly HashSet<string> seenStates = new HashSet<string>(), seenClips = new HashSet<string>(); float sampleAt; int deathStep, cDeath, flaskStep, cFlask, changedCount, camN; bool jitterHooked, jitterLogged; float camMax, camSum, plMax; Vector3 lastCam, lastPl; ProbeTarget dummy; float firstSwing = -1, lastSwing; bool cloned; bool sfxSent; bool dummyLogged; int frames1; float fps1At;
        string Clip(Animator a) { if (!a) return "noAnimator"; var c = a.GetCurrentAnimatorClipInfo(0); return c.Length > 0 && c[0].clip ? c[0].clip.name : "noClip"; }
        int Blades(Component c) => c.GetComponentsInChildren<Renderer>().Count(r => r.enabled && r.gameObject.activeInHierarchy && (r.name.Contains("blade") || r.name.Contains("Blade") || r.name.Contains("Sword") || r.name.Contains("sword") || r.name.Contains("Katana") || r.name.Contains("Espada")));
        UnityEngine.InputSystem.Keyboard reviveKeyboard;
        void ProbeHoldInteract(bool held)
        {
            if(reviveKeyboard==null)reviveKeyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            var state=held?new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E):new UnityEngine.InputSystem.LowLevel.KeyboardState();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(reviveKeyboard,state);
        }
        void CheckFlaskHud(string role)
        {
            var hud=Codice.UI.RunHUD.Instance;
            var slot=hud?(RectTransform)typeof(Codice.UI.RunHUD).GetField("cleanFlaskSlot",F).GetValue(hud):null;
            var text=hud?(TMPro.TextMeshProUGUI)typeof(Codice.UI.RunHUD).GetField("cleanFlask",F).GetValue(hud):null;
            Write(role+" FLASK_HUD visible="+(slot&&slot.gameObject.activeInHierarchy)+" glyph="+(slot&&slot.GetComponentsInChildren<UnityEngine.UI.Graphic>().Length>1)+" label="+(text?text.text:"MISSING"));
        }
        UnityEngine.InputSystem.Gamepad pad; Vector3 clientStart; float nextW;
        T Field<T>(LanSession s, string name) => (T)typeof(LanSession).GetField(name, F).GetValue(s);
        IEnumerator Start()
        {
            File.WriteAllText(report, "START " + (host ? "host" : "client") + " room=" + room + "\n"); Application.logMessageReceived += Log; Application.runInBackground = true;
            yield return new WaitForSecondsRealtime(host ? 1 : 9);
            LanSession.Open(); var session = LanSession.Instance; bool lan = Array.IndexOf(Environment.GetCommandLineArgs(), "-probe-lan") >= 0; session.useFusion = !lan; if (!host && lan) { yield return new WaitForSecondsRealtime(4); room = "127.0.0.1"; }
            typeof(LanSession).GetField("nickname", F).SetValue(session, host ? "Anfitrion" : "Invitado");
            typeof(LanSession).GetField("chosenClass", F).SetValue(session, host ? 0 : ProbeGuestClass());
            if (host) { LanSession.ForcedRoomCode = room; session.CreateHost(); }
            else { typeof(LanSession).GetField("address", F).SetValue(session, room); session.Join(); }
            float end = Time.realtimeSinceStartup + (Array.IndexOf(Environment.GetCommandLineArgs(), "-probe-floors") >= 0 ? 600 : 140); bool entered = false, started = false, living = false; float gameAt = 0, lobbyAt = -1;
            bool talismanAsked = false, effectsSent = false, sawWave = false, sawGlint = false, castAsked = false; float guestMana0 = 0; float nextTrace = 0; var states = new HashSet<string>(); Vector3 first = Vector3.zero; bool haveFirst = false; float travelled = 0; int renderers = 0; bool animator = false; string ghostName = "";
            while (Time.realtimeSinceStartup < end)
            {
                if (!session) { Write("FAILED_DISCONNECTED status=" + error); foreach (var kv in logCounts.Where(k => !k.Key.StartsWith("Log")).Take(10)) Write("LOG x" + kv.Value + " " + kv.Key); Write("LASTWARN " + lastWarning); break; }
                int members = Field<LanMember[]>(session, "members").Length;
                if (!entered && members >= 2 && lobbyAt < 0) { lobbyAt = Time.realtimeSinceStartup; Write("LOBBY members=" + members + " status=" + Field<string>(session, "status")); }
                if (host && !started && lobbyAt > 0 && Time.realtimeSinceStartup - lobbyAt > 3) { started = true; session.StartMatch(); Write("STARTMATCH"); }
                var run = RunDirector.Instance;
                if (run && run.Player)
                {
                    if (!entered) { entered = true; gameAt = Time.realtimeSinceStartup; frames0 = Time.frameCount; fpsAt = Time.realtimeSinceStartup; Write("ENTERED scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " class=" + run.Player.State.Class + " party=" + CoopScaling.Party + " enemyHealth=x" + CoopScaling.EnemyHealth + " enemyDamage=x" + CoopScaling.EnemyDamage + " extra=" + CoopScaling.ExtraEnemies + " gold=x" + CoopScaling.Gold); }
                    float t = Time.realtimeSinceStartup - gameAt;
                    if (Array.IndexOf(Environment.GetCommandLineArgs(), "-probe-floors") >= 0) { yield return Floors(session); yield return new WaitForSecondsRealtime(host ? 4 : 2); Application.Quit(); yield break; }
                    if (host)
                    {
                        // Walk a 3 m square so the remote copy has movement and a locomotion state to show.
                        var cc = run.Player.GetComponent<CharacterController>();
                        if (t > 3 && t < 15 && cc && cc.enabled) { int leg = (int)((t - 3) / 1.5f) % 4; var dir = leg == 0 ? Vector3.right : leg == 1 ? Vector3.forward : leg == 2 ? Vector3.left : Vector3.back; cc.Move(dir * 2 * Time.deltaTime); }
                        if (t > 16 && !living)
                        {
                            // Living descent in coop: the guest hires both companions, a brazier appears and the host collects a permanent key.
                            living = true; var guest = session.Players().FirstOrDefault(p => p != run.Player);
                            if (guest) { guest.State.HiredPilgrim = true; guest.State.BondedWisp = true; guest.State.GrantTalisman(Codice.Characters.Talisman.AzurTear); guest.GetComponent<Codice.Characters.RunSpellbook>()?.Learn(Codice.Characters.RunSpell.Fire); guestMana0 = guest.State.Mana; }
                            Codice.Expedition.DescentEncounter.Create(run.Player.transform.position + run.Player.transform.forward * 4, run.Expedition ? run.Expedition.transform : null, Codice.Expedition.DescentOffer.Rest);
                            Codice.Expedition.RouteMemoryPickup.Create(0, run.Player.transform.position + Vector3.up * .8f, run.Expedition ? run.Expedition.transform : null);
                            Write("HOST living setup guest=" + (guest ? guest.State.Class.ToString() : "none"));
                        }
                        if (t > 6 && t < 30 && Time.realtimeSinceStartup >= sampleAt) { sampleAt = Time.realtimeSinceStartup + .2f; var g = session.Players().FirstOrDefault(p => p != run.Player); var ap = g ? g.GetComponent<Codice.Presentation.ActorPresentation>() : null; if (ap) { seenStates.Add(ap.CurrentState ?? "null"); seenClips.Add(Clip(ap.Body)); } }
                        if(t>11&&t<23&&!contactTarget)
                        {
                            var guest=session.Players().FirstOrDefault(p=>p!=run.Player);
                            if(guest)
                            {
                                var at=guest.transform.position+guest.transform.forward*1.4f;
                                var origin=guest.transform.position+Vector3.up;
                                bool clear=!Physics.RaycastAll(origin,guest.transform.forward,1.5f,~0,QueryTriggerInteraction.Ignore)
                                    .Any(h=>!h.transform.IsChildOf(guest.transform));
                                if(clear&&UnityEngine.AI.NavMesh.SamplePosition(at,out var nav,.65f,UnityEngine.AI.NavMesh.AllAreas))
                                {
                                    var prefab=Resources.Load<Codice.Enemies.BasicEnemy>("ExpeditionEnemies/Hellhound");
                                    if(prefab)
                                    {
                                        contactTarget=Instantiate(prefab,nav.position,guest.transform.rotation);
                                        contactTarget.variantName="Coop melee target";
                                        contactTarget.healthMultiplier=30;contactTarget.Initialize(guest,1);contactTarget.enabled=false;
                                        contactTarget.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
                                        Write("HOST stationary contact target at "+nav.position+" guest="+guest.transform.position);
                                    }
                                }
                            }
                        }
                        else if(t>=23&&contactTarget)
                        {
                            float dealt=contactTarget.MaxHealth-contactTarget.Health;
                            var combat=session.Players().First(p=>p!=run.Player).GetComponent<Codice.Characters.PlayerCombat>();
                            confirmedMelee=dealt>0&&combat.GuestContactsApplied>0;
                            Write($"HOST validated guest melee damage={dealt} swings={combat.GuestSwingsAccepted} reports={combat.GuestHitReports} applied={combat.GuestContactsApplied} last={combat.LastContactCheck} paused={contactTarget.Paused}");
                            Destroy(contactTarget.gameObject);contactTarget=null;
                        }
                        { var g = session.Players().FirstOrDefault(p => p != run.Player);
                          string St(Component c) { var ap = c ? c.GetComponent<Codice.Presentation.ActorPresentation>() : null; var an = c ? c.GetComponentsInChildren<Animator>().FirstOrDefault(x => x.isActiveAndEnabled && x.runtimeAnimatorController) : null; return (ap ? ap.CurrentState : "noPres") + "/" + Clip(an) + " speed=" + (an ? an.speed : -1); }
                          if (g && !sfxSent && t > 20) { sfxSent = true; Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Chest, g.transform.position); }
                          if (g && flaskStep == 0 && t > 24) { flaskStep = 1; g.State.Damage(45); Write($"HOST hurt guest hp={g.State.Health:F0} estus={g.State.EstusCharges}"); }
                          if (g && flaskStep == 1 && t > 31) { flaskStep = 2; Write($"HOST guest after flask hp={g.State.Health:F0} estus={g.State.EstusCharges} grounded={g.Grounded}"); }
                          if (g && deathStep == 0 && t > 32) { deathStep = 1; g.State.Damage(g.State.MaxHealth * 5); Write("HOST killed guest alive=" + g.State.IsAlive); }
                          if (g && deathStep == 1 && t > 34.5f) { deathStep = 2; Write("HOST sees dead guest " + St(g)); var hc = run.Player.GetComponent<CharacterController>(); hc.enabled = false; run.Player.transform.position = g.transform.position + Vector3.right * 1.2f; hc.enabled = true; ProbeHoldInteract(true); }
                          if (g && deathStep == 2 && t > 36) { deathStep = 3; ProbeHoldInteract(false); Write($"HOST revive guest alive={g.State.IsAlive} hp={g.State.Health:F0}/{g.State.MaxHealth:F0} " + St(g)); }
                          if (deathStep == 3 && t > 39) { deathStep = 4; run.Player.State.Damage(run.Player.State.MaxHealth * 5); Write("HOST killed self"); }
                          if (deathStep == 4 && t > 41) { deathStep = 5; Write("HOST own dead " + St(run.Player)); } }
                        if (t > nextW) { nextW = t + 4; var g = session.Players().FirstOrDefault(p => p != run.Player); if (g) if (t > 16 && !cloned) { cloned = true; var src = FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None); int made = 0; foreach (var e in src) for (int k = 0; k < 6 && made < 24; k++, made++) Instantiate(e.gameObject, e.transform.position + new Vector3(k * 1.5f - 4, 0, 6), e.transform.rotation); Write("HOST cloned enemies=" + made); } Write($"HOST net zip={FusionCoopTransport.LastCompressed} worldJson={LanSession.LastWorldBytes} ents={LanSession.LastEntities} ackRtt={LanSession.AckRtt*1000:F0}ms link={session.HostLinkInfo}"); Write($"HOST t={t:F0} guestW={g?.State.GetWeapon(0)?.kind}/{g?.State.GetWeapon(1)?.kind} hostW={run.Player.State.GetWeapon(0)?.kind}/{run.Player.State.GetWeapon(1)?.kind}"); }
                        if (t > 6 && frames1 == 0) { frames1 = Time.frameCount; fps1At = Time.realtimeSinceStartup; }
                        if (t > 22 && !effectsSent) { effectsSent = true; session.EmitEffect(Codice.Presentation.DeflectGlint.NetKey, run.Player.transform.position + Vector3.up * 2, 1, .3f); session.EmitEffect(Codice.Bosses.GuardianEnergy.NetWave, run.Player.transform.position, 1, 1); Write("HOST sent glint + wave"); }
                        if (t > 20 && t < 21) { var key = FindObjectsByType<Codice.Expedition.RouteMemoryPickup>(FindObjectsSortMode.None).FirstOrDefault(); if (key) { key.Interact(run.Player); Write("HOST picked key teamMask=" + Codice.Expedition.DescentMemory.TeamLearned); } }
                        if (t > 50)
                        {
                            Write("HOST encounters=" + FindObjectsByType<Codice.Expedition.DescentEncounter>(FindObjectsSortMode.None).Length + " memories=" + FindObjectsByType<Codice.Expedition.RouteMemoryPickup>(FindObjectsSortMode.None).Length);
                            Write("HOST companions=" + FindObjectsByType<Codice.Expedition.DescentCompanion>(FindObjectsSortMode.None).Length);
                            var others = session.Players().Where(p => p != run.Player).ToArray();
                            Write("HOST remotePlayers=" + others.Length + (others.Length > 0 ? " class=" + others[0].State.Class + " renderers=" + others[0].GetComponentsInChildren<Renderer>().Count(r => r.enabled) : ""));
                            foreach (var kv in logCounts.Where(k => !k.Key.StartsWith("Log")).OrderByDescending(x => x.Value).Take(8)) Write("HOST log x" + kv.Value + " " + kv.Key);
                            if (others.Length > 0)
                            {
                                float H(Component c) { var rs = c.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer) && !(r is LineRenderer) && !(r is TrailRenderer)).ToArray(); if (rs.Length == 0) return 0; var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b.size.y; }
                                Write($"HOST heights local={H(run.Player):F2}m guest={H(others[0]):F2}m guestScale={others[0].transform.lossyScale} visuals=" + string.Join(",", others[0].GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.name + ":" + r.bounds.size.y.ToString("F2")).Take(12)));
                                var cam = new GameObject("Probe cam").AddComponent<Camera>(); cam.depth = 100; cam.transform.position = others[0].transform.position + others[0].transform.forward * 3 + Vector3.up * 1.6f; cam.transform.LookAt(others[0].transform.position + Vector3.up);
                                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-probe-screens")>=0)ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(report), "coop_host_sees_guest.png"));
                            }
                            if (others.Length > 0) Write($"HOST guestSpells={others[0].GetComponent<Codice.Characters.RunSpellbook>()?.Known.Count} guestMana {guestMana0:0} -> {others[0].State.Mana:0} (cast by guest: expect lower)");
                            if (others.Length > 0) Write($"HOST guestTalismanSlot0={others[0].State.TalismanInSlot(0)} (guest asked to remove: expect -1) guestStamina={others[0].GetComponent<Codice.Characters.PlayerStamina>()?.Value:0}");
                            if (others.Length > 0) Write($"HOST guest weapons={others[0].State.GetWeapon(0)?.kind}/{others[0].State.GetWeapon(1)?.kind} dual={others[0].State.DualWield} class={others[0].State.Class} host weapons={run.Player.State.GetWeapon(0)?.kind}/{run.Player.State.GetWeapon(1)?.kind}");
                            Write("HOST sees guest states=" + string.Join(",", seenStates) + " clips=" + string.Join(",", seenClips) + (others.Length > 0 ? " guestBlades=" + Blades(others[0]) + " hostBlades=" + Blades(run.Player) : ""));
                            Write("HOST fps=" + ((Time.frameCount - frames1) / Mathf.Max(.1f, Time.realtimeSinceStartup - fps1At)).ToString("F0"));
                            CheckFlaskHud("HOST");Write(others.Length >= 1 && confirmedMelee && string.IsNullOrEmpty(error) ? "HOST_PASSED" : "HOST_FAILED"); break;
                        }
                    }
                    else
                    {
                        foreach (var g in FindObjectsByType<LanGhost>(FindObjectsSortMode.None))
                        {
                            if (g.snapshot == null || g.snapshot.key == null || !g.snapshot.key.StartsWith("Player:") ) continue;
                            ghostName = g.snapshot.key + "/" + g.name; renderers = g.GetComponentsInChildren<Renderer>().Count(r => r.enabled); animator = g.GetComponentInChildren<Animator>();
                            if (!haveFirst) { first = g.snapshot.position; haveFirst = true; }
                            travelled = Mathf.Max(travelled, Vector3.Distance(first, g.snapshot.position));
                            if (!string.IsNullOrEmpty(g.snapshot.animation)) states.Add(g.snapshot.animation);
                            break;
                        }
                        { var gh = FindObjectsByType<LanGhost>(FindObjectsSortMode.None).FirstOrDefault(x => x.snapshot != null && x.snapshot.key != null && x.snapshot.key.StartsWith("Player:"));
                          if (cFlask == 0 && t > 26.5f) { cFlask = 1; LanSession.PendingFlask = true; Write("CLIENT drinks"); }
                          if (cDeath == 0 && t > 35) { cDeath = 1; var ap = run.Player.GetComponent<Codice.Presentation.ActorPresentation>(); var an = run.Player.GetComponentsInChildren<Animator>().FirstOrDefault(x => x.isActiveAndEnabled && x.runtimeAnimatorController); Write($"CLIENT own death alive={run.Player.State.IsAlive} state={ap?.CurrentState} clip={Clip(an)} speed={(an ? an.speed : -1)}"); }
                          if (cDeath == 1 && t > 43) { cDeath = 2; var an = gh ? gh.GetComponentInChildren<Animator>() : null; Write($"CLIENT sees host death anim={gh?.snapshot.animation} hp={gh?.snapshot.health} clip={Clip(an)} speed={(an ? an.speed : -1)}"); } }
                        if (t > 23 && t < 27) { if (!jitterHooked) { jitterHooked = true; run.Player.State.Changed += () => changedCount++; } var cam = Camera.main; if (cam) { var cp = cam.transform.position; if (lastCam != Vector3.zero) { float d = (cp - lastCam).magnitude; camMax = Mathf.Max(camMax, d); camSum += d; camN++; } lastCam = cp; var pp = run.Player.transform.position; if (lastPl != Vector3.zero) plMax = Mathf.Max(plMax, (pp - lastPl).magnitude); lastPl = pp; } }
                        if (t > 27 && !jitterLogged) { jitterLogged = true; Write($"CLIENT idle jitter camMaxStep={camMax:F3} camAvg={(camN > 0 ? camSum / camN : 0):F4} playerMaxStep={plMax:F3} stateChanged/s={changedCount / 4f:F1} nearCam=" + string.Join(",", FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && Camera.main && Vector3.Distance(r.bounds.center, Camera.main.transform.position) < 2.2f && !r.transform.IsChildOf(run.Player.transform)).Select(r => r.name).Take(8))); }
                        if (t > nextTrace) { nextTrace = t + 5; var pend = Field<LanMessage>(session, "pending"); var hostEnt = pend?.entities?.FirstOrDefault(e => e.key != null && e.key.StartsWith("Player:") && e.owner != Field<int>(session, "localId")); Write($"CLIENT t={t:F0} msgAge={Time.realtimeSinceStartup - Field<float>(session, "lastServer"):F1}s ents={pend?.entities?.Length ?? -1} hostPos={(hostEnt != null ? hostEnt.position.ToString() : "none")} mySlot0={run.Player.State.TalismanInSlot(0)} loading={Codice.UI.RunNavigation.Loading}"); }
                        if (t > 5 && t < 22)
                        {
                            if (pad == null) { pad = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>(); clientStart = run.Player.transform.position; }
                            Cursor.lockState = CursorLockMode.Locked;   /* gameplay input requires a captured cursor */
                            bool atk = ((int)(t * 2)) % 2 == 0; var st = new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = new Vector2(0, t < 5.7f ? .9f : 0) };
                            if (atk && t >= 12) st = st.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.RightShoulder);
                            UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, st);
                            if (Time.realtimeSinceStartup >= sampleAt) { sampleAt = Time.realtimeSinceStartup + .2f; var ap = run.Player.GetComponent<Codice.Presentation.ActorPresentation>(); if (ap) { seenStates.Add(ap.CurrentState ?? "null"); seenClips.Add(Clip(ap.Body)); } }
                        }
                        else if (pad != null && t >= 22) { UnityEngine.InputSystem.InputSystem.RemoveDevice(pad); pad = null; Write($"CLIENT melee reports={session.ObservedMeleeReports} target={session.LastMeleeObservation}"); Write($"CLIENT moved={Vector3.Distance(clientStart, run.Player.transform.position):F2}m lock={Cursor.lockState} pads={UnityEngine.InputSystem.Gamepad.all.Count} move={Codice.Mechanics.GameInput.Move()} weapons={run.Player.State.GetWeapon(0)?.kind}/{run.Player.State.GetWeapon(1)?.kind}"); Write("CLIENT own states=" + string.Join(",", seenStates) + " clips=" + string.Join(",", seenClips) + " ownBlades=" + Blades(run.Player)); }
                        if (!sawWave && FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(x => x.name == "Gurdi · onda de energía")) { sawWave = true; Write("CLIENT saw Gurdi wave"); }
                        if (!sawGlint && FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(x => x.name == "Deflect glint")) { sawGlint = true; Write("CLIENT saw glint"); }
                        if (t > 26 && !castAsked && run.Player.GetComponent<Codice.Characters.RunSpellbook>()?.Known.Count > 0) { castAsked = true; LanSession.PendingCast = true; Write("CLIENT knows " + run.Player.GetComponent<Codice.Characters.RunSpellbook>().Known.Count + " spell(s), casting"); }
                        if (t > 24 && !talismanAsked && run.Player.State.TalismanInSlot(0) >= 0) { talismanAsked = true; LanSession.PendingTalismanSlot = 0; LanSession.PendingTalisman = -1; Write("CLIENT asks remove talisman " + run.Player.State.TalismanInSlot(0)); }
                        if (t > 44)
                        {
                            Write($"CLIENT localTalismanSlot0={run.Player.State.TalismanInSlot(0)} asked={talismanAsked} staminaFromHost={run.Player.GetComponent<Codice.Characters.PlayerStamina>()?.Value:0}");
                            var ghosts = FindObjectsByType<LanGhost>(FindObjectsSortMode.None).Where(g => g.snapshot != null && g.snapshot.key != null).ToArray();
                            int pilgrim = ghosts.Count(g => g.snapshot.key == "Companion:Pilgrim"), wisp = ghosts.Count(g => g.snapshot.key == "Companion:Wisp"), rest = ghosts.Count(g => g.snapshot.key == "Encounter:Rest" && g.GetComponent<LanInteractProxy>() && !string.IsNullOrEmpty(g.GetComponent<LanInteractProxy>().text));
                            bool learned = Codice.Expedition.DescentMemory.Owns(0);
                            Write("CLIENT ghostKeys=" + string.Join(",", ghosts.Select(g => g.snapshot.key).Distinct()) + " pendingEntities=" + (Field<LanMessage>(session, "pending")?.entities?.Length ?? -1) + " keys=" + string.Join(",", (Field<LanMessage>(session, "pending")?.entities ?? new LanEntity[0]).Select(e => e.key).Distinct()));
                            Write($"CLIENT living pilgrim={pilgrim} wisp={wisp} brazierProxy={rest} keyLearned={learned} enemyGhosts={ghosts.Count(g => g.snapshot.key.StartsWith("Enemy:"))}");
                            if (!(pilgrim == 1 && wisp == 1 && rest == 1 && learned)) error = error ?? "living descent not mirrored";
                            Write($"CLIENT ghost={ghostName} renderers={renderers} animator={animator} travelled={travelled:F2}m animations={string.Join(",", states)}");
                            bool ok = haveFirst && renderers > 0 && animator && travelled > 1 && states.Count >= 2 && string.IsNullOrEmpty(error);
                            var audio = Codice.Presentation.GameAudio.Instance; var cues = System.Enum.GetValues(typeof(Codice.Presentation.SoundCue)).Cast<Codice.Presentation.SoundCue>().Select(c => (c, n: audio ? audio.Count(c) : 0)).Where(x => x.n > 0).OrderByDescending(x => x.n).Take(6);
                            Write("CLIENT audio " + string.Join(" ", cues.Select(x => x.c + "=" + x.n)) + " overSeconds=" + (Time.realtimeSinceStartup - gameAt).ToString("F0"));
                            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-probe-screens")>=0)ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(report), "coop_client.png"));
                            Write("CLIENT fps=" + ((Time.frameCount - frames0) / Mathf.Max(.1f, Time.realtimeSinceStartup - fpsAt)).ToString("F0"));
                            foreach (var kv in logCounts.OrderByDescending(x => x.Value).Take(6)) Write("CLIENT log x" + kv.Value + " " + kv.Key);
                            CheckFlaskHud("CLIENT");Write(ok ? "CLIENT_PASSED" : "CLIENT_FAILED"); break;
                        }
                    }
                }
                yield return null;
            }
            if (!entered) Write("FAILED_TIMEOUT status=" + (session ? Field<string>(session, "status") : "no session"));
            if(entered&&!lan)yield return TestReturn(session);
            yield return new WaitForSecondsRealtime(host ? 3 : 1); Application.Quit();
        }
    }
    /// Dev build: Game.exe -options-shot <dir>  -> opens Ajustes, screenshots it, presses VOLUMEN − and logs the volume.
    public sealed class OptionsProbe : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-options-shot"); if (i < 0 || i + 1 >= a.Length) return; var p = new GameObject("Options probe").AddComponent<OptionsProbe>(); p.dir = a[i + 1]; DontDestroyOnLoad(p.gameObject); }
        string dir;
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(6);
            var log = Path.Combine(dir, "options_probe.txt"); File.WriteAllText(log, "");
            var menu = FindObjectsByType<Codice.UI.MainMenuUI>(FindObjectsSortMode.None).FirstOrDefault();
            if (!menu) { File.AppendAllText(log, "no menu\n"); Application.Quit(); yield break; }
            typeof(Codice.UI.MainMenuUI).GetMethod("ShowOptions", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, new object[] { true });
            yield return new WaitForSecondsRealtime(1.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "options.png"));
            var opts = (RectTransform)typeof(Codice.UI.MainMenuUI).GetField("options", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(menu);
            foreach (RectTransform c in opts) File.AppendAllText(log, $"{c.name} pos={c.anchoredPosition} size={c.sizeDelta} active={c.gameObject.activeSelf}\n");
            float before = AudioListener.volume; var minus = opts.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b => b.name.StartsWith("VOLUMEN") && !b.name.EndsWith("+"));
            if (minus) minus.onClick.Invoke(); yield return null;
            File.AppendAllText(log, $"volume before={before} after={AudioListener.volume} button={(minus ? minus.name : "none")}\n");
            yield return new WaitForSecondsRealtime(1); Application.Quit();
        }
    }
    public sealed class ProbeTarget : MonoBehaviour, Codice.Characters.IDamageReceiver { public float damage; public int hits; public void ReceiveDamage(Codice.Characters.DamageInfo hit) { hits++; damage += hit.amount; Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Hit, transform.position); } }
}
#endif
