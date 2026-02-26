using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;
using VRDrawing.Mode;

public class ItemSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private ItemBarController itemBar;
    
    [Header("Item Prefabs")]
    [SerializeField] private GameObject compassPrefab;
    [SerializeField] private GameObject drawingBoardActivatorPrefab;
    [SerializeField] private GameObject cameraPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 0.3f);
    
    private Dictionary<ItemType, GameObject> activeItems = new Dictionary<ItemType, GameObject>();
    
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
        if (itemType == ItemType.DrawingBoard)
        {
            if (DrawingModeManager.Instance != null && DrawingModeManager.Instance.IsInDrawingMode)
            {
                ReturnItem(itemType);
                return;
            }
        }
        else
        {
            if (activeItems.ContainsKey(itemType) && activeItems[itemType] != null)
            {
                ReturnItem(itemType);
                return;
            }
        }
        
        GameObject prefab = GetPrefabForType(itemType);
        if (prefab == null)
        {
            return;
        }
        
        Vector3 spawnPosition = spawnPoint.position + spawnPoint.TransformDirection(spawnOffset);
        Quaternion spawnRotation = spawnPoint.rotation;
        
        GameObject newItem = Instantiate(prefab, spawnPosition, spawnRotation);
        activeItems[itemType] = newItem;
        
        if (itemType == ItemType.DrawingBoard)
        {
            DrawingBoardActivator activator = newItem.GetComponent<DrawingBoardActivator>();
            if (activator != null)
            {
                activator.ActivateDrawingMode();
            }
            else
            {
            }
        }
        
        OnItemSpawned?.Invoke(newItem, itemType);
    }
    
    private GameObject GetPrefabForType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Compass:
                return compassPrefab;
            case ItemType.DrawingBoard:
                return drawingBoardActivatorPrefab;
            case ItemType.Camera:
                return cameraPrefab;
            default:
                return null;
        }
    }
    
    public void ReturnItem(ItemType itemType)
    {
        if (itemType == ItemType.DrawingBoard && DrawingModeManager.Instance != null)
        {
            DrawingModeManager.Instance.ExitDrawingMode();
        }
        
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
            if (kvp.Value != null)
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
