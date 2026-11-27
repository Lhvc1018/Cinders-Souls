using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Codice.Characters;
namespace Codice.UI
{
 public sealed class RunStatusHUD:MonoBehaviour
 {
  PlayerCharacter player;RunSpellbook spells;ElementalStatus status;RectTransform root;TextMeshProUGUI spellName,classStatus;readonly Image[] bars=new Image[4];const int MaxTiles=8;readonly TextMeshProUGUI[] captions=new TextMeshProUGUI[4],buffs=new TextMeshProUGUI[MaxTiles];readonly GameObject[] rows=new GameObject[4],tiles=new GameObject[MaxTiles];readonly Image[] frames=new Image[MaxTiles],timerFill=new Image[MaxTiles];readonly HudGlyph[] glyphs=new HudGlyph[MaxTiles];readonly System.Collections.Generic.List<BuffInfo> gathered=new System.Collections.Generic.List<BuffInfo>();
  void Start()
  {
   player=GetComponent<PlayerCharacter>();spells=GetComponent<RunSpellbook>();status=GetComponent<ElementalStatus>();
   var go=new GameObject("States and incantations",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);UIWidgets.ConfigureCanvas(go.GetComponent<Canvas>());go.GetComponent<Canvas>().sortingOrder=5;root=go.GetComponent<RectTransform>();
   // Active effects row under the vitals: pooled tiles filled from every IBuffSource on the player (spells, imbues, items...).
   for(int i=0;i<MaxTiles;i++){var t=UIWidgets.Image(root,"Active effect",58+i*56,128,50,50,new Color(.08f,.07f,.05f,.85f));tiles[i]=t.gameObject;frames[i]=t;glyphs[i]=UIWidgets.Rect(t.transform,"Effect sigil",12,4,26,26).gameObject.AddComponent<HudGlyph>();glyphs[i].raycastTarget=false;timerFill[i]=UIWidgets.Filled(t.transform,"Time left",0,47,50,3,ProceduralUISprites.Gold);buffs[i]=UIWidgets.Label(t.transform,"Duration",0,28,50,20,13);buffs[i].alignment=TextAlignmentOptions.Center;t.gameObject.SetActive(false);}
   for(int i=0;i<4;i++){var row=UIWidgets.Rect(root,"Element buildup",-170,-255+i*38,340,30);row.anchorMin=row.anchorMax=new Vector2(.5f,0);rows[i]=row.gameObject;UIWidgets.Image(row,"Well",0,0,340,8,new Color(.06f,.06f,.06f,.8f));bars[i]=UIWidgets.Filled(row,"Accumulation",0,0,340,8,new[]{new Color(.9f,.35f,.1f),Color.cyan,new Color(.5f,.65f,.18f),new Color(.6f,.65f,1)}[i]);captions[i]=UIWidgets.Label(row,"State",0,10,340,24,17);captions[i].alignment=TextAlignmentOptions.Center;}
   classStatus=UIWidgets.Label(root,"Class identity",58,284,570,32,17);
   spellName=UIWidgets.Label(root,"Selected incantation",58,212,440,70,19);
  }
  void Update()
  {
   if(!root)return;if(!spells)spells=GetComponent<RunSpellbook>();if(!status)status=GetComponent<ElementalStatus>();if(!spells||!status)return;bool visible=player.State.IsAlive&&!(RunHUD.Instance&&(RunHUD.Instance.Paused||RunHUD.Instance.ShopVisible||RunHUD.Instance.ResultsVisible));root.gameObject.SetActive(true);
   gathered.Clear();foreach(var src in GetComponents<IBuffSource>())src.CollectBuffs(gathered);
   if(player.State.WardRemaining>0)gathered.Add(new BuffInfo("",HudSymbol.Ward,new Color(.6f,.75f,1f),player.State.WardRemaining,Mathf.Max(player.State.WardRemaining,1)));
   // Passive blessings always show (gold frame); while one is triggered it counts down.
   for(int p=0;p<player.State.PassiveCount;p++){float pt=player.State.PassiveTime(p);string nm=UIWidgets.Passive(player.State.GetPassive(p));gathered.Add(new BuffInfo(nm.Length>5?nm.Substring(0,5):nm,HudSymbol.Special,new Color(.85f,.75f,.45f),pt>0?pt:999,pt>0?Mathf.Max(pt,8):0));}
   for(int i=0;i<MaxTiles;i++){bool on=visible&&i<gathered.Count;tiles[i].SetActive(on);if(!on)continue;var b=gathered[i];glyphs[i].Set(b.icon);glyphs[i].color=b.tint;frames[i].color=new Color(b.tint.r*.18f,b.tint.g*.18f,b.tint.b*.18f,.88f);
    buffs[i].text=b.remaining<99?Mathf.CeilToInt(b.remaining)+"s":b.label;timerFill[i].fillAmount=b.duration>0?Mathf.Clamp01(b.remaining/b.duration):1;timerFill[i].color=b.tint;
    if(b.remaining<3&&b.remaining>0)tiles[i].transform.localScale=Vector3.one*(1+.06f*Mathf.Sin(Time.time*14));else tiles[i].transform.localScale=Vector3.one;}
   for(int i=0;i<4;i++){float remain=Mathf.Max(0,status.Until[i+1]-Time.time);rows[i].SetActive(visible&&(status.Buildup[i+1]>0||remain>0));bars[i].fillAmount=remain>0?1:status.Buildup[i+1]/100;captions[i].text=new[]{"QUEMADURA","CONGELACIÓN","VENENO","ELECTROCUCIÓN"}[i]+(remain>0?" · "+Mathf.CeilToInt(remain)+"s":"");}
   classStatus.gameObject.SetActive(visible);var identity=GetComponent<ClassIdentity>();classStatus.text=identity?identity.Status+identity.Arsenal:"";
   spellName.gameObject.SetActive(visible);spellName.text=spells.Known.Count>0?RunSpellbook.Names[(int)spells.Known[spells.Selected]]+"\n[C] Cambiar · [T] Lanzar · "+RunSpellbook.Costs[(int)spells.Known[spells.Selected]]+" maná":"";
  }
  void OnDestroy(){if(root)Destroy(root.gameObject);}
 }
}
