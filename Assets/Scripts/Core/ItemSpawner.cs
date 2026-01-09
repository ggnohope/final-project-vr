using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private ItemBarController itemBar;
    
    [Header("Item Prefabs")]
    [SerializeField] private GameObject compassPrefab;
    [SerializeField] private GameObject drawingBoardPrefab;
    [SerializeField] private GameObject penPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 0.3f);
    
    private Dictionary<ItemType, GameObject> activeItems = new Dictionary<ItemType, GameObject>();
    private GameObject placedBoard;
    
    public static ItemSpawner Instance { get; private set; }
    
    public System.Action<GameObject, ItemType> OnItemSpawned;
    public System.Action<ItemType> OnItemReturned;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (spawnPoint == null)
        {
            spawnPoint = Camera.main.transform;
        }
    }
    
    private void OnEnable()
    {
        if (itemBar != null)
        {
            itemBar.OnItemSelected += SpawnItem;
        }
    }
    
    private void OnDisable()
    {
        if (itemBar != null)
        {
            itemBar.OnItemSelected -= SpawnItem;
        }
    }
    
    public void SpawnItem(ItemType itemType)
    {
        if (activeItems.ContainsKey(itemType) && activeItems[itemType] != null)
        {
            ReturnItem(itemType);
            return;
        }
        
        GameObject prefab = GetPrefabForType(itemType);
        if (prefab == null)
        {
            Debug.LogError($"ItemSpawner: No prefab for {itemType}");
            return;
        }
        
        Vector3 spawnPosition = spawnPoint.position + spawnPoint.TransformDirection(spawnOffset);
        Quaternion spawnRotation = spawnPoint.rotation;
        
        GameObject newItem = Instantiate(prefab, spawnPosition, spawnRotation);
        activeItems[itemType] = newItem;
        
        if (itemType == ItemType.DrawingBoard)
        {
            BoardPlacementController placement = newItem.GetComponent<BoardPlacementController>();
            if (placement != null)
            {
                placement.OnBoardPlaced += OnBoardPlaced;
                placement.OnBoardCancelled += () => OnBoardCancelled(itemType);
                placement.EnterPlacementMode();
            }
            else
            {
                Debug.LogError("ItemSpawner: DrawingBoard missing BoardPlacementController");
                Destroy(newItem);
                activeItems.Remove(itemType);
                return;
            }
        }
        
        OnItemSpawned?.Invoke(newItem, itemType);
    }
    
    private void OnBoardPlaced(GameObject board)
    {
        placedBoard = board;
    }
    
    private void OnBoardCancelled(ItemType itemType)
    {
        ReturnItem(itemType);
    }
    
    private GameObject GetPrefabForType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Compass:
                return compassPrefab;
            case ItemType.DrawingBoard:
                return drawingBoardPrefab;
            case ItemType.Pen:
                return penPrefab;
            default:
                return null;
        }
    }
    
    private void OnItemGrabbed()
    {
    }
    
    public void ReturnItem(ItemType itemType)
    {
        if (activeItems.ContainsKey(itemType) && activeItems[itemType] != null)
        {
            Destroy(activeItems[itemType]);
            activeItems.Remove(itemType);
            
            if (itemBar != null)
            {
                itemBar.ReturnItem(itemType);
            }
            
            OnItemReturned?.Invoke(itemType);
        }
    }
    
    public void ReturnCurrentItem()
    {
        foreach (var kvp in activeItems)
        {
            if (kvp.Value != null && kvp.Value != placedBoard)
            {
                ReturnItem(kvp.Key);
                return;
            }
        }
    }
    
    public GameObject GetItem(ItemType itemType)
    {
        if (activeItems.ContainsKey(itemType))
        {
            return activeItems[itemType];
        }
        return null;
    }
    
    public bool HasActiveItem(ItemType itemType)
    {
        return activeItems.ContainsKey(itemType) && activeItems[itemType] != null;
    }
}
