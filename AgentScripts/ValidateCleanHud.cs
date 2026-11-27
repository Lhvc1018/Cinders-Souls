using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Codice.Characters;
using Codice.UI;
public static class ValidateCleanHud
{
    public static async Task<string> Run()
    {
        var hud=RunHUD.Instance;var state=hud.Player.State;var snapshot=state.Capture();
        var labels=hud.GetComponentsInChildren<TextMeshProUGUI>(true);
        TextMeshProUGUI Label(string n)=>labels.First(t=>t&&t.name==n);
        void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        int checks=0;
        try
        {
            foreach(PlayerClass kind in Enum.GetValues(typeof(PlayerClass)))
            {
                state.Initialize(kind);await Task.Delay(80);
                Check(hud.PlayerHealth.Caption.text.Contains("/"),"Missing health fraction");
                Check(hud.PlayerMana.gameObject.activeSelf,"Energy visibility "+kind);
                Check(!Label("Low health").enabled,"False low health");
                state.Damage(state.MaxHealth*.6f,true);await Task.Delay(80);
                Check(Label("Low health").enabled,"Missing low health "+kind);checks+=4;
                for(int slot=0;slot<2;slot++)if(state.SelectWeapon(slot))
                {
                    await Task.Delay(80);Check(Label("Equipped name").text.StartsWith(UIWidgets.WeaponName(state.ActiveWeapon)),"Held slot did not update");checks++;
                }
                state.Damage(state.Armor);state.SpendMana(state.MaxMana*.6f);await Task.Delay(80);
                Check(Label("Low armor").enabled&&Label("Low energy").enabled,"Missing armor or mana warning");checks++;
            }
            state.Initialize(PlayerClass.Warrior);state.ConsumeRelic(0);await Task.Delay(80);
            Check(labels.Any(t=>t&&t.name=="Relic label"&&t.isActiveAndEnabled&&t.text.EndsWith("s")),"Cooldown missing");checks++;
            foreach(var passive in new[]{PassiveKind.Overload,PassiveKind.MercuryBoots,PassiveKind.VampiricHeart})state.EquipPassive(passive);
            hud.SetPaused(true);await Task.Delay(100);Canvas.ForceUpdateCanvases();
            var overflow=string.Join(";",labels.Where(t=>t&&t.isActiveAndEnabled&&t.isTextOverflowing).Select(t=>t.name+": "+t.text));
            Check(overflow.Length==0,"Pause text overflow "+overflow);checks++;
            Check(Label("Current class").text.Contains("BENDICIONES PASIVAS"),"Passive inventory missing");checks++;
            string report=Screen.width+"x"+Screen.height+": "+checks+" checks passed for five classes, weapon switching, fractions, low health, cooldowns and passive inventory.";
            System.IO.File.AppendAllText("Docs/CleanHUD_Validation.txt",report+"\n");return report;
        }
        finally{state.Restore(snapshot);hud.SetPaused(false);Time.timeScale=0;}
    }
}
