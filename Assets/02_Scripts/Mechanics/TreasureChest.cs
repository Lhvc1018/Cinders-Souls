using Codice.Characters;
using UnityEngine;

namespace Codice.Mechanics
{
    public sealed class TreasureChest : MonoBehaviour, IPlayerInteractable
    {
        [Min(1)] public int price = 60;
        public bool Opened { get; private set; }
        public bool expeditionReward,relicReward,activeReward;
        public int rewardSeed,rewardTier=1,expeditionRoom=-1;
        public string Message { get; private set; }
        [SerializeField] Transform lootSocket;
        public Vector3 LootOrigin => lootSocket ? lootSocket.position : transform.position + Vector3.up * .35f;
        Vector3 lootDestination;
        void SpawnWeapon(PlayerCombat combat, WeaponInstance weapon)
        {
            var pickup = combat.CreatePickup(weapon, LootOrigin);
            pickup.BeginChestReveal(lootDestination, .7f);
        }
        Transform lid;
        Quaternion closedRotation;
        float openProgress;
        public bool TryOpen(PlayerCharacter player)
        {
            if(Opened || !player || !player.State.IsAlive || player.ControlsBlocked || Time.timeScale == 0 || Codice.UI.RunNavigation.Loading ||
                Vector3.Distance(player.transform.position,transform.position)>4) return false;
            var combat=player.GetComponent<PlayerCombat>();
            if(!combat)return false;
            if(player.State.Gold<price){Message="Necesitas "+price+" monedas";Codice.Presentation.GameAudio.Play(Codice.Presentation.SoundCue.Error);return false;}
            Opened=true;
            if(!player.State.SpendGold(price)){Opened=false;return false;}
            Codice.Presentation.GameAudio.PlayAt(Codice.Presentation.SoundCue.Chest,transform.position);
            for(int i=0;i<5;i++){float angle=i*Mathf.PI*2/5;var orb=LootPickup.Create(LootOrigin,LootKind.Armor,3);orb.gameObject.AddComponent<ChestLootReveal>().Initialize(LootOrigin,transform.position+new Vector3(Mathf.Cos(angle)*.8f,.2f,Mathf.Sin(angle)*.8f));}
            if(expeditionReward)
            {
                Vector3 exit=Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up).normalized;
                Vector3 point=transform.position+exit*1.3f+Vector3.up*.15f;
                if(relicReward){var altar=RelicAltar.Create(point,rewardSeed,activeReward);altar.Interact(player);}
                else {
                    var rng=new System.Random(rewardSeed);int roll=rng.Next(100);
                    if(roll<45)SpawnWeapon(combat,new WeaponInstance((WeaponKind)rng.Next(0,10),Mathf.Clamp(rewardTier,1,3)));
                    // Talismans (rings / amulets) are rarer: they go straight to the inventory.
                    if(rng.Next(100)<16){var t=(Talisman)rng.Next(TalismanCatalog.Count);if(player.State.GrantTalisman(t))Codice.UI.RunHUD.Instance?.Announce("TALISMÁN · "+TalismanCatalog.All[(int)t].name+(player.State.IsTalismanEquipped(t)?" · equipado":" · en tu inventario"));}
                    // Ash boons can spill out of weapon chests too.
                    if(rng.Next(100)<30)Codice.Expedition.AshBoonOrb.Create((AshBoon)rng.Next(BoonCatalog.Count),point+Vector3.up*1.1f+Vector3.Cross(Vector3.up,exit)*1.2f,transform.parent);
                    else {var pickup=LootPickup.Create(LootOrigin,roll<95?LootKind.Passive:LootKind.Potion,1);if(roll<95)pickup.passive=(PassiveKind)rng.Next(5);pickup.gameObject.AddComponent<ChestLootReveal>().Initialize(LootOrigin,point);}
                }
                Message="Botín y fragmentos de armadura · Recoger";
                Debug.Log("[Expedition chest] room="+expeditionRoom+" tier="+rewardTier+" relic="+relicReward+" seed="+rewardSeed);
                return true;
            }
            int level=RunDirector.Instance?RunDirector.Instance.Level:1;
            int tier=Mathf.Clamp(1+(level-1)/5+Random.Range(0,2),1,4);
            var weapon=new WeaponInstance((WeaponKind)Random.Range(0,10),tier);
            Vector3 direction=Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up).normalized;
            SpawnWeapon(combat,weapon);
            Message="Cofre abierto • Recoge el arma con E";
            return true;
        }
        public void ApplyLan(bool opened){Opened=opened;}
        public void Interact(PlayerCharacter player) { TryOpen(player); }
        void Awake()
        {
            var renderers=GetComponentsInChildren<Renderer>();
            if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);if(bounds.size.x>.001f)transform.localScale*=1.05f/bounds.size.x;}
            var scaled=GetComponentsInChildren<Renderer>();
            var contentBounds = new Bounds(transform.position + Vector3.up * .3f, new Vector3(1,.6f,.6f));
            if(scaled.Length>0){contentBounds=scaled[0].bounds;foreach(var r in scaled)contentBounds.Encapsulate(r.bounds);}
            if(!lootSocket){lootSocket=new GameObject("Loot socket").transform;lootSocket.SetParent(transform,true);lootSocket.position=contentBounds.center;}
            lootDestination = new Vector3(LootOrigin.x, contentBounds.max.y + .5f, LootOrigin.z);
            foreach(var child in GetComponentsInChildren<Transform>())
                if(child!=transform && (child.name.ToLowerInvariant().Contains("lid") || child.name.ToLowerInvariant().Contains("top")))
                {lid=child;closedRotation=lid.localRotation;break;}
        }
        void Update()
        { if(Opened && lid){openProgress=Mathf.MoveTowards(openProgress,1,Time.deltaTime*2);lid.localRotation=closedRotation*Quaternion.Euler(-75*openProgress,0,0);} }
        public static void InstallInScene()
        {
            foreach(var candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                // Character rigs also contain a bone named "chest"; it is not loot.
                if (candidate.GetComponentInParent<PlayerCharacter>() || candidate.GetComponentInParent<Codice.Enemies.BasicEnemy>() ||
                    candidate.GetComponentInParent<Codice.Bosses.BossCharacter>() || candidate.GetComponentInParent<Animator>()) continue;
                if(!(candidate.name.Equals("chest_gold",System.StringComparison.OrdinalIgnoreCase) || candidate.name.Equals("chest",System.StringComparison.OrdinalIgnoreCase)) ||
                    (candidate.parent && (candidate.parent.name.Equals("chest_gold",System.StringComparison.OrdinalIgnoreCase) || candidate.parent.name.Equals("chest",System.StringComparison.OrdinalIgnoreCase))) || candidate.GetComponentInParent<TreasureChest>())continue;
                var renderers=candidate.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                candidate.gameObject.AddComponent<TreasureChest>();
                bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                var interaction=new GameObject("Chest interaction");interaction.transform.position=bounds.center;interaction.transform.SetParent(candidate,true);
                var box=interaction.AddComponent<BoxCollider>();box.size=bounds.size+Vector3.one*.12f;box.isTrigger=true;
            }
        }
    }
}
