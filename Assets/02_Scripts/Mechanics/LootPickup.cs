using Codice.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Codice.Mechanics
{
    public enum LootKind { Ammunition, Passive, Arrows, Shuriken, Mana, Armor, Potion }
    public sealed class LootPickup : MonoBehaviour, IPlayerInteractable
    {
        public LootKind kind;
        public bool IsAmmo => kind != LootKind.Passive;
        public bool Supports(WeaponInstance weapon)=>kind==LootKind.Armor||kind==LootKind.Potion||kind==LootKind.Mana||weapon!=null&&(kind==LootKind.Ammunition||kind==LootKind.Arrows&&weapon.kind==WeaponKind.Bow&&!weapon.shuriken||kind==LootKind.Shuriken&&weapon.shuriken);
        public string AmmoName=>kind==LootKind.Armor?"FRAGMENTO DE ARMADURA":kind==LootKind.Potion?"CARGA DE FRASCO":kind==LootKind.Mana?"ESENCIA AZUL":kind==LootKind.Arrows?"FLECHAS":kind==LootKind.Shuriken?"SHURIKEN":"CARTUCHOS";
        public PassiveKind passive;
        public int amount = 4;
        public bool respawns;
        public float respawnSeconds = 15;
        public GameObject visual;
        public bool Available => Time.time >= readyAt;
        float readyAt;
        bool consumed;
        Vector3 visualRest;
        float visualPhase;
        void Start()
        {
            var library = Codice.Presentation.PresentationLibrary.Load();
            var prefab = library ? (kind == LootKind.Ammunition ? library.ammunitionModel :
                library.passiveModels != null && (int)passive < library.passiveModels.Length ? library.passiveModels[(int)passive] : null) : null;
            if(kind==LootKind.Arrows||kind==LootKind.Ammunition)prefab=Resources.Load<GameObject>("CindersDungeon/ArrowBundle");
            if(kind==LootKind.Ammunition)prefab=Resources.Load<GameObject>("CindersDungeon/Cartridges");
            if(kind==LootKind.Shuriken)prefab=Resources.Load<GameObject>("Shuriken");
            if(kind==LootKind.Passive)prefab=Resources.Load<GameObject>("CindersDungeon/Passive"+(int)passive);
            if(kind==LootKind.Mana)prefab=Resources.Load<GameObject>("CindersDungeon/ManaShard");
            if(kind==LootKind.Armor)prefab=null;
            if(kind==LootKind.Potion)prefab=Resources.Load<GameObject>("CindersDungeon/EstusBottle");
            if (prefab)
            {
                if (visual) Destroy(visual);
                visual = Instantiate(prefab, transform, false);
                visual.name = kind == LootKind.Passive ? "Power-up • " + passive : "Munición";
                PickupScale.Fit(visual,kind==LootKind.Arrows?.44f:.28f);
            }
            if (visual) visualRest = visual.transform.localPosition;
            visualPhase = Random.value * Mathf.PI * 2;
        }
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player&&IsAmmo) TryCollect(player);
        }
        void Update()
        {
            if(kind==LootKind.Armor&&!consumed&&!Codice.Multiplayer.LanSession.Client){var collector=Codice.Multiplayer.LanSession.Host?Codice.Multiplayer.LanSession.Instance.NearestPlayer(transform.position):RunDirector.Instance?.Player as PlayerCharacter;if(collector&&Vector3.Distance(transform.position,collector.transform.position)<1.6f)TryCollect(collector);}   /* coop: the guest collects armor too */
            if (visual && !consumed)
            {
                visual.transform.localPosition = visualRest + Vector3.up * (.10f + Mathf.Sin(Time.time * 2 + visualPhase) * .08f);
                visual.transform.Rotate(0, 35 * Time.deltaTime, 0, Space.Self);
            }
            if (respawns && consumed && Available)
            { consumed = false; SetVisible(true); }
        }
        void SetVisible(bool visible)
        { if (visual) visual.SetActive(visible); var collider = GetComponent<Collider>(); if (collider) collider.enabled = visible; }
        public void Interact(PlayerCharacter player)
        {
            if(kind==LootKind.Passive&&!Codice.Multiplayer.LanSession.Active&&Codice.UI.RunHUD.Instance){Codice.UI.RunHUD.Instance.OpenPassiveChoice(this);return;}
            var keys = Keyboard.current;
            bool remote = Codice.Multiplayer.LanAvatar.IsRemote(player);   /* coop guest: the host's keyboard must not choose its slot */
            int slot = remote ? -1 : keys != null && keys.leftShiftKey.isPressed ? 0 : keys != null && keys.leftCtrlKey.isPressed ? 1 : keys != null && keys.leftAltKey.isPressed ? 2 : -1;
            if (kind == LootKind.Passive && remote && player.State.PassiveCount >= 3) { var old = player.State.GetPassive(0); if (TryCollect(player, 0)) Codice.UI.RunHUD.Instance?.Announce("Pasivas llenas · " + old + " reemplazada por " + passive); return; }
            if (!TryCollect(player, slot)) Debug.Log("Botín: selecciona un arma con munición faltante o usa Shift/Ctrl/Alt + E para reemplazar pasiva 1/2/3.", this);
        }
        public bool TryCollect(PlayerCharacter player, int replaceSlot = -1)
        {
            if (consumed || !Available || !player || !player.State.IsAlive || player.ControlsBlocked || Time.timeScale == 0 || Vector3.Distance(transform.position, player.transform.position) > 3.5f) return false;
            bool received = kind==LootKind.Armor?player.State.RestoreArmor(amount):kind==LootKind.Potion?(player.State.AddEstusCharge()||player.State.Heal(player.State.MaxHealth*.25f)):kind==LootKind.Mana?player.State.RestoreMana(amount):IsAmmo ? player.State.AddAmmunition(amount,kind==LootKind.Arrows?1:kind==LootKind.Shuriken?2:0) : player.State.EquipPassive(passive, replaceSlot);
            if (!received) return false;
            Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Pickup);
            consumed = true; SetVisible(false);
            if (respawns) readyAt = Time.time + respawnSeconds; else Destroy(gameObject);
            return true;
        }
        public static LootPickup Create(Vector3 position, LootKind kind, int amount)
        {
            var obj = new GameObject(kind == LootKind.Passive ? "Sello de pasiva (E)" : "Munición (E)");
            obj.transform.position = position;
            var pickup = obj.AddComponent<LootPickup>(); pickup.kind = kind; pickup.amount = amount;
            pickup.passive = (PassiveKind)Random.Range(0, 5);
            var trigger = obj.AddComponent<SphereCollider>(); trigger.radius = .4f; trigger.isTrigger = true;
            var body = obj.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var visual = GameObject.CreatePrimitive(kind == LootKind.Passive||kind==LootKind.Armor ? PrimitiveType.Sphere : PrimitiveType.Cube);
            visual.name = "Pickup indicator"; Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(obj.transform, false); visual.transform.localScale = Vector3.one * (kind==LootKind.Armor?.11f:.3f);
            if(kind==LootKind.Armor){var material=ArmorMaterial;visual.GetComponent<Renderer>().sharedMaterial=material;}
            pickup.visual = visual; return pickup;
        }
        static Material armorMaterial;
        public static Material ArmorMaterial {get{if(!armorMaterial){armorMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));armorMaterial.SetColor("_BaseColor",new Color(.65f,.8f,.9f));armorMaterial.EnableKeyword("_EMISSION");armorMaterial.SetColor("_EmissionColor",new Color(.3f,.55f,.8f)*2);}return armorMaterial;}}
        public static void DropFromEnemy(PlayerCharacter player, Vector3 position)
        {
            if (!RunDirector.Instance || RunDirector.Instance.Player != player) return;
            float roll = Random.value;
            if (roll < .12f)
            {
                var floor=RunDirector.Instance.Expedition;int tier=floor?(floor.CurrentRoom<=2?1:2):Mathf.Clamp(2+(RunDirector.Instance.Level-1)/5,2,4);
                player.GetComponent<PlayerCombat>().CreatePickup(new WeaponInstance((WeaponKind)Random.Range(0, 10),tier),position+Vector3.up*.3f);
            }
            else if (roll < .60f) {
                var held=player.State.ActiveWeapon;
                var type=held!=null&&held.shuriken?LootKind.Shuriken:held!=null&&held.kind==WeaponKind.Musket?LootKind.Ammunition:held!=null&&held.kind==WeaponKind.Staff?LootKind.Mana:LootKind.Arrows;
                Create(position+Vector3.up*.3f,type,type==LootKind.Mana?20:type==LootKind.Ammunition?3:6);
            }
        }
    }
}
