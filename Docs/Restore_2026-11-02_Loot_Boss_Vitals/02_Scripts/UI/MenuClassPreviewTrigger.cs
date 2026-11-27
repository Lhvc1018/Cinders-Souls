using UnityEngine;
using UnityEngine.EventSystems;

namespace Codice.UI
{
    public sealed class MenuClassPreviewTrigger : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public MainMenuUI menu;
        public int index;
        public void OnPointerEnter(PointerEventData eventData) { if (menu) menu.PreviewClass(index); }
        public void OnSelect(BaseEventData eventData) { if (menu) menu.PreviewClass(index); }
    }
}
