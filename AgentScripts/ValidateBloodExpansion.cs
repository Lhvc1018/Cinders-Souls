using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Codice.Characters;
using Codice.Mechanics;
public static class ValidateBloodExpansion
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string StateChecks()
    {
        int count=0;
        foreach(PlayerClass kind in Enum.GetValues(typeof(PlayerClass)))
        {
            var s=new CharacterState();s.Initialize(kind);float max=s.MaxArmor;
            Check(s.Armor==max,"Initial armor");s.Damage(10);Check(s.Armor==max-10&&s.Health==s.MaxHealth,"Armor absorbs first");
            s.Damage(max);Check(s.Armor==0&&s.Health==s.MaxHealth-10,"Overflow reaches health");
            s.TickMana(5.9f);Check(s.Armor==0,"Early regeneration");s.TickMana(.2f);Check(Mathf.Abs(s.Armor-.5f)<.001f,"Delayed regeneration");
            s.TickMana(100);Check(s.Armor==max,"Armor bounded");s.Damage(4,true);Check(s.Armor==max&&s.Health==s.MaxHealth-14,"Poison bypasses armor");
            float mana=s.Mana;Check(s.ConsumeRelic(0)&&s.Mana==mana-RelicCatalog.ManaCost(s.GetRelic(0)),"Relic mana cost");
            mana=s.Mana;Check(!s.ConsumeRelic(0)&&s.Mana==mana,"Cooldown prevents double charge");s.SpendMana(s.Mana);Check(!s.ConsumeRelic(1),"No mana denial");
            var copy=new CharacterState();copy.Initialize(kind);copy.Restore(s.Capture());Check(copy.Armor==s.Armor&&copy.Mana==s.Mana,"Resource snapshot");
            Check(kind==PlayerClass.Shinobi||copy.GetWeapon(1)==null,"Phantom weapon after snapshot");count+=12;
        }
        string report=count+" checks passed across five classes: armor absorption, overflow, delay, regeneration cap, poison, mana costs and snapshots.";
        System.IO.File.WriteAllText("Docs/BloodExpansion_StateValidation.txt",report);return report;
    }
    static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    public static async Task<string> Loot()
    {
        var p=Codice.UI.RunHUD.Instance.Player;Time.timeScale=1;p.enabled=false;var report=new System.Text.StringBuilder();
        foreach(var chest in UnityEngine.Object.FindObjectsByType<TreasureChest>())Check(Mathf.Abs(Bounds(chest.gameObject).size.x-1.05f)<.015f,"Chest width inconsistent");
        foreach(var kind in new[]{LootKind.Arrows,LootKind.Passive,LootKind.Mana,LootKind.Ammunition})
        {
            var pickup=LootPickup.Create(p.transform.position+Vector3.right*5,kind,6);await Task.Delay(150);var b=Bounds(pickup.gameObject);float longest=Mathf.Max(b.size.x,b.size.y,b.size.z);
            Check(longest<=(kind==LootKind.Arrows?.46f:.3f),"Oversized pickup "+kind+" "+longest);report.AppendLine(kind+" max size="+longest.ToString("F3")+"m");UnityEngine.Object.Destroy(pickup.gameObject);
        }
        var bottle=Resources.Load<GameObject>("CindersDungeon/EstusBottle");var instance=UnityEngine.Object.Instantiate(bottle);Check(Mathf.Abs(Bounds(instance).size.y-.18f)<.01f,"Potion height");UnityEngine.Object.Destroy(instance);
        int gold=p.State.Gold;int coins=UnityEngine.Object.FindObjectsByType<GoldPickup>().Length;Check(GoldPickup.Award(7,p)&&p.State.Gold==gold+7&&UnityEngine.Object.FindObjectsByType<GoldPickup>().Length==coins,"Direct gold credit");
        report.AppendLine("Potion height=0.18m; chests width=1.05m; direct gold credited without world coins.");p.enabled=true;
        System.IO.File.WriteAllText("Docs/BloodExpansion_LootValidation.txt",report.ToString());return report.ToString();
    }
}
