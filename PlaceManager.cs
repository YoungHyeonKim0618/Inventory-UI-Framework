
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/*
 * PlaceableUI와 PlacementGrid가 존재하는 씬에 있어야 하는 매니저 클래스.
 * 싱글톤 패턴을 이용해 Grid나 Placeable에서 접근 가능하다.
 */
public abstract class PlaceManager : MonoBehaviour
{
    public static PlaceManager instance;
    public const float CELL_SIZE = 50;

    // 씬에 존재하는 모든 gridUI
    [SerializeField] protected List<PlacementGridUI> gridUis;

    [SerializeField] private Transform placeableUiRoot;
    [SerializeField] private Transform draggingPlaceableUiRoot;
    
    [SerializeField] private PlaceableUI placeableUiPrefab;
    [SerializeField] private PlaceableCellUI placeableCellUiPrefab;
    [SerializeField] private PlacementSlotUI placementSlotUiPrefab;


    protected List<PlaceableUI> allPlaceableUIs = new List<PlaceableUI>();
        
    private void Awake()
    {
        if (!instance) instance = this;
        else Destroy(this.gameObject);
    }

    private void Start()
    {
        Init();
    }

    protected virtual void Init()
    {
        foreach (var gridUI in gridUis)
        {
            gridUI.InitGrid(placementSlotUiPrefab, this);
        }
        InitOutsidePlaces();
        
        BeginDrag.AddListener(SetAsDragging);
        EndDrag.AddListener(SetAsNotDragging);
    }

    #region +PlacementGridUI Event

    /*
     * PlaceableUI에서 모든 GridUI로 호출되는 이벤트들.
     * GridUI는 호출한 PlaceableUI가 자신 위에 있는지 확인하고 동작함.
     */
    [HideInInspector]
    public UnityEvent<PlaceableUI> BeginDrag = new UnityEvent<PlaceableUI>();
    [HideInInspector]
    public UnityEvent<PlaceableUI> Drag = new UnityEvent<PlaceableUI>();
    [HideInInspector]
    public UnityEvent<PlaceableUI> EndDrag = new UnityEvent<PlaceableUI>();

    private void SetAsDragging(PlaceableUI pui)
    {
        pui.transform.SetParent(draggingPlaceableUiRoot);
    }
    private void SetAsNotDragging(PlaceableUI pui)
    {
        pui.transform.SetParent(placeableUiRoot);
    }


    #endregion

    #region +Creating PlaceableUI

    public void CreatePlaceableUI(IPlaceable placeable)
    {
        var pui = Instantiate(placeableUiPrefab,placeableUiRoot);
        pui.InitPlaceableUI(placeable,placeableCellUiPrefab);
        ReturnPlaceableUiToOutside(pui,false);
        OnPlaceableReturn.AddListener(pui.CheckPlaceableReturn);
        
        allPlaceableUIs.Add(pui);
    }

    public void DestroyPlaceableUI(PlaceableUI pui)
    {
        if (allPlaceableUIs.Contains(pui)) allPlaceableUIs.Remove(pui);
        Destroy(pui.gameObject);
    }

    #endregion

    #region +Callbacks
    
    [HideInInspector]
    public UnityEvent<IPlaceable> OnPlaceableReturn = new UnityEvent<IPlaceable>();


    #endregion

    #region +Grid Slot Expansion

    public abstract bool TryExpand(PlacementGridUI gridUI, V2Int coord);

    #endregion
    #region +Outside Places

    // GridUI에 PlaceableUI의 장착이 실패했을 때 돌아가는 위치들을 나타내는 Transform 리스트.
    [SerializeField]
    private List<Transform> outsidePlaces = new List<Transform>();
    [SerializeField]
    private PlaceableUI[] placeableUisOutside;
    private bool isThereEmptyOutsidePlace => placeableUisOutside.Any(x => x == null);

    private int GetSmallestEmptyOutsidePlaceIndex()
    {
        for (int i = 0; i < outsidePlaces.Count; i++)
        {
            if (placeableUisOutside[i] == null) return i;
        }

        return -1;
    }

    private void InitOutsidePlaces()
    {
        placeableUisOutside = new PlaceableUI[outsidePlaces.Count];
        BeginDrag.AddListener(RemovePlaceableUiFromOutsidePlace);
    }

    /*
     * Returns placeableUI to the outside of the placementGridUI.
     * GridUI 바깥으로 PlaceableUI를 보낸다.
     * wantsNear : 만약 true라면, 가장 가까운 위치로 반환함 (만약 그 위치에 다른 PlaceableUI가 있더라도)
     */
    public void ReturnPlaceableUiToOutside(PlaceableUI pui, bool wantsNear)
    {
        if (wantsNear)
        {
            Transform target = GetNearestEmptyOutsidePlace(pui.transform.position);
            int targetIndex = outsidePlaces.FindIndex(x => x == target);
            if (targetIndex != -1 && placeableUisOutside[targetIndex] != null)
            {
                // if the place is full, replace it
                PlaceableUI prevPui = placeableUisOutside[targetIndex];
                placeableUisOutside[targetIndex] = pui;
                pui.transform.position = outsidePlaces[targetIndex].transform.position;
                    
                ReturnPlaceableUiToOutside(prevPui,false);
            }
        }
        else
        {
            if (isThereEmptyOutsidePlace)
            {
                // if there is any empty space
                int index = GetSmallestEmptyOutsidePlaceIndex();

                placeableUisOutside[index] = pui;
                //TODO : check unexpected working while returning
                StartCoroutine(WaitAndReturnPlaceablePosition(pui, outsidePlaces[index].transform.position));
            }
            else 
            {
                //TODO : what if there is no empty place outside?
                print("No empty outside place");
            }
        }
    }

    private IEnumerator WaitAndReturnPlaceablePosition(PlaceableUI pui, Vector2 worldPos)
    {
        pui.isDragTarget = false;
        Tween tween = pui.transform.DOMove(worldPos, 0.25f).SetUpdate(true);
        yield return tween.WaitForCompletion();
        pui.isDragTarget = true;
    }


    /// <returns>Transform of the nearest empty outside place.</returns>
    private Transform GetNearestEmptyOutsidePlace(Vector2 pos)
    {
        float minDistance = float.MaxValue;
        Transform ret = null;
        
        foreach (var pt in outsidePlaces)
        {
            float distance = Vector2.Distance(pos, pt.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                ret = pt;
            }
        }
        return ret;
    }

    private void RemovePlaceableUiFromOutsidePlace(PlaceableUI pui)
    {
        for (int i = 0; i < outsidePlaces.Count; i++)
        {
            if (placeableUisOutside[i] != null && placeableUisOutside[i] == pui)
                placeableUisOutside[i] = null;
        }
    }
    #endregion

    #region +PlaceableUI Descriptions

    /*
     * Description Panel을 제어하기 위한 변수들.
     * disPlayPanelTime : PlaceableUI 위에 얼마나 오래 마우스를 올려야 표시되는지 설정
     * panelAnchor : 마우스 기준으로 패널이 어느 쪽으로 보여질지 설정
     * panelOffset : 패널이 마우스와 얼마나 멀리 위치할지 설정
     */
    [Header("Description Panel")]
    [SerializeField]
    public float displayPanelTime;
    
    [SerializeField, Range(-1,1)]
    public int panelAnchorX;
    [SerializeField, Range(-1,1)]
    public int panelAnchorY;
    [SerializeField, Range(-100, 100)] public float panelOffset;
    [SerializeField] private PlaceableDescriptionPanel descriptionPanel;

    public void DisplayPlaceable(IPlaceable placeable, Vector2 mousePos)
    {
        descriptionPanel.DisplayPlaceable(placeable,mousePos);
    }

    public void MoveDisplayPlaceable( Vector2 mousePos)
    {
        descriptionPanel.SetPanelPosition(mousePos);
    }
    public void CloseDescriptionPanel()
    {
        descriptionPanel.Close();
    }

    public abstract Color GetColorByRarity(int rarity);

    /*
     * DescriptionPanel이 위치할 수 있는 최소값/최대값을 반환한다.
     * Canvas 설정이나 의도에 따라 다르게 동작해야 하므로 사용 시 Override 후 구현해줘야 함.
     */
    public abstract Vector2 GetMinRectSupport();
    public abstract Vector2 GetMaxRectSupport();

    #endregion

    #region MyRegion
    
    /*
     * 월드 포지션, 마우스(스크린) 포지션으로부터 anchoredPosition을 반환하는 메서드.
     * 화면 전체 크기 캔버스 상의, anchor가 (0.5, 0.5)인 오브젝트라고 가정한다.
     * Canvas 설정에 따라 다르므로 Override 후 구현해줘야 함.
     */
    public abstract Vector2 WorldToRectPosition(Vector2 worldPos);
    public abstract Vector2 MouseToRectPosition(Vector2 mousePos);

    #endregion
}
