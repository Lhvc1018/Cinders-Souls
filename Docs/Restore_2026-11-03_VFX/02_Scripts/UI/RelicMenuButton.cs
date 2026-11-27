using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace Codice.UI
{
    public sealed class RelicMenuButton : Button
    {
        Tween pulse;
        protected override void DoStateTransition(SelectionState state,bool instant)
        {
            base.DoStateTransition(state,instant);
            var face=targetGraphic as RelicButtonFace;
            if(!face)return;
            face.highlighted=state==SelectionState.Highlighted||state==SelectionState.Selected||state==SelectionState.Pressed;
            face.pressed=state==SelectionState.Pressed;face.SetVerticesDirty();
            pulse?.Kill();
            var destination=state==SelectionState.Disabled?new Color(.38f,.38f,.38f):face.highlighted?Color.white:new Color(.76f,.73f,.70f);
            if(instant||!Application.isPlaying)face.color=destination;
            else pulse=DOTween.To(()=>face.color,c=>face.color=c,destination,.18f).SetUpdate(true);
        }
        protected override void OnDisable(){pulse?.Kill();base.OnDisable();}
    }
}
