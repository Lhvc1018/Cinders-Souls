using System.Collections.Generic;
using System.Linq;
using Codice.Characters;
using UnityEngine;
namespace Codice.Prologue
{
    /// Permanent descent progress and the Vigía's lessons. Each visit he speaks only about what you have reached
    /// (keys, refuges, the forge, the deep worlds) and always explains the arsenal of the class you chose.
    public static class VigiaLessons
    {
        const string DeepestKey = "Cinders.Progress.Deepest", RunsKey = "Cinders.Progress.Runs", HeardKey = "Cinders.Vigia.Heard";
        public enum Lesson { Intro, Memories, Refuges, Forge, Cemetery, Horde }
        /// Deepest floor ever reached; permanent keys from earlier builds count as proof of depth.
        public static int Deepest { get { int k = Codice.Expedition.DescentMemory.Keys; return Mathf.Max(PlayerPrefs.GetInt(DeepestKey, 0), (k & 4) != 0 ? 7 : (k & 2) != 0 ? 4 : (k & 1) != 0 ? 1 : 0); } }
        public static int Runs => PlayerPrefs.GetInt(RunsKey, 0);
        public static bool Heard(Lesson l) => (PlayerPrefs.GetInt(HeardKey, 0) & (1 << (int)l)) != 0;
        public static void Reach(int floor) { if (floor > Deepest) { PlayerPrefs.SetInt(DeepestKey, floor); PlayerPrefs.Save(); } }
        public static void CountRun() { PlayerPrefs.SetInt(RunsKey, Runs + 1); PlayerPrefs.Save(); }
        /// The forge only appears once the Vigía has told you about Brann (always present in coop so every machine builds the same floor).
        public static bool ForgeRevealed => Codice.Multiplayer.LanSession.Active || Heard(Lesson.Forge) || Deepest >= 4;
        static bool Available(Lesson l)
        {
            switch (l)
            {
                case Lesson.Intro: return true;
                case Lesson.Memories: return Runs >= 2 || Deepest >= 1;
                case Lesson.Refuges: return Deepest >= 2;
                case Lesson.Forge: return Deepest >= 2 || Runs >= 3;
                case Lesson.Cemetery: return Deepest >= 6;
                case Lesson.Horde: return Deepest >= 10;
            }
            return false;
        }
        static readonly Dictionary<Lesson, string[]> Text = new Dictionary<Lesson, string[]>
        {
            { Lesson.Memories, new[]{
                "Volviste. La ceniza no te recuerda, pero yo sí. Escucha: los guardianes de 1-1, 2-1 y 3-1 guardan llaves que no se pierden al morir.",
                "Recógelas con E. No se gastan: abren cámaras selladas en cada descenso futuro. El atril de la entrada te dirá cuáles te faltan." } },
            { Lesson.Refuges, new[]{
                "Más abajo hay refugios de errantes. Un brasero te devuelve aliento una sola vez; no lo desperdicies con la vida llena.",
                "Irel, el peregrino, cobra 180 de oro y lanza dardos a tus enemigos. Lumbre, el espíritu de ceniza, cobra 100 y te devuelve maná en combate. Puedes llevar a uno de cada uno hasta que el intento termine." } },
            { Lesson.Forge, new[]{
                "Hay un herrero, Brann. Martilla en la capilla del pacto desde 1-2. No vende armas: reforja la que empuñas.",
                "Si amas tu hoja, no la cambies: llévasela. Cada visita la sube un temple, de T1 a T4, y conserva lo que es. Con un arma de tu escuela te cobra menos." } },
            { Lesson.Cemetery, new[]{
                "Has visto el cementerio que sube y baja. Las escaleras esconden criptas laterales: luchar en ellas paga con reliquias.",
                "Y los muertos no son iguales: las crías de osario corren, las arañas de brasa aguantan. Mira sus colores antes de cargar." } },
            { Lesson.Horde, new[]{
                "Bajo la fundición acampa la Horda de los Colmillos. Orcos que comen hierro, trasgos que roban velas.",
                "En 5-1 un ogro de guerra guarda el foso; en 5-2 el Rey Trasgo te lanzará piedras y barro; en 5-3 te espera el Berserker Orco. No lo enfrentes con un arma que tu clase no domine." } },
        };
        static string List(PlayerClass c, WeaponAffinity a)
        {
            var kinds = System.Enum.GetValues(typeof(WeaponKind)).Cast<WeaponKind>().Where(k => ClassArsenal.Affinity(c, k) == a).Select(k => Codice.UI.UIWidgets.WeaponName(k).ToLowerInvariant()).ToList();
            if (c == PlayerClass.Shinobi && a == WeaponAffinity.Maestra) kinds.InsertRange(0, new[] { "katana", "shuriken" });
            if (c == PlayerClass.Berserker && a == WeaponAffinity.Maestra) kinds.Add("dos espadas a la vez");
            return kinds.Count == 0 ? "ninguna" : string.Join(", ", kinds);
        }
        /// Two pages explaining the chosen class's weapon affinities, technique and penalties.
        public static string[] Affinity(PlayerClass c)
        {
            var start = new CharacterState(); start.Initialize(c); var w = start.ActiveWeapon;
            string tech = ClassArsenal.Technique(c, w);
            return new[]{
                $"Te veo cargar como {ClassArsenal.ClassName(c).ToLowerInvariant()}. Cada escuela tiene sus armas. <color=#E7C46A>Maestras: {List(c,WeaponAffinity.Maestra)}</color>. <color=#8FC9A2>Diestras: {List(c,WeaponAffinity.Diestra)}</color>.",
                $"Con un arma ajena pierdes filo y ritmo; las <color=#DE6A5E>proscritas ({List(c,WeaponAffinity.Proscrita)})</color> ni siquiera podrás alzarlas. " + (tech != null ? $"Con tu arma de escuela despiertas una técnica: <color=#D6B77A>{tech}</color>." : "Busca el arma de tu escuela y despertará su técnica.")
            };
        }
        /// Builds this visit's speech: intro (full or short), every unlocked lesson not yet heard, then the class arsenal.
        public static List<string> Build(PlayerClass c, string[] intro, out List<Lesson> fresh)
        {
            var lines = new List<string>(); fresh = new List<Lesson>();
            bool first = !Heard(Lesson.Intro);
            if (first) lines.AddRange(intro);
            else lines.Add(Runs > 1 ? $"Otra vez tú. Ya colgué {Runs - 1} velas con tu nombre y sigues respirando. Bien." : "Otra vez tú. Bien.");
            // Like any game: the Vigía reads what is in your hands and teaches those controls.
            string hands = Codice.Mechanics.GameInput.UsingGamepad ? "pad" : "keys";
            if (first || PlayerPrefs.GetString(DeviceKey, "") != hands) { lines.Add(ControlsLesson(first)); PlayerPrefs.SetString(DeviceKey, hands); PlayerPrefs.Save(); }
            foreach (var l in Text.Keys) if (Available(l) && !Heard(l))
            {
                // On a first meeting the intro already greeted you: drop the "you came back" openers.
                foreach (var page in Text[l]) lines.Add(first ? page.Replace("Volviste. La ceniza no te recuerda, pero yo sí. Escucha: ", "Y algo que pocos saben: ").Replace("Has visto el cementerio", "Bajo el segundo mundo hay un cementerio") : page);
                fresh.Add(l);
            }
            lines.AddRange(Affinity(c));
            if (!first && fresh.Count == 0) lines.Add("No tengo nada nuevo para ti. Baja más hondo y volveré a hablar.");
            fresh.Add(Lesson.Intro);
            return lines;
        }
        const string DeviceKey = "Cinders.Vigia.Device";
        public static string ControlsLesson(bool first)
        {
            string L(Codice.Mechanics.GameAction a) => "<color=#E7C46A>" + Codice.Mechanics.GameInput.Label(a) + "</color>";
            if (Codice.Mechanics.GameInput.UsingGamepad)
                return (first ? "Veo un mando en tus manos" : "Ahora empuñas un mando") + $". {L(Codice.Mechanics.GameAction.Move)} te mueve y {L(Codice.Mechanics.GameAction.Look)} gira la mirada. {L(Codice.Mechanics.GameAction.Attack)} golpea, mantener {L(Codice.Mechanics.GameAction.HeavyAttack)} carga un golpe pesado, {L(Codice.Mechanics.GameAction.Block)} bloquea —justo antes del impacto es un parry—, {L(Codice.Mechanics.GameAction.Dodge)} esquiva y {L(Codice.Mechanics.GameAction.Jump)} salta. {L(Codice.Mechanics.GameAction.Interact)} toma lo que encuentres, {L(Codice.Mechanics.GameAction.Flask)} bebe del frasco y {L(Codice.Mechanics.GameAction.Special)} despierta el arte de tu arma. {L(Codice.Mechanics.GameAction.Pause)} abre tu inventario.";
            return (first ? "Tus manos están sobre las teclas" : "Volviste a las teclas") + $". {L(Codice.Mechanics.GameAction.Move)} te mueve y el ratón gira la mirada. Un clic izquierdo corto golpea. Mantén el botón y suéltalo para desatar el ataque cargado. También puedes mantener Z y soltarla. {L(Codice.Mechanics.GameAction.Block)} bloquea —justo antes del impacto es un parry—, {L(Codice.Mechanics.GameAction.Dodge)} esquiva y {L(Codice.Mechanics.GameAction.Jump)} salta. {L(Codice.Mechanics.GameAction.Interact)} toma lo que encuentres, {L(Codice.Mechanics.GameAction.Flask)} bebe del frasco y {L(Codice.Mechanics.GameAction.Special)} despierta el arte de tu arma. Si un día empuñas un mando, lo notaré.";
        }
        public static void MarkHeard(IEnumerable<Lesson> lessons)
        {
            int mask = PlayerPrefs.GetInt(HeardKey, 0); foreach (var l in lessons) mask |= 1 << (int)l;
            PlayerPrefs.SetInt(HeardKey, mask); PlayerPrefs.Save();
        }
        public static string Unlocks()
        {
            var parts = new List<string>();
            parts.Add(Heard(Lesson.Memories) ? "[+] Llaves" : "[-] Llaves");
            parts.Add(Heard(Lesson.Refuges) ? "[+] Refugios" : "[-] Refugios");
            parts.Add(ForgeRevealed ? "[+] Forja de Brann" : "[-] Forja");
            parts.Add(Heard(Lesson.Horde) ? "[+] La Horda" : "[-] ???");
            return string.Join("   ", parts);
        }
    }
}
