using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SkillScrollSlotHover : MonoBehaviour, IPointerEnterHandler
{
    public SkillScrollSelectionUI menu;
    public int slot;
    public void OnPointerEnter(PointerEventData eventData){if(menu!=null)menu.SelectSlot(slot);}
}
