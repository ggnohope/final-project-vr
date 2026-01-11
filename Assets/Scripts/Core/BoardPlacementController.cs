using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[System.Obsolete("BoardPlacementController is deprecated. Use VRDrawing.Mode.DrawingModeManager instead.")]
public class BoardPlacementController : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private float maxRaycastDistance = 10f;
    [SerializeField] private float placementOffset = 0.01f;
    [SerializeField] private LayerMask placementLayerMask = ~0;
    
    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Color validColor = new Color(0.3f, 0.8f, 1f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private InputActionProperty rotateAction;
    [Tooltip("Use X axis for horizontal rotation")]
    [SerializeField] private bool useXAxis = true;
    [Tooltip("If true, rotation is disabled (for simpler placement)")]
    [SerializeField] private bool disableRotation = true;
    
    [Header("Confirm/Cancel")]
    [SerializeField] private InputActionProperty confirmAction;
    [SerializeField] private InputActionProperty cancelAction;
    
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform drawingSurface;
    
    private GameObject rightDirectInteractor;
    
    private GameObject ghostPreview;
    private bool isInPlacementMode = false;
    private bool isValidPlacement = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentRotationY = 0f;
    private Renderer[] ghostRenderers;
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    
    public static BoardPlacementController CurrentPlacingBoard { get; private set; }
    
    public System.Action<GameObject> OnBoardPlaced;
    public System.Action OnBoardCancelled;
    
    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }
        
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        if (drawingSurface == null)
        {
            drawingSurface = transform.Find("DrawingSurface");
        }
        
        GameObject xrOrigin = GameObject.Find("XR Origin (VR)");
        if (xrOrigin != null)
        {
            Transform rightController = xrOrigin.transform.Find("Camera Offset/RightHandController/RightDirectInteractor");
            if (rightController != null)
            {
                rightDirectInteractor = rightController.gameObject;
            }
        }
    }
    
    private void OnDisable()
    {
        if (isInPlacementMode)
        {
            DisableInputActions();
        }
    }
    
    private void EnableInputActions()
    {
        if (confirmAction.action != null)
        {
            confirmAction.action.Enable();
            confirmAction.action.performed += OnConfirmPlacement;
        }
        else
        {
            Debug.LogWarning("BoardPlacementController: Confirm action not assigned. Board placement will not work.");
        }
        
        if (cancelAction.action != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPlacement;
        }
        
        if (!disableRotation && rotateAction.action != null)
        {
            rotateAction.action.Enable();
        }
    }
    
    private void DisableInputActions()
    {
        if (confirmAction.action != null)
        {
            confirmAction.action.performed -= OnConfirmPlacement;
            confirmAction.action.Disable();
        }
        
        if (cancelAction.action != null)
        {
            cancelAction.action.performed -= OnCancelPlacement;
            cancelAction.action.Disable();
        }
        
        if (rotateAction.action != null)
        {
            rotateAction.action.Disable();
        }
    }
    
    public void EnterPlacementMode()
    {
        if (isInPlacementMode) return;
        
        isInPlacementMode = true;
        CurrentPlacingBoard = this;
        
        transform.position = playerCamera.position + playerCamera.forward * 2f;
        transform.rotation = Quaternion.LookRotation(playerCamera.forward);
        
        CreateGhostPreview();
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }
        
        if (rightDirectInteractor != null)
        {
            rightDirectInteractor.SetActive(false);
        }
        
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        EnableInputActions();
    }
    
    private void CreateGhostPreview()
    {
        if (ghostPreview != null)
        {
            Destroy(ghostPreview);
        }
        
        ghostPreview = new GameObject("GhostPreview");
        ghostPreview.transform.SetParent(transform);
        ghostPreview.transform.localPosition = Vector3.zero;
        ghostPreview.transform.localRotation = Quaternion.identity;
        ghostPreview.transform.localScale = Vector3.one;
        ghostPreview.SetActive(true);
        
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("BoardPlacementController: No MeshFilter found for ghost preview");
        }
        
        int createdGhosts = 0;
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.transform == drawingSurface) continue;
            
            GameObject ghostChild = new GameObject(mf.gameObject.name + "_Ghost");
            ghostChild.transform.SetParent(ghostPreview.transform);
            ghostChild.transform.localPosition = transform.InverseTransformPoint(mf.transform.position);
            ghostChild.transform.localRotation = Quaternion.Inverse(transform.rotation) * mf.transform.rotation;
            ghostChild.transform.localScale = mf.transform.lossyScale;
            ghostChild.SetActive(true);
            
            MeshFilter ghostMF = ghostChild.AddComponent<MeshFilter>();
            ghostMF.mesh = mf.sharedMesh;
            
            MeshRenderer ghostMR = ghostChild.AddComponent<MeshRenderer>();
            ghostMR.enabled = true;
            
            if (ghostMaterial != null)
            {
                ghostMR.material = ghostMaterial;
            }
            else
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetColor("_BaseColor", validColor);
                ghostMR.material = mat;
            }
            
            createdGhosts++;
        }
        
        ghostRenderers = ghostPreview.GetComponentsInChildren<Renderer>();
        
        Renderer[] originalRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in originalRenderers)
        {
            if (r.transform.IsChildOf(ghostPreview.transform)) continue;
            r.enabled = false;
        }
    }
    
    private void Update()
    {
        if (!isInPlacementMode) return;
        
        try
        {
            HandleRotationInput();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BoardPlacementController rotation error: {e.Message}");
        }
        
        UpdateGhostPosition();
        UpdateGhostColor();
    }
    
    private void HandleRotationInput()
    {
        if (disableRotation) return;
        
        float rotationInput = 0f;
        
        if (rotateAction.action != null && rotateAction.action.enabled)
        {
            try
            {
                Vector2 stickValue = rotateAction.action.ReadValue<Vector2>();
                rotationInput = useXAxis ? stickValue.x : stickValue.y;
                
                if (Mathf.Abs(rotationInput) < 0.2f)
                {
                    rotationInput = 0f;
                }
            }
            catch (System.InvalidOperationException)
            {
                try
                {
                    float buttonValue = rotateAction.action.ReadValue<float>();
                    if (buttonValue > 0.5f)
                    {
                        rotationInput = 1f;
                    }
                }
                catch
                {
                }
            }
        }
        
        if (Mathf.Abs(rotationInput) > 0.01f)
        {
            currentRotationY += rotationInput * rotationSpeed * Time.deltaTime;
        }
    }
    
    private void UpdateGhostPosition()
    {
        if (ghostPreview == null) return;
        
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRaycastDistance, placementLayerMask))
        {
            isValidPlacement = true;
            targetPosition = hit.point + hit.normal * placementOffset;
            
            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.forward, hit.normal);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.ProjectOnPlane(playerCamera.up, hit.normal);
            }
            
            if (forward.sqrMagnitude > 0.001f)
            {
                forward.Normalize();
                Quaternion baseRotation = Quaternion.LookRotation(forward, hit.normal);
                targetRotation = baseRotation * Quaternion.Euler(0f, currentRotationY, 0f);
            }
            else
            {
                targetRotation = Quaternion.LookRotation(playerCamera.forward) * Quaternion.Euler(0f, currentRotationY, 0f);
            }
            
            ghostPreview.transform.position = targetPosition;
            ghostPreview.transform.rotation = targetRotation;
        }
        else
        {
            isValidPlacement = false;
            
            Vector3 defaultPosition = playerCamera.position + playerCamera.forward * 2f;
            ghostPreview.transform.position = defaultPosition;
            ghostPreview.transform.rotation = Quaternion.LookRotation(playerCamera.forward) * Quaternion.Euler(0f, currentRotationY, 0f);
        }
    }
    
    private void UpdateGhostColor()
    {
        if (ghostRenderers == null) return;
        
        Color targetColor = isValidPlacement ? validColor : invalidColor;
        
        foreach (Renderer r in ghostRenderers)
        {
            if (r != null && r.material != null)
            {
                r.material.SetColor("_BaseColor", targetColor);
            }
        }
    }
    
    private void OnConfirmPlacement(InputAction.CallbackContext context)
    {
        if (!isInPlacementMode || !isValidPlacement) return;
        
        ConfirmPlacement();
    }
    
    private void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (!isInPlacementMode) return;
        
        CancelPlacement();
    }
    
    private void ConfirmPlacement()
    {
        isInPlacementMode = false;
        CurrentPlacingBoard = null;
        
        if (ghostPreview != null)
        {
            transform.position = ghostPreview.transform.position;
            transform.rotation = ghostPreview.transform.rotation;
            
            Destroy(ghostPreview);
        }
        
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }
        
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        if (drawingSurface != null)
        {
            drawingSurface.gameObject.layer = LayerMask.NameToLayer("Drawing Surface");
        }
        
        DisableInputActions();
        
        if (rightDirectInteractor != null)
        {
            rightDirectInteractor.SetActive(true);
        }
        
        OnBoardPlaced?.Invoke(gameObject);
        
        DrawingSystem.Instance?.RegisterBoard(this);
    }
    
    private void CancelPlacement()
    {
        isInPlacementMode = false;
        CurrentPlacingBoard = null;
        
        if (rightDirectInteractor != null)
        {
            rightDirectInteractor.SetActive(true);
        }
        
        OnBoardCancelled?.Invoke();
        
        ItemSpawner.Instance?.ReturnCurrentItem();
    }
    
    public Transform GetDrawingSurface()
    {
        return drawingSurface;
    }
}
