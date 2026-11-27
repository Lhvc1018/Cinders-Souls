using Codice.Characters;
using Codice.Mechanics;
using Codice.Presentation;
using Codice.Expedition;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Codice.Prologue
{
    // Two roads after the Vigía: EL UMBRAL (descend now) and LA PRUEBA (a basic tutorial of every control).
    public sealed partial class PrologueDirector
    {
        enum Step { Move, Sprint, Jump, Dodge, Attack, Charged, Defend, Special, Relics, Passive, Scroll, Flask, Done }
        Transform directGate, trialGate, trialExitGate;
        TrainingDummy dummy;
        Vector3 trialExit;
        bool pathsOpen, trialStarted;
        Step step; float stepValue; Vector3 lastPos; int stepBaseline; float stepStarted;
        static Vector3 W(Vector2Int c) => new Vector3(c.x * 4, 0, c.y * 4);
        bool NearAnyExit() => Vector3.Distance(player.transform.position, ExitPosition) < 3.2f || TrialFinished && Vector3.Distance(player.transform.position, trialExit) < 3.2f;
        public bool TrialFinished => step == Step.Done;
        Codice.UI.RunHUD hud;
        /// The prologue now shows the same player HUD as the descent (life, mana, relics, flask, prompts).
        void InstallHud()
        {
            if (!player.GetComponent<RunSpellbook>()) player.gameObject.AddComponent<RunSpellbook>();
            if (!player.GetComponent<ElementalStatus>()) player.gameObject.AddComponent<ElementalStatus>();
            var holder = new GameObject("Prologue run context"); holder.SetActive(false); holder.transform.SetParent(transform);
            var scene = holder.AddComponent<Codice.Mechanics.RunScene>(); scene.player = player;
            hud = Codice.UI.RunHUD.Install(scene); Codice.UI.RunHUD.PauseMenuEnabled = false;
        }
        void LateUpdate() { if (hud && hud.Canvas) hud.Canvas.enabled = IntroFinished && !DialogueOpen; }
        void BuildPaths(Transform root)
        {
            directGate = Gate(root, new Vector3(0, 0, 22), 0, "Reja · Umbral");
            trialGate = Gate(root, new Vector3(-6, 0, 16), 90, "Reja · Prueba");
            trialExitGate = Gate(root, new Vector3(-24, 0, 26), 0, "Reja · Salida de la prueba");
            Sign(root, "EL UMBRAL", "Desciende ahora", new Vector3(0, 3.1f, 21.7f), 0, new Color(.85f, .18f, .1f));
            Sign(root, "LA PRUEBA", "Aprende a luchar", new Vector3(-5.7f, 3.1f, 16), -90, new Color(.86f, .7f, .4f));
            trialExit = W(PrologueLayout.TrialExitCell) + Vector3.forward * 1;
            var marker = new GameObject("Trial threshold").AddComponent<Light>(); marker.transform.SetParent(root); marker.transform.position = trialExit + Vector3.up;
            marker.type = LightType.Point; marker.color = new Color(1, .08f, .035f); marker.intensity = 4; marker.range = 10;
            Sign(root, "EL UMBRAL", "", trialExit + new Vector3(0, 2.7f, 0), 0, new Color(.85f, .18f, .1f));
            var oldLabel = root.GetComponentsInChildren<TMPro.TextMeshPro>(true); foreach (var l in oldLabel) if (l.name == "Threshold inscription") l.gameObject.SetActive(false);
            dummy = TrainingDummy.Create(W(PrologueLayout.Trial) + new Vector3(0, 0, 3), root);
        }
        Transform Gate(Transform root, Vector3 at, float yaw, string name)
        {
            var gate = new GameObject(name).transform; gate.SetParent(root); gate.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var iron = new Material(Shader.Find("Universal Render Pipeline/Lit")); iron.color = new Color(.12f, .11f, .1f); if (iron.HasProperty("_Metallic")) iron.SetFloat("_Metallic", .8f);
            for (int i = 0; i < 9; i++) Part(gate, PrimitiveType.Cylinder, new Vector3(-1.8f + i * .45f, 2.1f, 0), new Vector3(.09f, 2.1f, .09f), iron);
            for (int i = 0; i < 3; i++) Part(gate, PrimitiveType.Cube, new Vector3(0, .5f + i * 1.6f, 0), new Vector3(4, .14f, .12f), iron);
            var col = gate.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 2.2f, 0); col.size = new Vector3(4, 4.4f, .35f);
            return gate;
        }
        static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type); Object.Destroy(go.GetComponent<Collider>()); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = m;
        }
        void Sign(Transform root, string title, string sub, Vector3 at, float yaw, Color color)
        {
            var label = new GameObject("Sign · " + title).AddComponent<TextMeshPro>(); label.transform.SetParent(root);
            label.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            label.text = title + (sub.Length > 0 ? $"\n<size=55%><color=#B8A98A>{sub}</color></size>" : "");
            label.font = Resources.Load<TMP_FontAsset>("CindersFonts/UncialAntiqua"); label.fontSize = 4.2f; label.alignment = TextAlignmentOptions.Center;
            label.color = color; label.rectTransform.sizeDelta = new Vector2(5, 1.6f);
        }
        void OpenGate(Transform gate)
        {
            if (!gate || !gate.GetComponent<BoxCollider>().enabled) return;
            gate.GetComponent<BoxCollider>().enabled = false;
            gate.DOMoveY(gate.position.y + 4.3f, .9f).SetEase(Ease.InOutSine);
            GameAudio.PlayAt(SoundCue.Seal, gate.position);
        }
        void OpenPaths()
        {
            if (pathsOpen) return; pathsOpen = true;
            OpenGate(directGate); OpenGate(trialGate);
            objective.text = GameInput.Prompt("Elige tu camino\n<size=19>Al frente: EL UMBRAL · desciende ya\nA la izquierda: LA PRUEBA · aprende todos los controles</size>");
        }
        string PathsLine() => "Ahora elige. Al frente, EL UMBRAL: si ya sabes pelear, desciende. A la izquierda, LA PRUEBA: un muñeco viejo te enseñará cada golpe, cada guardia y cada trago. No hay vergüenza en aprender; la hay en morir sin saber.";

        // ---------------- the trial ----------------
        bool InTrial => Mathf.Abs(player.transform.position.x - W(PrologueLayout.Trial).x) < 10.5f && Mathf.Abs(player.transform.position.z - W(PrologueLayout.Trial).z) < 10.5f;
        bool CanDefend => player.State.Class == PlayerClass.Warrior || player.State.Class == PlayerClass.Shinobi || player.State.ActiveWeapon?.kind == WeaponKind.Shield;
        void TickTrial()
        {
            if (!pathsOpen || step == Step.Done || DialogueOpen) return;
            if (!trialStarted) { if (!InTrial) return; trialStarted = true; StartStep(Step.Move); GameAudio.Play(SoundCue.Portal); }
            var p = player.transform.position; float moved = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(lastPos.x, 0, lastPos.z)); lastPos = p;
            bool done = false;
            switch (step)
            {
                case Step.Move: stepValue += moved; done = stepValue > 6; break;
                case Step.Sprint: if (player.SprintRequested && GameInput.Move().sqrMagnitude > .1f) stepValue += Time.deltaTime; done = stepValue > 1.2f; break;
                case Step.Jump: done = GameInput.Pressed(GameAction.Jump) && Time.time > stepStarted + .2f; break;
                case Step.Dodge: done = player.IsDashing; break;
                case Step.Attack: done = dummy.Hits - stepBaseline >= 3; break;
                case Step.Charged: done = player.GetComponent<ChargedAttack>().Executed > stepBaseline && dummy.Hits > 0; break;
                case Step.Defend: done = (CanDefend ? dummy.Blocked : dummy.Evaded) - stepBaseline >= 2; break;
                case Step.Special: { var m = player.GetComponent<PlayerMechanics>(); var w = player.State.ActiveWeapon; done = m && (m.UltimateActive || m.UltimateQueued) || w != null && w.ultimateCharge < 50; break; }
                case Step.Relics: done = player.State.RelicWait(0) > 0 && player.State.RelicWait(1) > 0; break;
                case Step.Passive: done = player.State.BoonTotal > stepBaseline; break;
                case Step.Scroll: { var book = player.GetComponent<RunSpellbook>(); done = book && book.Known.Count > 0 && book.Ready > stepStarted; break; }
                case Step.Flask: { var f = player.GetComponent<EstusFlask>(); done = f && f.Drinking || player.State.EstusCharges < stepBaseline; break; }
            }
            if (done) Advance();
        }
        void StartStep(Step s)
        {
            step = s; stepValue = 0; stepStarted = Time.time; lastPos = player.transform.position;
            dummy.Attacking = s == Step.Defend;
            stepBaseline = s == Step.Attack ? dummy.Hits : s == Step.Charged ? player.GetComponent<ChargedAttack>().Executed : s == Step.Defend ? (CanDefend ? dummy.Blocked : dummy.Evaded) : 0;
            if (s == Step.Special) { var w = player.State.ActiveWeapon; if (w != null) w.ultimateCharge = 100; }
            if (s == Step.Relics) for (int i = 0; i < 2; i++) player.State.EquipRelic(i == 0 ? ActiveRelic.Censer : ActiveRelic.FrostBell, i);
            if (s == Step.Passive) { stepBaseline = player.State.BoonTotal; AshBoonOrb.Create(AshBoon.Vigor, dummy.transform.position + new Vector3(-2.2f, 1.1f, -1.5f), dummy.transform.parent); }
            if (s == Step.Scroll) { if (!player.GetComponent<RunSpellbook>()) player.gameObject.AddComponent<RunSpellbook>(); SpellPickup.Create(dummy.transform.position + new Vector3(2.2f, .6f, -1.5f), RunSpell.Frost, dummy.transform.parent); }
            if (s == Step.Flask)
            {
                if (!player.GetComponent<EstusFlask>()) player.gameObject.AddComponent<EstusFlask>();
                player.State.RefillEstus(); stepBaseline = player.State.EstusCharges; player.State.Damage(player.State.MaxHealth * .35f);
            }
            if (s == Step.Done) { OpenGate(trialExitGate); GameAudio.Play(SoundCue.Victory); }
            objective.text = GameInput.Prompt(StepText(s));
        }
        void Advance() { GameAudio.Play(SoundCue.Orb); GameInput.Rumble(.2f, .3f, .1f); StartStep(step + 1); }
        string StepText(Step s)
        {
            string n = $"LA PRUEBA · {(int)s + 1}/{(int)Step.Done}\n<size=21>";
            switch (s)
            {
                case Step.Move: return n + "Muévete con [WASD] y mira alrededor con [Ratón].</size>";
                case Step.Sprint: return n + "Corre: mantén [Ctrl] mientras avanzas." + (GameInput.UsingGamepad ? " Con mando, pulsa el stick izquierdo." : "") + "</size>";
                case Step.Jump: return n + "Salta con [Espacio].</size>";
                case Step.Dodge: return n + "Esquiva con [Mayús]. Durante la voltereta eres intocable.</size>";
                case Step.Attack: return n + "Golpea al muñeco 3 veces con [Clic izquierdo]. Encadena golpes para un combo.</size>";
                case Step.Charged: return n + (GameInput.UsingGamepad ? "Mantén [Z] y suelta" : "Mantén el botón izquierdo o Z y suelta") + $". Tu clase: {ChargedAttack.MoveName(player.State.Class)} — {ChargedAttack.MoveDescription(player.State.Class)}.</size>";
                case Step.Defend: return n + (CanDefend
                    ? "El muñeco va a golpearte. Bloquea con [Clic derecho] 2 veces. Si lo haces justo antes del impacto, es un parry."
                    : "El muñeco va a golpearte. Tu clase no bloquea: esquiva su golpe con [Mayús] 2 veces cuando brille.") + "</size>";
                case Step.Special: return n + "El arte de tu arma está listo: úsalo con [Q]. Se recarga golpeando.</size>";
                case Step.Relics: return n + $"Reliquias ACTIVAS: se usan con botón y gastan maná. Usa {RelicCatalog.Name(player.State.GetRelic(0))} con [F] y {RelicCatalog.Name(player.State.GetRelic(1))} con [G].</size>";
                case Step.Passive: return n + "Dones PASIVOS: no tienen botón, actúan solos. Recoge el orbe brillante con [E] (Vigor: +12 % de vida).</size>";
                case Step.Scroll: return n + "Escrituras de HECHIZO: apréndela con [E] y lánzala con [T]. Si tienes varias, cámbialas con [C].</size>";
                case Step.Flask: return n + "Estás herido. Bebe del frasco de ceniza con [H]. Si te golpean, la bebida se corta.</size>";
                default: return "LA PRUEBA SUPERADA\n<size=21>La reja del fondo se abrió. Cruza su umbral con [E] cuando quieras descender.</size>";
            }
        }
    }

    /// Wooden practice dummy: takes any hit, never breaks, and in the defence lesson swings at the player.
    public sealed class TrainingDummy : MonoBehaviour, IDamageReceiver
    {
        public int Hits { get; private set; }
        public int Blocked { get; private set; }
        public int Evaded { get; private set; }
        public bool Attacking;
        float nextSwing; Transform body;
        public static TrainingDummy Create(Vector3 at, Transform parent)
        {
            var root = new GameObject("Muñeco de práctica"); root.transform.SetParent(parent); root.transform.position = at;
            root.transform.rotation = Quaternion.Euler(0, 180, 0);
            var d = root.AddComponent<TrainingDummy>();
            var wood = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.36f, .24f, .14f) };
            var cloth = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.55f, .47f, .33f) };
            d.body = new GameObject("Body").transform; d.body.SetParent(root.transform, false);
            P(d.body, PrimitiveType.Cylinder, new Vector3(0, .8f, 0), new Vector3(.18f, .8f, .18f), wood);
            P(d.body, PrimitiveType.Capsule, new Vector3(0, 1.35f, 0), new Vector3(.62f, .55f, .45f), cloth);
            P(d.body, PrimitiveType.Cube, new Vector3(0, 1.55f, 0), new Vector3(1.5f, .13f, .13f), wood);
            P(d.body, PrimitiveType.Sphere, new Vector3(0, 2.05f, 0), new Vector3(.42f, .42f, .42f), cloth);
            P(d.body, PrimitiveType.Cube, new Vector3(.82f, 1.25f, .25f), new Vector3(.1f, .9f, .1f), wood); // practice blade
            var col = root.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 1.1f, 0); col.height = 2.3f; col.radius = .45f;
            return d;
        }
        static void P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(t); Destroy(go.GetComponent<Collider>()); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = m;
        }
        public void ReceiveDamage(DamageInfo hit)
        {
            Hits++;
            var dir = Vector3.ProjectOnPlane(hit.direction, Vector3.up); if (dir.sqrMagnitude < .01f) dir = transform.forward;
            body.DOKill(true); body.DOPunchRotation(Vector3.Cross(Vector3.up, dir.normalized) * (hit.critical ? 28 : 14), .45f, 6, .6f);
            var point = hit.hasImpactPoint ? hit.impactPoint : transform.position + Vector3.up * 1.3f;
            PresentationVfx.Impact(point, -dir); GameAudio.PlayAt(SoundCue.Hit, point);
        }
        void Update()
        {
            if (!Attacking) return;
            var player = FindFirstObjectByType<PlayerCharacter>(); if (!player) return;
            var to = Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up);
            if (to.sqrMagnitude > .1f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 240 * Time.deltaTime);
            if (Time.time < nextSwing) return;
            nextSwing = Time.time + 3f;
            if (to.magnitude < 4.5f) StartCoroutine(Swing(player));
        }
        System.Collections.IEnumerator Swing(PlayerCharacter player)
        {
            AttackTell.Flash(this, 1f, false); GameAudio.PlayAt(SoundCue.BossWindup, transform.position);
            yield return new WaitForSeconds(1f);
            body.DOKill(true); body.DOPunchRotation(new Vector3(0, -70, 0), .4f, 2, .3f); GameAudio.PlayAt(SoundCue.Swing, transform.position);
            var to = player.transform.position - transform.position;
            if (to.magnitude > 3.2f || !player.State.IsAlive) yield break;
            if (player.IsDashing) { Evaded++; yield break; }
            float before = player.State.Health + player.State.Armor;
            player.ReceiveDamage(new DamageInfo { amount = 10, source = gameObject, direction = to.normalized, melee = true, hasImpactPoint = true, impactPoint = player.transform.position + Vector3.up * 1.2f });
            float lost = before - player.State.Health - player.State.Armor;
            if (lost < 6) Blocked++;
            if (player.State.Health < player.State.MaxHealth * .3f) player.State.Heal(player.State.MaxHealth * .3f);
        }
    }
}
