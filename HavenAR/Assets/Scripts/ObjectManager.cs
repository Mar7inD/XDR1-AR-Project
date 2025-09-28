using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ObjectManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Button togglePanelButton;
    public GameObject objectPanel;
    public Button[] objectButtons;
    
    [Header("Prefabs to Spawn")]
    public GameObject[] objectPrefabs;
    
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    
    [Header("Settings")]
    public LayerMask landLayerMask = 1; // Default layer
    
    private GameObject selectedPrefab;
    private bool isDragging = false;
    private GameObject currentDragObject;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    
    void Start()
    {
        // Hide panel initially
        if (objectPanel != null)
            objectPanel.SetActive(false);
        
        // Setup button listeners
        if (togglePanelButton != null)
            togglePanelButton.onClick.AddListener(TogglePanel);
        
        // Setup object selection buttons
        for (int i = 0; i < objectButtons.Length && i < objectPrefabs.Length; i++)
        {
            int index = i; // Capture for closure
            if (objectButtons[i] != null && objectPrefabs[i] != null)
            {
                objectButtons[i].onClick.AddListener(() => SelectObject(objectPrefabs[index]));
            }
        }
    }
    
    void Update()
    {
        if (selectedPrefab != null)
        {
            HandleObjectPlacement();
        }
    }
    
    public void TogglePanel()
    {
        if (objectPanel != null)
        {
            objectPanel.SetActive(!objectPanel.activeSelf);
        }
    }
    
    public void SelectObject(GameObject prefab)
    {
        selectedPrefab = prefab;
        
        // Close panel after selection (optional)
        if (objectPanel != null)
            objectPanel.SetActive(false);
    }
    
    void HandleObjectPlacement()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 screenPosition = touch.position;
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartDragging(screenPosition);
                    break;
                    
                case TouchPhase.Moved:
                    if (isDragging && currentDragObject != null)
                    {
                        UpdateDragPosition(screenPosition);
                    }
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging)
                    {
                        FinishPlacement(screenPosition);
                    }
                    break;
            }
        }
        
        // Handle mouse input for editor testing
        #if UNITY_EDITOR
        HandleMouseInput();
        #endif
    }
    
    void StartDragging(Vector2 screenPosition)
    {
        if (IsValidLandPosition(screenPosition))
        {
            isDragging = true;
            
            // Create preview object
            Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);
            if (worldPosition != Vector3.zero)
            {
                currentDragObject = Instantiate(selectedPrefab, worldPosition, Quaternion.identity);
                
                // Make it semi-transparent during drag
                SetObjectTransparency(currentDragObject, 0.7f);
            }
        }
    }
    
    void UpdateDragPosition(Vector2 screenPosition)
    {
        if (IsValidLandPosition(screenPosition))
        {
            Vector3 worldPosition = GetWorldPositionFromScreen(screenPosition);
            if (worldPosition != Vector3.zero && currentDragObject != null)
            {
                currentDragObject.transform.position = worldPosition;
            }
        }
    }
    
    void FinishPlacement(Vector2 screenPosition)
    {
        if (currentDragObject != null)
        {
            if (IsValidLandPosition(screenPosition))
            {
                // Valid placement - make object fully opaque
                SetObjectTransparency(currentDragObject, 1f);
                
                // Add any additional placement logic here (e.g., snap to surface)
                SnapToSurface(currentDragObject);
            }
            else
            {
                // Invalid placement - destroy object
                DestroyImmediate(currentDragObject);
            }
        }
        
        // Reset state
        isDragging = false;
        currentDragObject = null;
        selectedPrefab = null;
    }
    
    bool IsValidLandPosition(Vector2 screenPosition)
    {
        // Use AR raycast to check if we hit a valid surface
        if (raycastManager != null && raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            // Check if the hit plane is classified as land/horizontal surface
            foreach (var _hit in raycastHits)
            {
                ARPlane plane = _hit.trackable as ARPlane;
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    return true;
                }
            }
        }
        
        // Fallback: use physics raycast for non-AR scenarios
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, landLayerMask))
        {
            // Additional checks can be added here (e.g., tag comparison)
            return hit.collider.CompareTag("Land") || hit.collider.CompareTag("Ground");
        }
        
        return false;
    }
    
    Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        if (raycastManager != null && raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            return raycastHits[0].pose.position;
        }
        
        // Fallback for non-AR scenarios
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, landLayerMask))
        {
            return hit.point;
        }
        
        return Vector3.zero;
    }
    
    void SetObjectTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
            }
        }
    }
    
    void SnapToSurface(GameObject obj)
    {
        // Cast a ray downward to snap object to surface
        Vector3 position = obj.transform.position;
        Ray ray = new Ray(position + Vector3.up * 2f, Vector3.down);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 5f, landLayerMask))
        {
            obj.transform.position = hit.point;
        }
    }
    
    #if UNITY_EDITOR
    void HandleMouseInput()
    {
        Vector2 mousePosition = Input.mousePosition;
        
        if (Input.GetMouseButtonDown(0))
        {
            StartDragging(mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateDragPosition(mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            FinishPlacement(mousePosition);
        }
    }
    #endif
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (togglePanelButton != null)
            togglePanelButton.onClick.RemoveAllListeners();
        
        foreach (var button in objectButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}