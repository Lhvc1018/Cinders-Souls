using System.Linq;
using System.Text;
using Codice.Mechanics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
/// Walkability audit of the current floor: navmesh path from the start to every room (stairs included), corridor props count, console errors.
public static class AgentReach
{
    public static string Run()
    {
        var r = RunDirector.Instance; if (!r || !r.Expedition) return "no run";
        var f = r.Expedition; var l = f.Layout; var sb = new StringBuilder();
        NavMesh.SamplePosition(new Vector3(0, 0, -3), out var start, 4, NavMesh.AllAreas);
        sb.AppendLine($"floor {f.FloorNumber} rooms={l.Count} secret={l.SecretRoom} vault={l.VaultRoom} vertical={l.Vertical}");
        var path = new NavMeshPath();
        for (int i = 1; i < l.Count; i++)
        {
            var c = f.Center(i); bool sampled = NavMesh.SamplePosition(c, out var h, 6, NavMesh.AllAreas);
            if (!sampled) { sb.AppendLine($" room {i}: NO NAVMESH near center {c}"); continue; }
            NavMesh.CalculatePath(start.position, h.position, NavMesh.AllAreas, path);
            float len = 0; for (int k = 1; k < path.corners.Length; k++) len += Vector3.Distance(path.corners[k - 1], path.corners[k]);
            string tag = i == l.SecretRoom ? " (secret, wall closed)" : i == l.VaultRoom ? " (vault)" : "";
            if (path.status != NavMeshPathStatus.PathComplete || tag != "") sb.AppendLine($" room {i}: {path.status} len={len:0}m{tag}");
        }
        var stairs = l.Levels.Count(kv => kv.Value.Stair); int badStair = 0;
        foreach (var kv in l.Levels.Where(kv => kv.Value.Stair))
        {
            var p = new Vector3(kv.Key.x * 4, (kv.Value.a + kv.Value.b) / 2, kv.Key.y * 4);
            if (!NavMesh.SamplePosition(p, out var sh, 1.2f, NavMesh.AllAreas)) { badStair++; sb.AppendLine($" stair cell {kv.Key} h={kv.Value.a:0.0}->{kv.Value.b:0.0} has no navmesh"); }
        }
        sb.AppendLine($" stairs={stairs} unwalkable={badStair} vents={Object.FindObjectsByType<Codice.Expedition.FlameVent>(FindObjectsSortMode.None).Length} lights={Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length}");
        return sb.ToString();
    }
    /// Puts the player at the foot of stair number N (arg.txt) looking up it and captures the game view.
    public static async System.Threading.Tasks.Task<string> StairShot()
    {
        var r = RunDirector.Instance; var f = r.Expedition; var l = f.Layout; var p = r.Player; p.State.LaboratoryImmortal = true;
        int n = 0; int.TryParse(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../AgentScripts/_bridge/arg.txt")).Trim(), out n);
        var st = l.Levels.Where(kv => kv.Value.Stair).Skip(n).FirstOrDefault(); if (st.Value == null) return "no stair " + n;
        var dir = new Vector3(st.Value.dir.x, 0, st.Value.dir.y); var foot = new Vector3(st.Key.x * 4, st.Value.a, st.Key.y * 4) - dir * 5f;
        NavMesh.SamplePosition(foot, out var h, 3, NavMesh.AllAreas);
        var cc = p.GetComponent<CharacterController>(); cc.enabled = false; p.transform.SetPositionAndRotation(h.position + Vector3.up * .1f, Quaternion.LookRotation(dir)); cc.enabled = true;
        foreach (var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
        await System.Threading.Tasks.Task.Delay(1500);
        var file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, $"../AgentScripts/_bridge/stair{n}.png"));
        ScreenCapture.CaptureScreenshot(file, 1); await System.Threading.Tasks.Task.Delay(800);
        return $"stair {st.Key} {st.Value.a:0.0}->{st.Value.b:0.0} dir={st.Value.dir} -> {file}";
    }
    /// Hits the secret wall like a player would, then checks the chamber becomes reachable and the minimap flag flips.
    public static async System.Threading.Tasks.Task<string> BreakSecret()
    {
        var r = RunDirector.Instance; var f = r.Expedition; var l = f.Layout; if (l.SecretRoom < 0) return "no secret";
        var wall = Object.FindAnyObjectByType<Codice.Expedition.SecretWall>(); if (!wall) return "NO SecretWall component";
        var cols = wall.GetComponentsInChildren<Collider>().Length; var mesh = wall.GetComponentInChildren<MeshFilter>()?.sharedMesh?.name;
        if (wall.key >= 0) return $"wall needs memory key {wall.key} (colliders={cols} mesh={mesh})";
        for (int i = 0; i < 6 && !f.SecretRevealed; i++) wall.ReceiveDamage(new Codice.Characters.DamageInfo { amount = 10, source = r.Player.gameObject, direction = Vector3.right });
        await System.Threading.Tasks.Task.Delay(500);
        NavMesh.SamplePosition(new Vector3(0, 0, -3), out var a, 4, NavMesh.AllAreas); NavMesh.SamplePosition(f.Center(l.SecretRoom), out var b, 6, NavMesh.AllAreas);
        var path = new NavMeshPath(); NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path);
        return $"colliders={cols} mesh={mesh} revealed={f.SecretRevealed} path={path.status}";
    }
    /// Fires a full Iaido sideways relative to the current facing and samples body facing / travel each frame.
    public static async System.Threading.Tasks.Task<string> Iaido()
    {
        var p = RunDirector.Instance.Player; var ca = p.GetComponent<Codice.Characters.ChargedAttack>(); if (!ca) return "no ChargedAttack";
        var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(Codice.Characters.ChargedAttack).GetField("chargeStart", BF).SetValue(ca, Time.time - 3);
        { var f = RunDirector.Instance.Expedition; NavMesh.SamplePosition(f.Center(2) - Vector3.forward * 6, out var hh, 4, NavMesh.AllAreas); var cc = p.GetComponent<CharacterController>(); cc.enabled = false; p.transform.SetPositionAndRotation(hh.position + Vector3.up * .1f, Quaternion.identity); cc.enabled = true;
          foreach (var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) e.gameObject.SetActive(false); await System.Threading.Tasks.Task.Delay(800); }
        var visual = p.GetComponentInChildren<Animator>().transform; var start = p.transform.position; var sb = new StringBuilder();
        typeof(Codice.Characters.ChargedAttack).GetMethod("Release", BF).Invoke(ca, null);
        for (int i = 0; i < 12; i++)
        {
            await System.Threading.Tasks.Task.Delay(40);
            var ap = p.GetComponent<Codice.Presentation.ActorPresentation>();
            sb.Append($"t{i * 40} active={ca.IaidoActive} moved={Vector3.Distance(start, p.transform.position):0.0} bodyVsDir={Vector3.Angle(visual.forward, ca.IaidoDirection):0} state={ap?.CurrentState}\n");
        }
        return sb.ToString();
    }
    /// Rendering audit of the loaded scene: materials whose shader is missing/unsupported (renders magenta), particle
    /// renderers without material, emission/HDR colours above 50 (bloom fireflies), and the camera NaN guard.
    public static string Shaders()
    {
        var sb = new StringBuilder(); var bad = new System.Collections.Generic.Dictionary<string, int>(); var hot = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.sharedMaterials.Length == 0 || r.sharedMaterials.Any(m => !m)) { string k = "NULL material on " + r.GetType().Name + " " + r.name; bad[k] = bad.TryGetValue(k, out var c0) ? c0 + 1 : 1; }
            foreach (var m in r.sharedMaterials)
            {
                if (!m) continue; var sh = m.shader;
                if (!sh || !sh.isSupported || sh.name == "Hidden/InternalErrorShader" || sh.name.StartsWith("Standard") || sh.name.StartsWith("Legacy") || sh.name.StartsWith("Particles/"))
                { string k = $"{(sh ? sh.name : "null")} | {m.name} | e.g. {r.name}"; bad[k] = bad.TryGetValue(k, out var c) ? c + 1 : 1; }
                foreach (var prop in new[] { "_EmissionColor", "_BaseColor", "_Color", "_TintColor" })
                    if (m.HasProperty(prop)) { var col = m.GetColor(prop); float mx = Mathf.Max(col.r, col.g, col.b); if (float.IsNaN(mx) || mx > 50) { string k = $"{prop}={mx:0} {m.name} ({sh?.name})"; hot[k] = hot.TryGetValue(k, out var c) ? c + 1 : 1; } }
            }
        }
        foreach (var kv in bad.OrderByDescending(k => k.Value)) sb.AppendLine($"BROKEN x{kv.Value}: {kv.Key}");
        foreach (var kv in hot.OrderByDescending(k => k.Value)) sb.AppendLine($"HOT x{kv.Value}: {kv.Key}");
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) { var d = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); sb.AppendLine($"camera {cam.name} hdr={cam.allowHDR} stopNaN={d?.stopNaN} post={d?.renderPostProcessing}"); }
        return sb.Length == 0 ? "clean" : sb.ToString();
    }
    /// Spawns impact / explosion / status VFX in front of the player and captures the frame (pink = broken material).
    public static async System.Threading.Tasks.Task<string> VfxShot()
    {
        var p = RunDirector.Instance.Player; var f = p.transform.forward; var o = p.transform.position + Vector3.up * 1.2f;
        for (int i = 0; i < 4; i++) Codice.Presentation.PresentationVfx.Impact(o + f * (2 + i) + p.transform.right * (i - 1.5f), -f);
        Codice.Presentation.PresentationVfx.Explosion(o + f * 6);
        Codice.Presentation.ImportedAttackVfx.Burst(o + f * 3 + Vector3.up, true, 1);
        await System.Threading.Tasks.Task.Delay(180);
        var file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../AgentScripts/_bridge/vfx.png")); ScreenCapture.CaptureScreenshot(file, 1); await System.Threading.Tasks.Task.Delay(600);
        return file;
    }
    /// Empties the flask, forces the floor finished and crosses the portal: samples veil / input block / estus on arrival.
    public static async System.Threading.Tasks.Task<string> Travel()
    {
        var r = RunDirector.Instance; var f = r.Expedition; var st = r.Player.State; var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(Codice.Characters.CharacterState).GetField("estusCharges", BF).SetValue(st, 0);
        typeof(Codice.Expedition.ExpeditionFloor).GetProperty("Finished").SetValue(f, true);
        var setPhase = typeof(RunDirector).GetMethod("SetPhase", BF | System.Reflection.BindingFlags.Public); setPhase?.Invoke(r, new object[] { RunPhase.Cleared });
        int from = r.FloorNumber; bool ok = r.TravelNextFloor(); var sb = new StringBuilder($"travel={ok} from {from}\n");
        for (int i = 0; i < 16; i++)
        {
            await System.Threading.Tasks.Task.Delay(400);
            var p = RunDirector.Instance ? RunDirector.Instance.Player : null;
            sb.Append($"{i * 0.4f:0.0}s veil={Codice.UI.FloorTransition.Active} blocked={GameInput.Blocked} floor={RunDirector.Instance?.FloorNumber} estus={(p ? p.State.EstusCharges : -1)}\n");
        }
        return sb.ToString();
    }
    public static string Status()
    {
        string r = $"paused={UnityEditor.EditorApplication.isPaused} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} run={(RunDirector.Instance ? RunDirector.Instance.FloorNumber.ToString() : "none")} loading={Codice.UI.RunNavigation.Loading} veil={Codice.UI.FloorTransition.Active} blocked={GameInput.Blocked} timeScale={Time.timeScale}";
        UnityEditor.EditorApplication.isPaused = false; return r;
    }
    /// Opens the inventory, selects every cell (16) and presses EQUIPAR on spells/talismans, after granting a spell, an imbue and talismans.
    public static async System.Threading.Tasks.Task<string> Inventory()
    {
        var p = RunDirector.Instance.Player; var sb = new StringBuilder(); var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var book = p.GetComponent<Codice.Characters.RunSpellbook>(); book.Learn(Codice.Characters.RunSpell.StormBlade); book.Learn(Codice.Characters.RunSpell.Fire);
        p.State.GrantTalisman(Codice.Characters.Talisman.CrimsonAmber); p.State.GrantTalisman(Codice.Characters.Talisman.BloodPact);
        float hpBefore = p.State.MaxHealth; p.State.GrantTalisman(Codice.Characters.Talisman.AzurTear);
        sb.AppendLine($"talismans owned={p.State.OwnedTalismanCount} slot0={p.State.TalismanInSlot(0)} slot1={p.State.TalismanInSlot(1)} maxHP={p.State.MaxHealth:0} maxMana={p.State.MaxMana:0} dmg={p.State.BoonDamage:0.00}");
        p.GetComponent<Codice.Characters.WeaponImbue>()?.Apply(Codice.Characters.Element.Shock, 10);
        var hud = Codice.UI.RunHUD.Instance; var t = typeof(Codice.UI.RunHUD);
        t.GetMethod("SetPaused", BF | System.Reflection.BindingFlags.Public)?.Invoke(hud, new object[] { true });
        await System.Threading.Tasks.Task.Delay(300);
        for (int i = 0; i < 16; i++)
        {
            t.GetField("inventorySelection", BF).SetValue(hud, i); t.GetMethod("RefreshInventory", BF).Invoke(hud, null);
            var title = ((TMPro.TextMeshProUGUI)t.GetField("itemTitle", BF).GetValue(hud)).text;
            if (i >= 8) t.GetMethod("EquipSelected", BF).Invoke(hud, null);
            sb.Append($"[{i}] {title} | ");
        }
        sb.AppendLine($"\nselected spell={book.Selected} known={string.Join(",", book.Known)} imbue={p.GetComponent<Codice.Characters.WeaponImbue>()?.Imbued}");
        var shot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../AgentScripts/_bridge/inv.png")); ScreenCapture.CaptureScreenshot(shot, 1); await System.Threading.Tasks.Task.Delay(500);
        t.GetMethod("SetPaused", BF | System.Reflection.BindingFlags.Public)?.Invoke(hud, new object[] { false });
        await System.Threading.Tasks.Task.Delay(300);
        var shot2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../AgentScripts/_bridge/hud.png")); ScreenCapture.CaptureScreenshot(shot2, 1); await System.Threading.Tasks.Task.Delay(500);
        return sb.ToString();
    }
    /// Look-dev capture: stands at the south edge of room N (arg "room tag"), looking across it, enemies hidden.
    public static async System.Threading.Tasks.Task<string> RoomShot()
    {
        var a = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "../AgentScripts/_bridge/arg.txt")).Trim().Split(' ');
        int room = int.Parse(a[0]); string tag = a.Length > 1 ? a[1] : "shot";
        var r = RunDirector.Instance; var f = r.Expedition; var l = f.Layout; var p = r.Player; p.State.LaboratoryImmortal = true;
        var c = f.Center(room); var plan = l.Plans[room]; var at = c + new Vector3(0, 0, -(plan.depth * 4 - 3));
        NavMesh.SamplePosition(at, out var h, 4, NavMesh.AllAreas);
        var cc = p.GetComponent<CharacterController>(); cc.enabled = false; p.transform.SetPositionAndRotation(h.position + Vector3.up * .1f, Quaternion.LookRotation(Vector3.forward)); cc.enabled = true;
        foreach (var e in Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
        await System.Threading.Tasks.Task.Delay(1600);
        var file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, $"../AgentScripts/_bridge/look_{tag}.png")); ScreenCapture.CaptureScreenshot(file, 1); await System.Threading.Tasks.Task.Delay(700);
        return file;
    }
    /// Lists renderers in view whose main colour is saturated orange/red and flat (unlit / emissive quads), with their path.
    public static string Oddities()
    {
        var sb = new StringBuilder(); var cam = RunDirector.Instance.Player.view; var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.isVisible || !GeometryUtility.TestPlanesAABB(planes, r.bounds) || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            var m = r.sharedMaterial; if (!m) continue; var sh = m.shader.name;
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Color.RGBToHSV(c, out var hh, out var ss, out var vv);
            if (sh.Contains("Unlit") || ss > .6f && vv > .5f)
            {
                var t = r.transform; string path = t.name; while (t.parent && path.Length < 120) { t = t.parent; path = t.name + "/" + path; }
                sb.AppendLine($"{sh} {c} size={r.bounds.size} at={r.bounds.center} :: {path}");
            }
        }
        return sb.ToString();
    }
    /// Close-up of the torch flame nearest to room 1 through a temporary camera (arg: distance).
    public static async System.Threading.Tasks.Task<string> TorchShot()
    {
        var f = RunDirector.Instance.Expedition; var c = f.Center(1); Transform best = null; float bd = 1e9f;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name == "Living torch flame") { float d = (t.position - c).sqrMagnitude; if (d < bd) { bd = d; best = t; } }
        if (!best) return "no torch";
        var torch = best.parent; var outDir = Vector3.ProjectOnPlane(best.position - torch.position, Vector3.up); if (outDir.sqrMagnitude < .001f) outDir = torch.forward; outDir.Normalize();
        var go = new GameObject("Look camera"); var cam = go.AddComponent<Camera>(); cam.depth = 100; cam.fieldOfView = 45; var d0 = cam.GetUniversalAdditionalCameraData(); d0.renderPostProcessing = true;
        go.transform.position = best.position + outDir * 2.4f + Vector3.up * -.5f + Vector3.Cross(Vector3.up, outDir) * .9f; go.transform.LookAt(best.position + Vector3.up * .1f);
        await System.Threading.Tasks.Task.Delay(900);
        var file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../AgentScripts/_bridge/torch.png")); ScreenCapture.CaptureScreenshot(file, 1); await System.Threading.Tasks.Task.Delay(600);
        string counts = string.Join(",", best.GetComponentsInChildren<ParticleSystem>().Select(p => p.name + ":" + p.particleCount)); var rb = torch.GetComponentInChildren<Renderer>().bounds;
        Object.Destroy(go); return $"torch at {best.position} bounds={rb.center}/{rb.size} particles={counts} -> {file}";
    }
    /// Parry checks: (1) Parry.TryDeflect accepts a Shinobi in a perfect guard facing the bolt and rejects one facing away;
    /// (2) a reflected bolt flies back and damages its shooter.
    public static async System.Threading.Tasks.Task<string> Reflect()
    {
        var r = RunDirector.Instance; var p = r.Player; var f = r.Expedition; var sb = new StringBuilder(); p.State.LaboratoryImmortal = true;
        var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var g = p.GetComponent<Codice.Characters.ShinobiGuard>();
        void Perfect() { typeof(Codice.Characters.ShinobiGuard).GetProperty("Guarding").SetValue(g, true); typeof(Codice.Characters.ShinobiGuard).GetField("perfectUntil", BF).SetValue(g, Time.time + 1); }
        if (!g) return "need Shinobi";
        Perfect(); bool front = Codice.Characters.Parry.TryDeflect(p, -p.transform.forward, p.transform.position + Vector3.up);
        Perfect(); bool back = Codice.Characters.Parry.TryDeflect(p, p.transform.forward, p.transform.position + Vector3.up);
        sb.AppendLine($"deflect facing={front} (expect True) fromBehind={back} (expect False)");
        // spawn an enemy shooter 8 m ahead and a bolt that we reflect
        var cc = p.GetComponent<CharacterController>();
        for (int room = 1; room < f.Layout.Count && !Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Any(e => e.IsAlive); room++)
        { NavMesh.SamplePosition(f.Center(room), out var h, 4, NavMesh.AllAreas); cc.enabled = false; p.transform.position = h.position + Vector3.up * .1f; cc.enabled = true; await System.Threading.Tasks.Task.Delay(1500); }
        var enemy = Object.FindObjectsByType<Codice.Enemies.BasicEnemy>(FindObjectsSortMode.None).Where(e => e.IsAlive).OrderBy(e => Vector3.Distance(e.transform.position, p.transform.position)).FirstOrDefault();
        if (!enemy) return sb + "no enemy";
        float hp0 = enemy.Health;
        var bolt = new GameObject("Test bolt"); var shotPos = p.transform.position + Vector3.up * 1.2f; bolt.transform.position = shotPos;
        var proj = bolt.AddComponent<Codice.Enemies.EnemyProjectile>(); proj.Launch(enemy, p, (shotPos - enemy.transform.position).normalized, 12, 15); proj.Reflect(shotPos, p);
        sb.AppendLine($"shooter at {enemy.transform.position} bolt at {bolt.transform.position}");
        for (int i = 0; i < 20 && bolt; i++) { await System.Threading.Tasks.Task.Delay(100); if (bolt) sb.Append($"{bolt.transform.position} "); }
        sb.AppendLine(enemy ? $"reflected bolt: shooter hp {hp0:0} -> {enemy.Health:0} boltAlive={(bool)bolt}" : $"reflected bolt: shooter hp {hp0:0} -> killed (destroyed)");
        return sb.ToString();
    }
    /// Lists objects built from Unity primitives (Cube/Sphere/Cylinder/Capsule) near the player: placeholder art to replace.
    public static string Primitives()
    {
        var p = RunDirector.Instance.Player.transform.position; var sb = new StringBuilder(); var seen = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            var m = mf.sharedMesh; if (!m || !(m.name == "Sphere" || m.name == "Cylinder" || m.name == "Cube" || m.name == "Capsule")) continue;
            var t = mf.transform; string path = t.name; for (int i = 0; i < 3 && t.parent; i++) { t = t.parent; path = t.name + "/" + path; }
            var r = mf.GetComponent<Renderer>(); string key = m.name + " :: " + path + " :: " + (r && r.sharedMaterial ? r.sharedMaterial.name : "");
            seen[key] = seen.TryGetValue(key, out var n) ? n + 1 : 1;
        }
        foreach (var kv in seen.OrderByDescending(k => k.Value)) sb.AppendLine($"x{kv.Value} {kv.Key}");
        return sb.ToString();
    }
    /// Distinct named objects with renderers within N m (arg) of the player start, grouped by top-level name under the floor root.
    public static string Near()
    {
        float radius = 14; var p = new Vector3(0, 0, -3); var sb = new StringBuilder(); var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if ((r.bounds.center - p).sqrMagnitude > radius * radius || r is ParticleSystemRenderer) continue;
            var t = r.transform; string path = t.name; for (int i = 0; i < 4 && t.parent && !t.parent.name.StartsWith("Seeded"); i++) { t = t.parent; path = t.name + "/" + path; }
            string key = path.Split('/')[0] + " [" + (r.TryGetComponent<MeshFilter>(out var mfx) && mfx.sharedMesh ? mfx.sharedMesh.name : r.GetType().Name) + "] " + r.sharedMaterial?.shader?.name;
            if (seen.Add(key)) sb.AppendLine(key + " size=" + r.bounds.size.ToString("F1") + " at " + r.bounds.center.ToString("F1"));
        }
        return sb.ToString();
    }
    /// Portrait of the player's head from the front and 3/4 (temporary camera), for face / mask look-dev.
    public static async System.Threading.Tasks.Task<string> Face()
    {
        var p = RunDirector.Instance.Player; var anim = p.GetComponentInChildren<Animator>(); var head = anim && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
        Vector3 h = head ? head.position : p.transform.position + Vector3.up * 1.6f; var fwd = anim ? anim.transform.forward : p.transform.forward;
        var go = new GameObject("Face camera"); var cam = go.AddComponent<Camera>(); cam.depth = 100; cam.fieldOfView = 30; cam.nearClipPlane = .05f;
        string res = "";
        foreach (var (tag, dir) in new[] { ("front", fwd), ("side", Quaternion.Euler(0, 40, 0) * fwd) })
        {
            go.transform.position = h + dir * 1.1f + Vector3.up * .05f; go.transform.LookAt(h);
            await System.Threading.Tasks.Task.Delay(500);
            var file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, $"../AgentScripts/_bridge/face_{tag}.png")); ScreenCapture.CaptureScreenshot(file, 1); await System.Threading.Tasks.Task.Delay(500); res += file + " ";
        }
        Object.Destroy(go); return res + " headBone=" + (head ? head.name : "none");
    }
    /// Sekiro deflect tiers on the Shinobi with a real enemy as the attacker: press timing -> HP and posture results.
    public static string Deflect()
    {
        var p = RunDirector.Instance.Player; var g = p.GetComponent<Codice.Characters.ShinobiGuard>(); if (!g) return "need Shinobi";
        var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; var T = typeof(Codice.Characters.ShinobiGuard);
        var attacker = new GameObject("Test attacker"); var sb = new StringBuilder();
        foreach (var (label, pressAgo, held, angle) in new[] { ("tap 0.10s antes (perfecto)", .1f, false, 0f), ("tap 0.30s antes (imperfecto)", .3f, false, 0f), ("mantener sin tap (bloqueo)", 9f, true, 0f), ("tap 0.10s, golpe de lado 80°", .1f, false, 80f), ("tap 0.10s, golpe por detrás", .1f, false, 180f), ("tap 0.6s antes (tarde)", .6f, false, 0f) })
        {
            T.GetProperty("Posture").SetValue(g, 100f); T.GetField("brokenUntil", BF).SetValue(g, 0f); T.GetField("pressAt", BF).SetValue(g, Time.time - pressAgo); T.GetProperty("Guarding").SetValue(g, held);
            var dir = Quaternion.Euler(0, angle, 0) * p.transform.forward; attacker.transform.position = p.transform.position + dir * 1.5f;
            float dmg = g.Resolve(new Codice.Characters.DamageInfo { amount = 20, melee = true, source = attacker, direction = -dir });
            sb.AppendLine($"{label}: daño {dmg:0} postura {g.Posture:0}");
        }
        Object.Destroy(attacker); return sb.ToString();
    }
    /// Gurdi del Abismo: goes to the boss room, fires the rebuilt chest laser and the slam ring, captures both, reports damage.
    public static async System.Threading.Tasks.Task<string> Gurdi()
    {
        var r = RunDirector.Instance; var f = r.Expedition; var p = r.Player; var sb = new StringBuilder(); var cc = p.GetComponent<CharacterController>();
        foreach (var e in f.Layout.Links) { if (e.y == f.Layout.Boss) f.Cleared[e.x] = true; if (e.x == f.Layout.Boss) f.Cleared[e.y] = true; }
        NavMesh.SamplePosition(f.Center(f.Layout.Boss), out var h, 8, NavMesh.AllAreas); cc.enabled = false; p.transform.position = h.position + Vector3.up * .1f; cc.enabled = true;
        Codice.Bosses.BossCharacter boss = null; for (int i = 0; i < 30 && !boss; i++) { await System.Threading.Tasks.Task.Delay(300); boss = Object.FindAnyObjectByType<Codice.Bosses.BossCharacter>(); }
        if (!boss) return "no boss"; await System.Threading.Tasks.Task.Delay(2500);
        sb.AppendLine($"boss={boss.name} kind={boss.kind} anchor={Codice.Bosses.GuardianEnergy.Anchor(boss).name}");
        boss.enabled = false;
        var cam = new GameObject("Look camera").AddComponent<Camera>(); cam.depth = 100; cam.fieldOfView = 55; cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var mid = (boss.transform.position + p.transform.position) * .5f; var side = Vector3.Cross(Vector3.up, (p.transform.position - boss.transform.position).normalized);
        cam.transform.position = mid + side * 9 + Vector3.up * 4; cam.transform.LookAt(mid + Vector3.up * 1.5f);
        var charge = Codice.Bosses.GuardianEnergy.Charge(boss, 1.5f, new Color(.7f, .45f, .95f)); await System.Threading.Tasks.Task.Delay(1300);
        string F(string n) => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, $"../AgentScripts/_bridge/{n}.png"));
        ScreenCapture.CaptureScreenshot(F("gurdi_charge"), 1); await System.Threading.Tasks.Task.Delay(300); Object.Destroy(charge);
        p.State.LaboratoryImmortal = false; float hp0 = p.State.Health + p.State.Armor;
        Codice.Bosses.GuardianEnergy.Fire(boss, p, 2.5f, new Color(.7f, .45f, .95f)); await System.Threading.Tasks.Task.Delay(1200);
        ScreenCapture.CaptureScreenshot(F("gurdi_beam"), 1); await System.Threading.Tasks.Task.Delay(1500);
        float hp1 = p.State.Health + p.State.Armor; sb.AppendLine($"beam: player hp+armor {hp0:0} -> {hp1:0}");
        Codice.Bosses.GuardianEnergy.Shockwave(boss, boss.transform.position, p, new Color(.7f, .45f, .95f)); await System.Threading.Tasks.Task.Delay(450);
        ScreenCapture.CaptureScreenshot(F("gurdi_wave"), 1); await System.Threading.Tasks.Task.Delay(900);
        sb.AppendLine($"wave: {hp1:0} -> {p.State.Health + p.State.Armor:0}");
        p.State.LaboratoryImmortal = true; Object.Destroy(cam.gameObject); boss.enabled = true; return sb.ToString();
    }
}