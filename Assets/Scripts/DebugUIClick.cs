using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DebugUIClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    
    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            Debug.Log($"[DebugUIClick] Attached to button: {button.name}");
            Debug.Log($"  Interactable: {button.interactable}");
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    void OnButtonClick()
    {
        Debug.Log($"✅ Button.onClick fired on {gameObject.name}!");
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"🖱️ OnPointerClick on {gameObject.name}");
        Debug.Log($"   Button: {eventData.button}");
        Debug.Log($"   Position: {eventData.position}");
        Debug.Log($"   PointerEnter: {eventData.pointerEnter?.name}");
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"👆 OnPointerEnter on {gameObject.name}");
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"👋 OnPointerExit from {gameObject.name}");
    }
}
