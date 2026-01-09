using UnityEngine;

public enum ItemType
{
    Compass,
    DrawingBoard,
    Pen
}

[System.Serializable]
public class ItemData
{
    public ItemType itemType;
    public string itemName;
    public GameObject prefab;
    public Sprite icon;
}
