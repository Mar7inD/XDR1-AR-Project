using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ScaleManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider scaleSlider;
    [SerializeField] private Button resetButton;
    [SerializeField] private GameObject scalePanel;
    [SerializeField] private TextMeshProUGUI scaleValueText;
    [SerializeField] private TextMeshProUGUI selectedObjectText;

    [Header("Scale Settings")]
    [SerializeField] private float minScale = 0.01f;
    [SerializeField] private float maxScale = 3.0f;

    [Header("Selection Visual")]
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private Color selectionColor = Color.yellow;
    [SerializeField] private float selectionGlowIntensity = 2.0f;

    // Current selection state
    private GameObject selectedObject;
    private Vector3 originalScale;
    private float originalScaleValue;
    
    // Selection visual state
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    
    // Input references
    private Camera arCamera;
    private Touchscreen touchscreen;
    private Mouse mouse;

    void Start()
    {
        // Setup slider
        if (scaleSlider != null)
        {
            scaleSlider.minValue = minScale;
            scaleSlider.maxValue = maxScale;
            scaleSlider.value = 1.0f;
            scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        }

        // Setup reset button
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetScale);
        }

        // Hide panel initially
        if (scalePanel != null)
        {
            scalePanel.SetActive(false);
        }

        // Get camera reference
        arCamera = Camera.main;
        if (arCamera == null)
            arCamera = FindFirstObjectByType<Camera>();

        // Get input devices
        touchscreen = Touchscreen.current;
        mouse = Mouse.current;

        UpdateUI();
    }

    void Update()
    {
        HandleInput();
        
        // Show/hide scale panel based on selection
        if (scalePanel != null)
        {
            scalePanel.SetActive(selectedObject != null);
        }
    }

    #region Input Handling
    private void HandleInput()
    {
        // Skip input if placement manager is currently placing objects
        if (ARPlacementManager.Instance != null && ARPlacementManager.Instance.IsPlacing)
            return;

        // Handle touch input on mobile
        if (touchscreen != null && touchscreen.touches.Count > 0)
        {
            var touch = touchscreen.touches[0];
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 touchPos = touch.position.ReadValue();
                
                // Check if touch is over UI
                if (!IsPointerOverUIElement(touchPos))
                {
                    HandleSelection(touchPos);
                }
            }
        }
        // Handle mouse input in editor
        else if (Application.isEditor && mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                
                // Check if mouse is over UI
                if (!IsPointerOverUIElement(mousePos))
                {
                    HandleSelection(mousePos);
                }
            }
        }
    }

    private bool IsPointerOverUIElement(Vector2 screenPosition)
    {
        // Create a pointer event data
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        // Raycast using the Graphics Raycaster and UI Raycaster
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        // Check if any UI elements were hit
        return raycastResults.Count > 0;
    }

    private void HandleSelection(Vector2 screenPos)
    {
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Find the root prefab object
            GameObject rootObject = FindRootPrefabObject(hitObject);
            
            // Check if it's a scalable object (not UI or other non-scalable objects)
            if (IsScalableObject(rootObject))
            {
                SelectObject(rootObject);
            }
            else
            {
                // Clicked on something that's not scalable, deselect current object
                DeselectObject();
            }
        }
        else
        {
            // Clicked on empty space, deselect current object
            DeselectObject();
        }
    }

    private GameObject FindRootPrefabObject(GameObject hitObject)
    {
        // Strategy 1: Look for the topmost object that has specific components or tags
        Transform current = hitObject.transform;
        GameObject candidateRoot = hitObject;

        // Traverse up the hierarchy to find the main prefab root
        while (current != null)
        {
            GameObject currentObj = current.gameObject;
            
            // Check if this object seems like a prefab root
            // You can customize these conditions based on your prefab structure
            if (IsPrefabRoot(currentObj))
            {
                candidateRoot = currentObj;
            }
            
            // If we've reached an object that's clearly a scene root or environment container, stop
            if (IsSceneRoot(currentObj))
            {
                break;
            }
            
            current = current.parent;
        }

        return candidateRoot;
    }

    private bool IsPrefabRoot(GameObject obj)
    {
        // Customize this logic based on your prefab naming/tagging conventions
        
        // Strategy 1: Check for specific tags that indicate prefab roots
        if (obj.CompareTag("Environment") || obj.CompareTag("Prop") || obj.CompareTag("Building"))
        {
            return true;
        }
        
        // Strategy 2: Check if object has certain components that indicate it's a prefab root
        // For example, if your prefabs have specific scripts at the root
        if (obj.GetComponent<PrefabIdentifier>() != null) // You'd need to create this component
        {
            return true;
        }
        
        // Strategy 3: Check naming conventions
        // If your prefabs follow a naming pattern like "Prefab_House", "Prefab_Tree", etc.
        if (obj.name.StartsWith("Prefab_") || obj.name.EndsWith("(Clone)"))
        {
            return true;
        }
        
        // Strategy 4: Check if object is at a reasonable hierarchy level
        // If the object is too deep in the hierarchy, it's probably a child component
        Transform root = obj.transform.root;
        int hierarchyDepth = GetHierarchyDepth(obj.transform, root);
        
        // If it's within 2-3 levels from the instantiated prefab, consider it a potential root
        if (hierarchyDepth <= 2)
        {
            return true;
        }
        
        return false;
    }

    private bool IsSceneRoot(GameObject obj)
    {
        // Check if this is a scene management object that we shouldn't traverse past
        if (obj.name.Contains("AR Session") || 
            obj.name.Contains("XR Origin") || 
            obj.name.Contains("Camera") ||
            obj.CompareTag("GameController") ||
            obj.layer == LayerMask.NameToLayer("UI"))
        {
            return true;
        }
        
        // If object has no parent, it's definitely a scene root
        return obj.transform.parent == null;
    }

    private int GetHierarchyDepth(Transform child, Transform root)
    {
        int depth = 0;
        Transform current = child;
        
        while (current != null && current != root)
        {
            depth++;
            current = current.parent;
        }
        
        return depth;
    }

    private bool IsScalableObject(GameObject obj)
    {
        // Don't scale null objects
        if (obj == null) return false;
        
        // Don't scale UI objects
        if (obj.layer == LayerMask.NameToLayer("UI")) return false;
        
        // Don't scale AR system objects
        if (obj.name.Contains("AR Session") || 
            obj.name.Contains("XR Origin") || 
            obj.name.Contains("Camera"))
        {
            return false;
        }
        
        // Check for specific scalable tags
        if (obj.CompareTag("Environment") || 
            obj.CompareTag("Prop") || 
            obj.CompareTag("Building") ||
            obj.CompareTag("Scalable"))
        {
            return true;
        }
        
        // If no specific tags, allow objects that seem to be prefab instances
        if (obj.name.EndsWith("(Clone)"))
        {
            return true;
        }
        
        // Default to allowing scaling for most objects
        return true;
    }
    #endregion

    #region Object Selection
    private void SelectObject(GameObject obj)
    {
        // If same object is already selected, do nothing
        if (selectedObject == obj) return;

        // Deselect previous object
        DeselectObject();

        // Select new object
        selectedObject = obj;
        originalScale = obj.transform.localScale;
        originalScaleValue = Mathf.Max(originalScale.x, originalScale.y, originalScale.z); // Use the largest component

        // Update slider range and value
        if (scaleSlider != null)
        {
            // Adjust slider range based on original scale
            float rangeMultiplier = originalScaleValue;
            scaleSlider.minValue = minScale * rangeMultiplier;
            scaleSlider.maxValue = maxScale * rangeMultiplier;
            scaleSlider.value = originalScaleValue;
        }

        // Apply selection visual
        ApplySelectionVisual(true);

        UpdateUI();
        
        Debug.Log($"Selected object: {obj.name} with original scale: {originalScale}");
    }

    private void DeselectObject()
    {
        if (selectedObject != null)
        {
            // Remove selection visual
            ApplySelectionVisual(false);
            
            Debug.Log($"Deselected object: {selectedObject.name}");
            selectedObject = null;
        }

        UpdateUI();
    }
    #endregion

    #region Selection Visual
    private void ApplySelectionVisual(bool selected)
    {
        if (selectedObject == null) return;

        Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>();

        if (selected)
        {
            // Store original materials and apply selection effect
            foreach (Renderer renderer in renderers)
            {
                if (!originalMaterials.ContainsKey(renderer))
                {
                    originalMaterials[renderer] = renderer.materials;
                }

                // Create glowing materials
                Material[] glowMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    Material originalMat = renderer.materials[i];
                    
                    if (selectionMaterial != null)
                    {
                        // Use custom selection material
                        glowMaterials[i] = new Material(selectionMaterial);
                        if (glowMaterials[i].HasProperty("_Color"))
                            glowMaterials[i].SetColor("_Color", selectionColor);
                        if (glowMaterials[i].HasProperty("_EmissionColor"))
                            glowMaterials[i].SetColor("_EmissionColor", selectionColor * selectionGlowIntensity);
                    }
                    else
                    {
                        // Create a copy of the original material with emission
                        glowMaterials[i] = new Material(originalMat);
                        
                        // Enable emission
                        glowMaterials[i].EnableKeyword("_EMISSION");
                        if (glowMaterials[i].HasProperty("_EmissionColor"))
                        {
                            glowMaterials[i].SetColor("_EmissionColor", selectionColor * selectionGlowIntensity);
                        }
                        
                        // Tint the base color slightly
                        if (glowMaterials[i].HasProperty("_Color"))
                        {
                            Color baseColor = glowMaterials[i].color;
                            glowMaterials[i].color = Color.Lerp(baseColor, selectionColor, 0.3f);
                        }
                        else if (glowMaterials[i].HasProperty("_BaseColor"))
                        {
                            Color baseColor = glowMaterials[i].GetColor("_BaseColor");
                            glowMaterials[i].SetColor("_BaseColor", Color.Lerp(baseColor, selectionColor, 0.3f));
                        }
                    }
                }
                
                renderer.materials = glowMaterials;
            }
        }
        else
        {
            // Restore original materials
            foreach (Renderer renderer in renderers)
            {
                if (originalMaterials.ContainsKey(renderer))
                {
                    renderer.materials = originalMaterials[renderer];
                }
            }
            originalMaterials.Clear();
        }
    }
    #endregion

    #region Scaling
    private void OnScaleChanged(float value)
    {
        if (selectedObject != null)
        {
            // Calculate scale ratio from original
            float scaleRatio = value / originalScaleValue;
            Vector3 newScale = originalScale * scaleRatio;
            
            selectedObject.transform.localScale = newScale;
            UpdateUI();
        }
    }

    private void ResetScale()
    {
        if (selectedObject != null && scaleSlider != null)
        {
            scaleSlider.value = originalScaleValue;
        }
    }
    #endregion

    #region UI Updates
    private void UpdateUI()
    {
        UpdateScaleText();
        UpdateSelectedObjectText();
    }

    private void UpdateScaleText()
    {
        if (scaleValueText != null && scaleSlider != null)
        {
            if (selectedObject != null)
            {
                float scaleRatio = scaleSlider.value / originalScaleValue;
                scaleValueText.text = $"Scale: {scaleSlider.value:F2} ({scaleRatio:P0})";
            }
            else
            {
                scaleValueText.text = "Scale: No object selected";
            }
        }
    }

    private void UpdateSelectedObjectText()
    {
        if (selectedObjectText != null)
        {
            if (selectedObject != null)
            {
                selectedObjectText.text = $"Selected: {selectedObject.name}";
            }
            else
            {
                selectedObjectText.text = "Tap an object to scale it";
            }
        }
    }
    #endregion

    #region Public Interface
    public GameObject GetSelectedObject()
    {
        return selectedObject;
    }

    public void ForceDeselectObject()
    {
        DeselectObject();
    }

    public void SelectSpecificObject(GameObject obj)
    {
        if (IsScalableObject(obj))
        {
            SelectObject(obj);
        }
    }

    public float GetCurrentScaleRatio()
    {
        if (selectedObject != null && scaleSlider != null)
        {
            return scaleSlider.value / originalScaleValue;
        }
        return 1.0f;
    }
    #endregion
}