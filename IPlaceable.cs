
using System.Collections.Generic;
using UnityEngine;

/*
 * 실제 아이템 (혹은 유물 등) 정보를 가지는 클래스가 상속해야 하는 인터페이스.
 * Grid 내 정리를 위한 정보를 반환하는 메서드와, DescriptionPanel을 위한 이름과 Sprite, 설명을 반환하는 메서드를 가짐.
 */
public interface IPlaceable
{
    public List<V2Int> GetCellPositions();
    public int GetRarity();
    public Sprite GetSprite();
    
    
    public int GetRotation();
    // returns whether this is flipped with y-axis.
    public bool IsFlipped();
    
    public void Rotate();

    public string GetName();
    public string GetDescription();

}

public struct V2Int
{
    public int x;
    public int y;

    public V2Int(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
    
    public static V2Int operator +(V2Int origin,V2Int target)
    {
        return new V2Int(origin.x + target.x, origin.y + target.y);
    }
    public static V2Int operator -(V2Int origin,V2Int target)
    {
        return new V2Int(origin.x - target.x, origin.y - target.y);
    }

    public static implicit operator V2Int(Vector2 vector2)
    {
        return new V2Int(Mathf.RoundToInt(vector2.x), Mathf.RoundToInt(vector2.y));
    }

    public override string ToString()
    {
        return $"{x},{y}";
    }
}