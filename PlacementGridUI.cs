

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/*
 * PlacementGrid를 시각적으로 보여주고, 드래그/드롭으로 인한 작동을 Grid와 동기화하는 클래스.
 */
public abstract class PlacementGridUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PlacementGrid myGrid;
    [SerializeField] public PlacementGrid GetGrid => myGrid;
    
    private float width => PlaceManager.CELL_SIZE * myGrid.sizeX;
    private float height => PlaceManager.CELL_SIZE * myGrid.sizeY;
    private Vector2 getRectPos => PlaceManager.instance.WorldToRectPosition(transform.position);
    private Vector2 minSupport => getRectPos - new Vector2(0.5f * width, 0.5f * height);
    private Vector2 maxSupport => getRectPos + new Vector2(0.5f * width, 0.5f * height);

    private PlacementSlotUI[,] slotUIs;
    private List<PlaceableUI> placedUIs = new List<PlaceableUI>();

    
    public void InitGrid(PlacementSlotUI slotUIPrefab, PlaceManager manager)
    {
        // assign unityEvent observers
        manager.BeginDrag.AddListener(BeginPlaceableUIDrag);
        manager.Drag.AddListener(PlaceableUIDrag);
        manager.EndDrag.AddListener(EndPlaceableUIDrag);
        
        // init my grid
        myGrid.Init();

        slotUIs = new PlacementSlotUI[myGrid.sizeY, myGrid.sizeX];
        
        CreateSlotUIs(slotUIPrefab);
        SyncWithGrid(false);
        
        
    }

    
    public void SetSlotExpansionChangedEvent(UnityEvent _event)
    {
        _event.AddListener(RefreshSlotUis);
    }

    private void CreateSlotUIs(PlacementSlotUI slotUIPrefab)
    {
        Vector2 minPivot = new Vector2(-0.5f * (myGrid.sizeX - 1) * PlaceManager.CELL_SIZE, -0.5f * (myGrid.sizeY - 1) * PlaceManager.CELL_SIZE);
        for (int i = 0; i < myGrid.sizeY; i++)
        {
            for (int j = 0; j < myGrid.sizeX; j++)
            {
                var slotUI = Instantiate(slotUIPrefab,transform);
                slotUIs[i, j] = slotUI;
                var slotRect = slotUI.GetComponent<RectTransform>();
                slotRect.anchoredPosition =
                    minPivot + new Vector2(j * PlaceManager.CELL_SIZE, i * PlaceManager.CELL_SIZE);
                slotRect.sizeDelta = new Vector2(PlaceManager.CELL_SIZE, PlaceManager.CELL_SIZE);
                
            }
        }
    }

    #region +Resetting and Refreshing GridUi

    /*
     * 자신의 Grid의 정보로 새로고침한다.
     */
    public void SyncWithGrid()
    {
        // sync slot info
        RefreshSlotUis();
        foreach (var pair in myGrid.PlaceablesDictionary)
        {
            foreach (var v2Int in pair.Value.GetCellPositions())
            {
                V2Int coord = pair.Key + v2Int;
                slotUIs[coord.y,coord.x].SetSlotState(PlacementSlotState.FULL,pair.Value.GetRarity());
            }
        }
    }
    
    private void RefreshSlotUis()
    {
        myGrid.RefreshExpandableSlotStates();
        for (int i = 0; i < myGrid.sizeY; i++)
        {
            for (int j = 0; j < myGrid.sizeX; j++)
            {
                slotUIs[i,j].SetSlotState(myGrid.slotStates[i,j],myGrid.GetRarityByCoord(j,i));
            }
        }
    }
    

    #endregion


    public void Place(PlaceableUI pui)
    {
        bool isPlaceable = true;
        foreach (var v2Int in pui.GetPlaceable.GetCellPositions())
        {
            Vector2 rect = PlaceManager.instance.WorldToRectPosition(pui.transform.position) + new Vector2(v2Int.x * PlaceManager.CELL_SIZE, v2Int.y * PlaceManager.CELL_SIZE);
            if (IsRectPosInside(rect))
            {
                V2Int coord = GetCoordFromRectPos(rect);
                if(myGrid.IsPlaceable(coord.x,coord.y)) continue;
            }
            isPlaceable = false;
            break;
        }
        
        if (isPlaceable)
        {
            V2Int targetCoord = GetCoordFromWorldPos(pui.transform.position);
            pui.transform.position = GetWorldPosFromCoord(targetCoord);
            myGrid.AddPlaceable(pui.GetPlaceable,targetCoord.x,targetCoord.y);
            pui.SetStateAsPlaced();
        }
        else PlaceManager.instance.ReturnPlaceableUiToOutside(pui,false);
        
        SyncWithGrid();
    }

    private void ResetAllSlotsColor()
    {
        foreach (var sui in slotUIs)
        {
            sui.ResetColor();
        }
    }
    #region +Drag of the PlaceableUI

    private void BeginPlaceableUIDrag(PlaceableUI pui)
    {
        if (IsPlaceableUiInsideEntirely(pui))
        {
            myGrid.RemovePlaceable(pui.GetPlaceable);
        }
        SyncWithGrid(false);
    }

    private void PlaceableUIDrag(PlaceableUI pui)
    {
        if (IsPlaceableUiInside(pui))
        {
            ResetAllSlotsColor();
            foreach (var v2Int in pui.GetPlaceable.GetCellPositions())
            {
                Vector2 rectPos = pui.myRect.anchoredPosition +
                                  new Vector2(v2Int.x * PlaceManager.CELL_SIZE, v2Int.y * PlaceManager.CELL_SIZE);
                // if cell is outside the gridUi, do nothing
                if(IsRectPosInside(rectPos))
                {
                    V2Int coord = GetCoordFromRectPos(rectPos);
                    // if out of index
                    if (coord.x< 0 || coord.x>= myGrid.sizeX || coord.y < 0 || coord.y >= myGrid.sizeY) continue;

                    PlacementSlotState state = myGrid.slotStates[coord.y , coord.x];
                    if (state == PlacementSlotState.LOCKED || state == PlacementSlotState.DISABLED)
                        slotUIs[coord.y, coord.x].SetColor(Color.red);
                    else slotUIs[coord.y, coord.x].SetColor(Color.green);
                }
            }
        }
        else ResetAllSlotsColor();
    }

    private void EndPlaceableUIDrag(PlaceableUI pui)
    {
        if(IsPlaceableUiInside(pui))
        {
            if (IsPlaceableUiInsideEntirely(pui))
            {
                Place(pui);
            }
            else
            {
                PlaceManager.instance.ReturnPlaceableUiToOutside(pui, false);
            }
        }
        ResetAllSlotsColor();
        SyncWithGrid();
    }

    private bool IsPlaceableUiInside(PlaceableUI pui)
    {
        foreach (var v2Int in pui.GetPlaceable.GetCellPositions())
        {
            Vector2 rect = PlaceManager.instance.WorldToRectPosition(pui.transform.position) + new Vector2(v2Int.x * PlaceManager.CELL_SIZE, v2Int.y * PlaceManager.CELL_SIZE);
            if (IsRectPosInside(rect)) return true;
        }
        return false;
    }

    private bool IsPlaceableUiInsideEntirely(PlaceableUI pui)
    {
        foreach (var v2Int in pui.GetPlaceable.GetCellPositions())
        {
            Vector2 rect = PlaceManager.instance.WorldToRectPosition(pui.transform.position) + new Vector2(v2Int.x * PlaceManager.CELL_SIZE, v2Int.y * PlaceManager.CELL_SIZE);
            if (!IsRectPosInside(rect)) return false;
        }
        return true;
    }
    private bool IsRectPosInside(Vector2 rect)
    {
        return rect.x >= minSupport.x && rect.y >= minSupport.y && rect.x <= maxSupport.x &&
               rect.y <= maxSupport.y;
    }
    

    // worldPos로부터 '가장 가까운'좌표 반환
    private V2Int GetCoordFromWorldPos(Vector2 pos)
    {
        pos = PlaceManager.instance.WorldToRectPosition(pos);
        return GetCoordFromRectPos(pos);
    }
    private V2Int GetCoordFromWorldPos(Vector3 pos)
    {
        pos = PlaceManager.instance.WorldToRectPosition(pos);
        return GetCoordFromRectPos(pos);
    }
    private V2Int GetCoordFromScreenPos(Vector2 pos)
    {
        pos = PlaceManager.instance.MouseToRectPosition(pos);
        return GetCoordFromRectPos(pos);
    }
    private V2Int GetCoordFromRectPos(Vector2 pos)
    {
        V2Int ret = new V2Int(0, 0);
        
        // 범위를 실제 Grid 크기 안으로 재설정
        pos = new Vector2(Mathf.Clamp(pos.x, minSupport.x, maxSupport.x),
            Mathf.Clamp(pos.y, minSupport.y, maxSupport.y));

        float pX = Mathf.Clamp((pos.x - minSupport.x) / width,0,1 - float.Epsilon) * myGrid.sizeX;
        float pY = Mathf.Clamp((pos.y - minSupport.y) / height,0,1 - float.Epsilon) * myGrid.sizeY;
        

        ret.x = Mathf.Clamp(Mathf.FloorToInt(pX),0,myGrid.sizeX - 1);
        ret.y = Mathf.Clamp(Mathf.FloorToInt(pY),0,myGrid.sizeY);
        
        return ret;
    }


    private Vector2 GetWorldPosFromCoord(int x, int y)
    {
        return slotUIs[y, x].transform.position;
    }private Vector2 GetWorldPosFromCoord(V2Int v2Int)
    {
        return GetWorldPosFromCoord(v2Int.x, v2Int.y);
    }


    #endregion

    
    #region +Debug
    
    [SerializeField] private Canvas puiCanvas;
    /*
     * 게임 시작 전에 크기와 위치를 미리 보기 위한 기즈모를 그림.
     */
    private void OnDrawGizmos() 
    {
        if(puiCanvas != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position,
                new Vector3(myGrid.sizeX * PlaceManager.CELL_SIZE * puiCanvas.transform.localScale.x,
                    myGrid.sizeY * PlaceManager.CELL_SIZE * puiCanvas.transform.localScale.x, 1));
        }
    }

    #endregion

    /*
     * 해금 가능한 상태일 때, 클릭함으로써 해당 좌표의 Slot을 해금시킴.
     */
    public void OnPointerClick(PointerEventData eventData)
    {
        V2Int coord = GetCoordFromScreenPos(eventData.position);
        PlacementSlotState state = myGrid.slotStates[coord.y, coord.x];
        if ( state == PlacementSlotState.EXPANDABLE || state == PlacementSlotState.LOCKED)
        {
            PlaceManager.instance.TryExpand(this, coord);
        }
    }
}
