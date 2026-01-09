using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using System.Collections.Generic;

public class ItemBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject barPanel;
    [SerializeField] private Transform itemButtonParent;
    [SerializeField] private Button itemButtonPrefab;
    
    [Header("Items")]
    [SerializeField] private List<ItemData> availableItems = new List<ItemData>();
    
    [Header("Positioning")]
    [SerializeField] private float distanceFromPlayer = 1.5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.3f, 0f);
    
    [Header("Input")]
    [SerializeField] private InputActionProperty toggleBarAction;
    
    [Header("UI Ray Control")]
    [SerializeField] private XRRayInteractor uiRayInteractor;
    [SerializeField] private bool autoFindUIRay = true;
    
    private XRInteractorLineVisual lineVisual;
    
    private bool isBarVisible = false;
    private int selectedIndex = -1;
    private List<Button> itemButtons = new List<Button>();
    private Dictionary<ItemType, bool> spawnedItems = new Dictionary<ItemType, bool>();
    
    public static ItemBarController Instance { get; private set; }
    
    public System.Action<ItemType> OnItemSelected;
    
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
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }
        
        barPanel.SetActive(false);
        CreateItemButtons();
        
        if (autoFindUIRay && uiRayInteractor == null)
        {
            uiRayInteractor = GameObject.Find("UI Ray Interactor")?.GetComponent<XRRayInteractor>();
        }
        
        if (uiRayInteractor != null)
        {
            lineVisual = uiRayInteractor.GetComponent<XRInteractorLineVisual>();
        }
        
        UpdateUIRayVisibility(false);
    }
    
    private void OnEnable()
    {
        if (toggleBarAction.action != null)
        {
            toggleBarAction.action.Enable();
            toggleBarAction.action.performed += OnToggleBar;
        }
    }
    
    private void OnDisable()
    {
        if (toggleBarAction.action != null)
        {
            toggleBarAction.action.performed -= OnToggleBar;
            toggleBarAction.action.Disable();
        }
    }
    
    private void CreateItemButtons()
    {
        foreach (Transform child in itemButtonParent)
        {
            Destroy(child.gameObject);
        }
        itemButtons.Clear();
        spawnedItems.Clear();
        
        for (int i = 0; i < availableItems.Count; i++)
        {
            int index = i;
            ItemData item = availableItems[i];
            
            spawnedItems[item.itemType] = false;
            
            Button button = Instantiate(itemButtonPrefab, itemButtonParent);
            button.GetComponentInChildren<Text>().text = item.itemName;
            
            if (item.icon != null)
            {
                Image icon = button.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = item.icon;
                }
            }
            
            button.onClick.AddListener(() => SelectItem(index));
            itemButtons.Add(button);
        }
        
        UpdateButtonHighlights();
    }
    
    private void OnToggleBar(InputAction.CallbackContext context)
    {
        ToggleBar();
    }
    
    public void ToggleBar()
    {
        isBarVisible = !isBarVisible;
        barPanel.SetActive(isBarVisible);
        UpdateUIRayVisibility(isBarVisible);
        
        if (isBarVisible)
        {
            PositionBarInFrontOfPlayer();
        }
    }
    
    public void ShowBar()
    {
        isBarVisible = true;
        barPanel.SetActive(true);
        UpdateUIRayVisibility(true);
        PositionBarInFrontOfPlayer();
    }
    
    public void HideBar()
    {
        isBarVisible = false;
        barPanel.SetActive(false);
        UpdateUIRayVisibility(false);
    }
    
    private void UpdateUIRayVisibility(bool visible)
    {
        if (uiRayInteractor != null)
        {
            uiRayInteractor.enabled = visible;
            
            if (lineVisual == null)
            {
                lineVisual = uiRayInteractor.GetComponent<XRInteractorLineVisual>();
            }
            
            if (lineVisual != null)
            {
                lineVisual.enabled = visible;
            }
        }
        else if (autoFindUIRay)
        {
            uiRayInteractor = GameObject.Find("UI Ray Interactor")?.GetComponent<XRRayInteractor>();
            if (uiRayInteractor != null)
            {
                uiRayInteractor.enabled = visible;
                lineVisual = uiRayInteractor.GetComponent<XRInteractorLineVisual>();
                if (lineVisual != null)
                {
                    lineVisual.enabled = visible;
                }
            }
        }
    }
    
    private void PositionBarInFrontOfPlayer()
    {
        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        forward.Normalize();
        
        Vector3 targetPosition = playerCamera.position + forward * distanceFromPlayer + offset;
        transform.position = targetPosition;
        
        transform.rotation = Quaternion.LookRotation(forward);
    }
    
    public void SelectItem(int index)
    {
        if (index < 0 || index >= availableItems.Count) return;
        
        ItemType itemType = availableItems[index].itemType;
        
        selectedIndex = index;
        
        bool wasSpawned = spawnedItems.ContainsKey(itemType) && spawnedItems[itemType];
        
        if (wasSpawned)
        {
            spawnedItems[itemType] = false;
            UpdateButtonHighlights();
        }
        else
        {
            spawnedItems[itemType] = true;
            UpdateButtonHighlights();
        }
        
        OnItemSelected?.Invoke(itemType);
        
        HideBar();
    }
    
    public void ScrollLeft()
    {
        selectedIndex--;
        if (selectedIndex < 0) selectedIndex = availableItems.Count - 1;
        UpdateButtonHighlights();
    }
    
    public void ScrollRight()
    {
        selectedIndex++;
        if (selectedIndex >= availableItems.Count) selectedIndex = 0;
        UpdateButtonHighlights();
    }
    
    private void UpdateButtonHighlights()
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            ItemType itemType = availableItems[i].itemType;
            bool isSpawned = spawnedItems.ContainsKey(itemType) && spawnedItems[itemType];
            
            ColorBlock colors = itemButtons[i].colors;
            
            if (isSpawned)
            {
                // Yellow color for "click to return"
                colors.normalColor = new Color(1f, 0.9f, 0.3f);
                colors.highlightedColor = new Color(1f, 1f, 0.5f);
                colors.pressedColor = new Color(0.9f, 0.8f, 0.2f);
                colors.selectedColor = new Color(1f, 0.9f, 0.3f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
            }
            else
            {
                // Normal gray color
                colors.normalColor = new Color(0.6f, 0.6f, 0.6f);
                colors.highlightedColor = new Color(0.7f, 0.9f, 1f);
                colors.pressedColor = new Color(0.5f, 0.8f, 1f);
                colors.selectedColor = new Color(0.6f, 0.6f, 0.6f);
                colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
            }
            
            itemButtons[i].colors = colors;
        }
    }
    
    public ItemData GetSelectedItem()
    {
        if (selectedIndex >= 0 && selectedIndex < availableItems.Count)
        {
            return availableItems[selectedIndex];
        }
        return null;
    }
    
    public void ReturnItem(ItemType itemType)
    {
        if (spawnedItems.ContainsKey(itemType))
        {
            spawnedItems[itemType] = false;
            UpdateButtonHighlights();
        }
    }
}
