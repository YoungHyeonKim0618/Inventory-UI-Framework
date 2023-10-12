
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/*
 * PlaceableUI가 자식으로 가지는 클래스. IPlaceable의 GetCellPositions() 정보를 통해 생성되며,
 * 자신의 부모 PlaceableUI의 움직임을 관리함.
 */
public class PlaceableCellUI : MonoBehaviour ,IBeginDragHandler,IDragHandler,IEndDragHandler, IPointerClickHandler
{
    private PlaceableUI hostPlaceableUI;



    public void SetHost(PlaceableUI placeableUI)
    {
        hostPlaceableUI = placeableUI;
    }
    
    /*
     * PlaceableUI는 자체적으로 Image 등이 없고, 그 자식인 CellUI에서 드래그를 관리한다.
     */
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        hostPlaceableUI.BeginDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !hostPlaceableUI.isDragTarget) return;
        Vector3 newPos = Camera.main.ScreenToWorldPoint(eventData.position);
        hostPlaceableUI.transform.position = new Vector3(newPos.x, newPos.y, 0);
        hostPlaceableUI.Drag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        hostPlaceableUI.EndDrag();
    }
    
    /*
     * 드래그 중 우클릭 시 host를 회전시킴.
     */
    public void OnPointerClick(PointerEventData eventData)
    {
        if (hostPlaceableUI.CurrentState == PlaceableState.DRAGGING && eventData.button == PointerEventData.InputButton.Right)
        {
            hostPlaceableUI.Rotate();
            // GridUI의 그래픽을 새로고침하기 위해 Drag()도 호출.
            hostPlaceableUI.Drag();
        }
    }
}
