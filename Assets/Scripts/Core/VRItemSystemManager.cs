using UnityEngine;
using UnityEngine.InputSystem;

public class VRItemSystemManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionProperty returnItemAction;
    
    [Header("References")]
    [SerializeField] private ItemBarController itemBar;
    [SerializeField] private ItemSpawner itemSpawner;
    
    public static VRItemSystemManager Instance { get; private set; }
    
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
        
        if (itemBar == null) itemBar = FindFirstObjectByType<ItemBarController>();
        if (itemSpawner == null) itemSpawner = FindFirstObjectByType<ItemSpawner>();
    }
    
    private void OnEnable()
    {
        if (returnItemAction.action != null)
        {
            returnItemAction.action.Enable();
            returnItemAction.action.performed += OnReturnItem;
        }
        
        if (itemSpawner != null)
        {
            itemSpawner.OnItemReturned += OnItemReturnedToBar;
        }
    }
    
    private void OnDisable()
    {
        if (returnItemAction.action != null)
        {
            returnItemAction.action.performed -= OnReturnItem;
            returnItemAction.action.Disable();
        }
        
        if (itemSpawner != null)
        {
            itemSpawner.OnItemReturned -= OnItemReturnedToBar;
        }
    }
    
    private void OnReturnItem(InputAction.CallbackContext context)
    {
        if (itemSpawner != null)
        {
            itemSpawner.ReturnCurrentItem();
        }
    }
    
    private void OnItemReturnedToBar(ItemType itemType)
    {
        if (itemBar != null)
        {
            itemBar.ShowBar();
        }
    }
}
