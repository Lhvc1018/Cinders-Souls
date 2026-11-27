using UnityEngine;
namespace Codice.Characters
{
    /// Grado de afinidad entre una clase y un tipo de arma.
    public enum WeaponAffinity { Proscrita = -1, Torpe = 0, Competente = 1, Diestra = 2, Maestra = 3 }

    /// Sistema definitivo clase ↔ arma: cada clase tiene una tabla de afinidad por tipo de arma que escala daño,
    /// cadencia y probabilidad de crítico; las armas proscritas no pueden empuñarse (ni aparecen en el botín) y
    /// cada clase obtiene una técnica exclusiva solo cuando lucha con el arma de su escuela.
    public static class ClassArsenal
    {
        //                      Sword Axe Bow Dagger Hammer Polearm Scythe Shield Staff Musket
        static readonly int[,] table = {
            /* Warrior   */ {  3,  3,  0,  1,  2,  2,  1,  3, -1,  0 },
            /* Tank      */ {  1,  2, -1, -1,  3,  2,  1,  3,  0,  1 },
            /* Archer    */ {  0,  0,  3,  2, -1,  1,  0, -1,  1,  2 },
            /* Mage      */ {  0, -1,  1,  1, -1,  0,  2, -1,  3, -1 },
            /* Shinobi   */ {  1,  0,  1,  2, -1,  1,  1, -1, -1, -1 },
            /* Berserker */ {  2,  3, -1,  1,  2,  1,  2, -1, -1, -1 },
        };
        static readonly float[] damage = { .72f, .85f, .93f, 1f }, speed = { .85f, .93f, .97f, 1f }, crit = { 0, 0, .04f, .08f };

        public static WeaponAffinity Affinity(PlayerClass c, WeaponInstance w)
        {
            if (w == null) return WeaponAffinity.Competente;
            if (c == PlayerClass.Shinobi && (w.katana || w.shuriken)) return WeaponAffinity.Maestra;
            if (w.katana && c != PlayerClass.Shinobi) return c == PlayerClass.Berserker || c == PlayerClass.Warrior ? WeaponAffinity.Competente : WeaponAffinity.Torpe;
            if (w.shuriken) return c == PlayerClass.Archer ? WeaponAffinity.Diestra : WeaponAffinity.Torpe;
            return (WeaponAffinity)table[(int)c, (int)w.kind];
        }
        public static WeaponAffinity Affinity(PlayerClass c, WeaponKind k) => (WeaponAffinity)table[(int)c, (int)k];
        public static bool Forbidden(PlayerClass c, WeaponInstance w) => Affinity(c, w) == WeaponAffinity.Proscrita;
        static int Index(WeaponAffinity a) => Mathf.Clamp((int)a, 0, 3);
        public static float DamageFactor(CharacterState s, WeaponInstance w)
        {
            var a = Effective(s, w); float f = damage[Index(a)];
            // Berserker · Sed de sangre: por debajo del 50 % de vida sus armas maestras golpean un 20 % más.
            if (s.Class == PlayerClass.Berserker && a == WeaponAffinity.Maestra && s.Health < s.MaxHealth * .5f) f *= 1.2f;
            return f;
        }
        public static float SpeedFactor(CharacterState s, WeaponInstance w) => speed[Index(Effective(s, w))];
        public static float CritBonus(CharacterState s, WeaponInstance w) => crit[Index(Effective(s, w))];
        /// Berserker con dos espadas cuenta como su escuela maestra.
        public static WeaponAffinity Effective(CharacterState s, WeaponInstance w) => s.Class == PlayerClass.Berserker && s.DualWield && w == s.ActiveWeapon ? WeaponAffinity.Maestra : Affinity(s.Class, w);

        /// Rerolls a looted weapon kind until the class can wield it (deterministic per seed value).
        public static WeaponKind Usable(PlayerClass c, WeaponKind k, int salt = 0)
        {
            for (int i = 0; i < 10; i++) { var kind = (WeaponKind)(((int)k + i * 3 + salt) % 10); if (Affinity(c, kind) != WeaponAffinity.Proscrita) return kind; }
            return WeaponKind.Sword;
        }
        public static string Label(WeaponAffinity a) => a == WeaponAffinity.Maestra ? "MAESTRA" : a == WeaponAffinity.Diestra ? "DIESTRA" : a == WeaponAffinity.Competente ? "COMPETENTE" : a == WeaponAffinity.Torpe ? "TORPE" : "PROSCRITA";
        public static string Color(WeaponAffinity a) => a == WeaponAffinity.Maestra ? "#E7C46A" : a == WeaponAffinity.Diestra ? "#8FC9A2" : a == WeaponAffinity.Competente ? "#C9C2B4" : a == WeaponAffinity.Torpe ? "#D9A06B" : "#DE6A5E";
        public static string Describe(CharacterState s, WeaponInstance w)
        {
            var a = Effective(s, w); if (a == WeaponAffinity.Proscrita) return $"<color={Color(a)}>PROSCRITA · {ClassName(s.Class)} no puede empuñarla</color>";
            int i = Index(a);
            string text = $"<color={Color(a)}>Afinidad {Label(a)}</color> · daño x{damage[i]:0.00} · cadencia x{speed[i]:0.00}" + (crit[i] > 0 ? $" · +{crit[i] * 100:0}% crítico" : "");
            string perk = Technique(s.Class, w);
            return perk != null ? text + "\n<color=#D6B77A>" + perk + "</color>" : text;
        }
        public static string ClassName(PlayerClass c) => c == PlayerClass.Warrior ? "El Caballero" : c == PlayerClass.Tank ? "El Tanque" : c == PlayerClass.Archer ? "El Arquero" : c == PlayerClass.Mage ? "El Mago" : c == PlayerClass.Shinobi ? "El Shinobi" : "El Assasin";
        /// Técnica exclusiva de clase: solo se activa con el arma de su escuela.
        public static string Technique(PlayerClass c, WeaponInstance w)
        {
            if (w == null) return null;
            switch (c)
            {
                case PlayerClass.Warrior: return w.kind == WeaponKind.Sword && !w.katana ? "Juramento del acero: represalia con ventana ampliada; con escudo en la otra ranura recibes −15 %" : w.kind == WeaponKind.Shield ? "Muro del juramento: −15 % de daño cuerpo a cuerpo recibido" : null;
                case PlayerClass.Tank: return w.kind == WeaponKind.Hammer ? "Bastión: cada golpe de martillo suma +12 de firmeza" : null;
                case PlayerClass.Archer: return w.kind == WeaponKind.Bow && !w.shuriken ? "Ojo de halcón: la precisión se carga un 35 % más rápido" : w.kind == WeaponKind.Dagger ? "Remate del cazador: +35 % por la espalda" : null;
                case PlayerClass.Mage: return w.kind == WeaponKind.Staff ? "Canalizador: los conjuros cuestan un 25 % menos de maná" : w.kind == WeaponKind.Scythe ? "Siega de almas: cada tajo devuelve 4 de maná" : null;
                case PlayerClass.Shinobi: return w.katana ? "Filo del lobo: el katana rompe la postura un 40 % más rápido" : w.shuriken ? "Estrellas del lobo: shuriken con afinidad maestra" : null;
                case PlayerClass.Berserker: return w.kind == WeaponKind.Axe || w.kind == WeaponKind.Sword ? "Sed de sangre: bajo el 50 % de vida, +20 % de daño con armas maestras" : null;
            }
            return null;
        }
        // ---- runtime hooks used by PlayerCombat / ClassIdentity / StaffSpells ----
        public static float ManaCostFactor(CharacterState s, WeaponInstance w) => s.Class == PlayerClass.Mage && w != null && w.kind == WeaponKind.Staff ? .75f : 1;
        public static float PostureFactor(CharacterState s, WeaponInstance w) => s.Class == PlayerClass.Shinobi && w != null && w.katana ? 1.4f : 1;
        public static float FocusRate(CharacterState s, WeaponInstance w) => s.Class == PlayerClass.Archer && w != null && w.kind == WeaponKind.Bow && !w.shuriken ? 1.35f : 1;
        public static float GuardFactor(CharacterState s) => s.Class == PlayerClass.Warrior && (Has(s, WeaponKind.Shield) || s.ActiveWeapon?.kind == WeaponKind.Shield) ? .85f : 1;
        public static float RiposteWindow(CharacterState s) => s.Class == PlayerClass.Warrior && s.ActiveWeapon != null && s.ActiveWeapon.kind == WeaponKind.Sword && !s.ActiveWeapon.katana ? .5f : .38f;
        static bool Has(CharacterState s, WeaponKind k) { for (int i = 0; i < 2; i++) if (s.GetWeapon(i)?.kind == k) return true; return false; }
        /// Melee hit side effects and the matching multiplier.
        public static float OnMeleeHit(PlayerCharacter p, WeaponInstance w, Component target, bool backstab)
        {
            var s = p.State; float mult = 1;
            if (s.Class == PlayerClass.Tank && w.kind == WeaponKind.Hammer) p.GetComponent<ClassIdentity>()?.AddFirmness(12);
            if (s.Class == PlayerClass.Mage && w.kind == WeaponKind.Scythe) s.RestoreMana(4);
            if (s.Class == PlayerClass.Archer && w.kind == WeaponKind.Dagger && target && backstab) mult *= 1.35f;
            return mult;
        }
    }
}
