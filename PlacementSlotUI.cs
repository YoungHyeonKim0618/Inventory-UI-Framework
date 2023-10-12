
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlacementSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PlacementGridUI gridUI;
    public PlacementSlotState myState;
    public Image myImage;
    public IPlacementSlotUISprites spritesData;


    public void SetSlotState(PlacementSlotState state, int rarity = -1)
    {
        myState = state;
        RefreshImage(rarity);
    }

    public void SetColor(Color color)
    {
        myImage.color = color;
    }

    public void ResetColor()
    {
        myImage.color = Color.white;
    }

    protected virtual void RefreshImage(int rarity)
    {
        switch (myState)
        {
            case PlacementSlotState.EMPTY:
                myImage.sprite = spritesData.GetEmptySlotSprite();
                break;
            case PlacementSlotState.FULL:
                switch (rarity)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        myImage.sprite = spritesData.GetSpritesByRarity()[rarity];
                        break;
                    
                    default:
                        myImage.sprite = spritesData.GetFullSlotSprite();
                        break;
                }
                break;
            case PlacementSlotState.LOCKED:
                myImage.sprite = spritesData.GetLockedSlotSprite();
                break;
            case PlacementSlotState.DISABLED:
                myImage.sprite = spritesData.GetDisabledSlotSprite();
                break;
            case PlacementSlotState.EXPANDABLE:
                myImage.sprite = spritesData.GetExpandableSlotSprite();
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myState == PlacementSlotState.EXPANDABLE)
            myImage.sprite = spritesData.GetExpandableSlotPointerOnSprite();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (myState == PlacementSlotState.EXPANDABLE)
            myImage.sprite = spritesData.GetExpandableSlotSprite();
    }

}

public enum PlacementSlotState
{
    // 해금되고 장착된 IPlaceable이 없는 상태
    EMPTY = 0,
    // 해금되고 IPlaceable이 장착된 상태
    FULL = 1,
    // 잠김 상태
    LOCKED = 2,
    // 해금되었지만 패널티 등으로 인해 사용 불가인 상태
    DISABLED = 3,
    // 확장 가능함. LOCKED와 같이 동작하지만, 해금 시 EMPTY로 바뀜.
    EXPANDABLE = 4,
}

/*
 * 슬롯의 Sprite 정보를 담은 구조체.
 * ScriptableObject를 만들고 상속하는 걸 추천함.
 */
public interface IPlacementSlotUISprites
{
    public Sprite GetEmptySlotSprite();
    public List<Sprite> GetSpritesByRarity();
    public Sprite GetFullSlotSprite();
    public Sprite GetLockedSlotSprite();
    public Sprite GetDisabledSlotSprite();
    
    public Sprite GetExpandableSlotSprite();
    public Sprite GetExpandableSlotPointerOnSprite();
}