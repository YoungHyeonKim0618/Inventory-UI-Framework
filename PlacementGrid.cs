
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/*
 * 내부에 장착된 IPlaceable들의 정보를 가지는 클래스.
 * 실제적 정보는 모두 이 클래스가 가지며, GridUI와는 SyncWithGrid()나 Place()를 통해 정보를 교환한다.
 */
public abstract class PlacementGrid : MonoBehaviour
{
    // Independent placeables equipped in.
    private List<IPlaceable> placeables;
    public List<IPlaceable> Placeables => placeables;

    // Dictionary to save placeable and coordinate.
    private Dictionary<V2Int, IPlaceable> placeablesDictionary;
    public Dictionary<V2Int, IPlaceable> PlaceablesDictionary => placeablesDictionary;
    
    // Array to find placeable by coordinate
    private IPlaceable[,] placeablesByCoord;

    public PlacementSlotState[,] slotStates;

    #region +Serializable Fields

    
    
    [SerializeField] public int sizeX, sizeY;
    

    #endregion

    public void Init()
    {
        placeables = new List<IPlaceable>();
        slotStates = new PlacementSlotState[sizeY, sizeX];
        placeablesDictionary = new Dictionary<V2Int, IPlaceable>();
        placeablesByCoord = new IPlaceable[sizeY, sizeX];
        
        InitSlotUnlockInfo();
    }

    /*
     * 필요하다면 외부로부터 슬롯의 해금 정보를 받아와서 동기화한다.
     * 기본적으로는 모든 슬롯이 해금되어있음.
     */
    protected virtual void InitSlotUnlockInfo()
    {
        for (int i = 0; i < sizeY; i++)
        {
            for (int j = 0; j < sizeX; j++)
            {
                slotStates[i, j] = PlacementSlotState.EMPTY;
            }
        }
    }

    #region +Unity Events

    /*
     * 내부 IPlaceable들에 변화가 생기면 호출되는 이벤트.
     * 예를 들어, IPlaceable이 상하좌우의 다른 IPlaceable 정보를 사용한다면 OnChanged()시 새로고침해줘야 함.
     */
    [HideInInspector]
    public UnityEvent<IPlaceable> OnPlace;
    [HideInInspector]
    public UnityEvent<IPlaceable> OnRemove;
    [HideInInspector]
    public UnityEvent OnChanged;

    #endregion


    #region +Slot Expansion

    /*
     * 현재 슬롯을 해금할 수 있는지 여부 반호나
     */
    public abstract bool IsExpandable();

    /*
     * 해당 좌표의 슬롯이 해금될 수 있는 상태인지를 반환.
     * 기본적으로는 'LOCKED' 상태이고, 상하좌우로 'EMPTY' 혹은 'FULL' (즉, 해금된) 슬롯이 하나라도 있어야 true 반환.
     */
    protected virtual bool IsExpandableSlot(int x, int y)
    {
        PlacementSlotState state = slotStates[y, x];
        if (state == PlacementSlotState.LOCKED)
        {
            V2Int[] offsets = { new V2Int(1, 0), new V2Int(0, 1), new V2Int(-1, 0), new V2Int(0, -1) };
            foreach (var offset in offsets)
            {
                V2Int coord = new V2Int(x + offset.x, y + offset.y);
                
                // check index out of range
                if (coord.x >= 0 && coord.x < sizeX && coord.y >= 0 && coord.y < sizeY)
                {
                    if (slotStates[coord.y, coord.x] == PlacementSlotState.FULL ||
                        slotStates[coord.y, coord.x] == PlacementSlotState.EMPTY)
                        return true;
                }
            }
        }
        return false;
    }

    /*
     * 좌표를 통해 슬롯을 해금해 확장한다.
     * 만약 외부와 슬롯의 해금 정보를 동기화해야 한다면, override 후 구현해야 함.
     * 
     * [Ex] slotStates[y, x] = PlacementSlotState.EMPTY;
     * [Ex] GameManager.getInstance.AddSlotUnlocked(x,y);
     */
    public virtual void ExpandSlot(int x, int y)
    {
        if (x >= 0 && x < sizeX && y >= 0 && y < sizeY)
        {
            slotStates[y, x] = PlacementSlotState.EMPTY;
        }
    }

    /*
     * 해금 가능한 상태라면, 해금 가능한 슬롯들의 상태를 'LOCKED'에서 'EXPANDABLE'로 바꾼다.
     * 기본적으로는 유저에게 어떤 슬롯을 해금할 수 있는지 시각적으로 보여주기 위해 호출됨.
     */
    public void RefreshExpandableSlotStates()
    {
        if (IsExpandable())
        {
            for (int i = 0; i < sizeY; i++)
            {
                for (int j = 0; j < sizeX; j++)
                {
                    if (slotStates[i, j] == PlacementSlotState.LOCKED && IsExpandableSlot(j,i))
                        slotStates[i, j] = PlacementSlotState.EXPANDABLE;
                }
            }
        }
        else
        {
            for (int i = 0; i < sizeY; i++)
            {
                for (int j = 0; j < sizeX; j++)
                {
                    if (slotStates[i, j] == PlacementSlotState.EXPANDABLE)
                        slotStates[i, j] = PlacementSlotState.LOCKED;
                }
            }
        }
    }

    #endregion
    
    public IPlaceable GetPlaceable(int x, int y)
    {
        IPlaceable placeable;
        bool success = placeablesDictionary.TryGetValue(new V2Int(x,y), out placeable);

        return success ? placeable : null;
    }
    public IPlaceable GetPlaceable(V2Int coord)
    {
        return GetPlaceable(coord.x, coord.y);
    }
    
    public void AddPlaceable(IPlaceable placeable, int x, int y){
        if(!placeables.Contains(placeable))
        {
            placeables.Add(placeable);
            placeablesDictionary.TryAdd(new V2Int(x, y), placeable);

            foreach (var v2Int in placeable.GetCellPositions())
            {
                // if there are placeables overlapping
                if (placeablesByCoord[y + v2Int.y, x + v2Int.x] != null)
                {
                    PlaceManager.instance.OnPlaceableReturn.Invoke(placeablesByCoord[y + v2Int.y, x + v2Int.x]);
                    RemovePlaceable(placeablesByCoord[y + v2Int.y, x + v2Int.x]);
                }
                
                placeablesByCoord[y + v2Int.y, x + v2Int.x] = placeable;
            }
        }
        OnChanged.Invoke();
    }

    public void AddPlaceable(IPlaceable placeable, V2Int v2Int)
    {
        AddPlaceable(placeable,v2Int.x,v2Int.y);
    }

    public void RemovePlaceable(IPlaceable placeable)
    {
        V2Int coord = GetPlaceableCoordinate(placeable);
        placeables.Remove(placeable);
        placeablesDictionary.Remove(coord);

        if(coord.x != Int32.MinValue)
        {
            foreach (var v2Int in placeable.GetCellPositions())
            {
                placeablesByCoord[coord.y + v2Int.y, coord.x + v2Int.x] = null;
            }
        }
        OnChanged.Invoke();
    }

    public void RemovePlaceable(int x, int y)
    {
        IPlaceable toBeRemoved;
        bool success = placeablesDictionary.TryGetValue(new V2Int(x, y), out toBeRemoved);

        if (success)
        {
            RemovePlaceable(toBeRemoved);
        }
    }

    private V2Int GetPlaceableCoordinate(IPlaceable placeable)
    {
        if (placeables.Contains(placeable))
        {
            foreach (var pair in placeablesDictionary)
            {
                if (pair.Value == placeable)
                    return pair.Key;
            }
        }
        return new V2Int(Int32.MinValue, Int32.MinValue);
    }

    public int GetRarityByCoord(int x, int y)
    {
        int ret = -1;

        if (placeablesByCoord[y, x] != null)
            return placeablesByCoord[y, x].GetRarity();

        return ret;
    }

    #region +Placeable Conditions

    public bool IsPlaceable(int x, int y)
    {
        // index out of range
        if (x< 0 || x>= sizeX || y < 0 || y >= sizeY)
            return false;
        // locked or disabled
        PlacementSlotState state = slotStates[y, x];
        if (state == PlacementSlotState.LOCKED || state == PlacementSlotState.DISABLED || state == PlacementSlotState.EXPANDABLE) return false;
        else return true;
    }

    #endregion
}
