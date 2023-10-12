

/*
 * A class that is draggable, and placeable onto PlacementGridUI.
 */

using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlaceableUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,IPointerMoveHandler
{
    #region +Settings

    private bool isDraggable;
    private bool rotationTweening;

    #endregion

    #region +Instance States

    private PlaceableState currentState;

    public PlaceableState CurrentState => currentState;

    public void SetStateAsPlaced()
    {
        currentState = PlaceableState.PLACED;
    }
    
    public bool isDragTarget;

    #endregion
    
    private IPlaceable myPlaceable;
    public IPlaceable GetPlaceable => myPlaceable;

    [HideInInspector]
    public RectTransform myRect;
    public void InitPlaceableUI(IPlaceable placeable, PlaceableCellUI cellUiPrefab)
    {
        myRect = GetComponent<RectTransform>();
        myRect.sizeDelta = new Vector2(5 * PlaceManager.CELL_SIZE, 5 * PlaceManager.CELL_SIZE);
        myPlaceable = placeable;
        GetComponent<Image>().sprite = myPlaceable.GetSprite();
        AddCellUis(cellUiPrefab);

        isDragTarget = true;
    }

    private void AddCellUis(PlaceableCellUI prefab)
    {
        foreach (var v2Int in myPlaceable.GetCellPositions())
        {
            var cell = Instantiate(prefab,transform);
            cell.GetComponent<RectTransform>().sizeDelta = new Vector2(PlaceManager.CELL_SIZE, PlaceManager.CELL_SIZE);
            cell.transform.localPosition =
                new Vector3(v2Int.x * PlaceManager.CELL_SIZE, v2Int.y * PlaceManager.CELL_SIZE, 0);
            cell.SetHost(this);
        }
    }
    
    public void Rotate()
    {
        //자신의 rotation 변경
        Vector3 euler = transform.rotation.eulerAngles;

        float rotationValue = 90f;
        if (myPlaceable.IsFlipped()) rotationValue *= -1;
        
        transform.DORotate(new Vector3(euler.x, euler.y, euler.z + rotationValue), 0.1f).SetUpdate(true);
        
        //Placeable의 Rotate
        myPlaceable.Rotate();
    }
    
    
    private void SyncWithPlaceable()
    {
        
        int rotation = myPlaceable.GetRotation() % 4;

        // if isFlipped(), set rotation value negative.
        float rotationValue = myPlaceable.IsFlipped() ? -90 * rotation : 90 * rotation;

        transform.rotation = myPlaceable.IsFlipped()
            ? Quaternion.Euler(0, 180, rotationValue)
            : Quaternion.Euler(0, 0, rotationValue);
    }

    /*
     * When placeable is placed, Overlapping prev placeables should be returned.
     * So if the PlaceManager's event OnPlaceableReturn<Placeable> invokes,
     * all the puis should check if returning placeable is mine.
     * If so :  Return(this).
     */
    public void CheckPlaceableReturn(IPlaceable placeable)
    {
        if (myPlaceable == placeable)
        {
            PlaceManager.instance.ReturnPlaceableUiToOutside(this,false);
        }
    }

    #region +CellUI로부터의 마우스 이벤트

    public void BeginDrag()
    {
        currentState = PlaceableState.DRAGGING;
        PlaceManager.instance.BeginDrag.Invoke(this);
        
        OnPointerExit(null);
    }
    public void Drag()
    {
        PlaceManager.instance.Drag.Invoke(this);
        
        //PlaceManager.instance.CloseDescriptionPanel();
    }
    public void EndDrag()
    {
        // if placed, that gridUI will set currentState as PLACED.
        currentState = PlaceableState.LAID;
        
        PlaceManager.instance.EndDrag.Invoke(this);
        SyncWithPlaceable();
        
        OnPointerEnter(null);
    }
    #endregion

    [Header("Description")] [SerializeField]

    private bool isPointerInside;
    private float displayPanelTimer;

    private void Update()
    {
        if (isPointerInside)
        {
            displayPanelTimer += Time.deltaTime;
            if (displayPanelTimer >= PlaceManager.instance.displayPanelTime && displayPanelTimer <= PlaceManager.instance.displayPanelTime + Time.deltaTime)
            {
                PlaceManager.instance.DisplayPlaceable(myPlaceable,Input.mousePosition);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        displayPanelTimer = 0;
        isPointerInside = true;
    }
    public void OnPointerMove(PointerEventData eventData)
    {
        if(displayPanelTimer >= PlaceManager.instance.displayPanelTime)
            PlaceManager.instance.MoveDisplayPlaceable(eventData.position);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        PlaceManager.instance.CloseDescriptionPanel();
        isPointerInside = false;
    }

}

public enum PlaceableState
{
    // Outside Grid
    LAID,
    // Dragging now
    DRAGGING,
    // Inside Grid
    PLACED
}
