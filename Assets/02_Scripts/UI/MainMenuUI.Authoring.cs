using UnityEngine;using TMPro;using UnityEngine.UI;
namespace Codice.UI{
 public sealed partial class MainMenuUI{
  [SerializeField,Tooltip("Edit this hierarchy outside Play Mode. Runtime applies its layout and labels, retaining functional buttons.")] Transform authoredLayout;
  [ContextMenu("Generar menú editable")]
  public void GenerateEditableMenu(){
   if(Application.isPlaying)return;
#if UNITY_EDITOR
   if(authoredLayout)UnityEditor.Undo.DestroyObjectImmediate(authoredLayout.gameObject);
   var old=transform.Find("Main menu canvas");if(old)UnityEditor.Undo.DestroyObjectImmediate(old.gameObject);
   Start();authoredLayout=transform.Find("Main menu canvas");authoredLayout.name="MENU EDITABLE";
   foreach(var group in authoredLayout.GetComponentsInChildren<CanvasGroup>(true))group.alpha=1;
   UnityEditor.EditorUtility.SetDirty(this);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
  }
  void ApplyAuthoredLayout(Transform target){if(!authoredLayout)return;CopyLayout(authoredLayout,target);authoredLayout.gameObject.SetActive(false);}
  static void CopyLayout(Transform source,Transform target){
   if(source is RectTransform a&&target is RectTransform b){b.anchorMin=a.anchorMin;b.anchorMax=a.anchorMax;b.pivot=a.pivot;b.sizeDelta=a.sizeDelta;b.anchoredPosition=a.anchoredPosition;b.localRotation=a.localRotation;b.localScale=a.localScale;}
   var st=source.GetComponent<TextMeshProUGUI>();var tt=target.GetComponent<TextMeshProUGUI>();if(st&&tt){tt.text=st.text;tt.font=st.font;tt.fontSize=st.fontSize;tt.color=st.color;tt.alignment=st.alignment;tt.characterSpacing=st.characterSpacing;}
   var si=source.GetComponent<Image>();var ti=target.GetComponent<Image>();if(si&&ti){ti.sprite=si.sprite;ti.color=si.color;}
   var sr=source.GetComponent<RawImage>();var tr=target.GetComponent<RawImage>();if(sr&&tr&&sr.texture is Texture2D){tr.texture=sr.texture;tr.color=sr.color;}
   foreach(Transform child in source){var match=target.Find(child.name);if(match)CopyLayout(child,match);}
  }
 }
}
