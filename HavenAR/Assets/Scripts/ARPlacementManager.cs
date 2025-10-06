using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

public class ARPlacementManager : MonoBehaviour
{
    public static ARPlacementManager Instance;

    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Preview Settings")]
    [SerializeField] private float previewAlpha = 0.5f;
    [SerializeField] private Color previewTint = Color.white;

    [Header("Environment Settings")]
    [SerializeField] private bool allowMultipleEnvironments = false;

    [Header("Environment Tags")]
    [SerializeField] private string[] environmentTags = { "Environment" };

    [Header("Environment Spawn Tags")]
    [SerializeField] private string[] environmentSpawnTags = { "Land" };

    [Header("AR Plane Management")]
    public ARPlaneManager planeManager;

    [Header("Object Manipulation")]
    public ObjectManipulator objectManipulator;

    [Header("UI References")]
    public GameObject environmentButton;
    public GameObject objectsButton;

    // Current state
    private PanelToggle environmentPanelToggle;
    private PanelToggle objectsPanelToggle;
    private GameObject currentPrefab;
    private GameObject previewInstance;
    private GameObject currentEnvironment;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    // Drag and drop state
    private bool isDragging = false;

    // Input System references
    private Touchscreen touchscreen;
    private Mouse mouse;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
        if (arCamera == null)
            arCamera = Camera.main;

        // Initialize panel toggle
        if (environmentButton != null)
            environmentPanelToggle = environmentButton.GetComponent<PanelToggle>();

        if (objectsButton != null)
            objectsPanelToggle = objectsButton.GetComponent<PanelToggle>();

        // Get object manipulator if not assigned
        if (objectManipulator == null)
            objectManipulator = FindFirstObjectByType<ObjectManipulator>();

        // Get input devices
        touchscreen = Touchscreen.current;
        mouse = Mouse.current;
    }

    void Update()
    {
        // Only handle touch input if not dragging from UI
        if (!isDragging)
        {
            HandleInput();
        }
    }

    #region Input Handling
    void HandleInput()
    {
        // Only handle placement input if not manipulating objects and not dragging from UI
        if (objectManipulator != null && objectManipulator.HasSelectedObject())
        {
            return; // Let ObjectManipulator handle input
        }

        // Handle touch input on mobile
        if (touchscreen != null && touchscreen.touches.Count > 0)
        {
            var touch = touchscreen.touches[0];
            HandleTouch(touch.position.ReadValue(), touch.phase.ReadValue());
        }
        // Handle mouse input in editor
        else if (Application.isEditor && mouse != null)
        {
            HandleMouseInput();
        }
    }

    void HandleTouch(Vector2 screenPos, UnityEngine.InputSystem.TouchPhase phase)
    {
        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                if (currentPrefab == null) SelectObjectAtPosition(screenPos);
                break;
            case UnityEngine.InputSystem.TouchPhase.Moved:
                if (previewInstance != null) UpdatePreview(screenPos);
                break;
            case UnityEngine.InputSystem.TouchPhase.Ended:
            case UnityEngine.InputSystem.TouchPhase.Canceled:
                if (previewInstance != null) PlaceObject(screenPos);
                break;
        }
    }

    void HandleMouseInput()
    {
        Vector2 mousePos = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame && currentPrefab == null)
        {
            SelectObjectAtPosition(mousePos);
        }
        else if (mouse.leftButton.isPressed && previewInstance != null)
        {
            UpdatePreview(mousePos);
        }
        else if (mouse.leftButton.wasReleasedThisFrame && previewInstance != null)
        {
            PlaceObject(mousePos);
        }
    }
    #endregion

    #region Drag and Drop Interface
    public void StartDragging(GameObject prefab)
    {
        if (prefab == null) return;

        // Check environment restrictions
        if (IsEnvironmentPrefab(prefab) && !allowMultipleEnvironments && currentEnvironment != null)
        {
            Debug.LogWarning("Cannot place multiple environments");
            return;
        }

        currentPrefab = prefab;
        // Match scale to current environment if applicable
        if (currentEnvironment != null && !IsEnvironmentPrefab(currentPrefab))
            currentPrefab.transform.localScale = currentEnvironment.transform.localScale;

        isDragging = true;
        CreatePreview();
    }

    public void UpdateDragging(Vector2 screenPos)
    {
        if (!isDragging || previewInstance == null) return;

        UpdatePreview(screenPos);
    }

    public void EndDragging(Vector2 screenPos)
    {
        if (!isDragging) return;

        isDragging = false;

        if (previewInstance != null && previewInstance.activeInHierarchy)
        {
            PlaceObject(screenPos);

        }
        else
        {
            CancelPlacement();
        }
    }
    #endregion

    #region Object Selection and Placement
    void SelectObjectAtPosition(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            // Handle existing object selection logic here
            SelectExistingObject(hitObject);
        }
    }

    void CreatePreview()
    {
        Debug.Log("Creating preview for " + currentPrefab.name);
        if (currentPrefab == null)
        {
            return;
        }

        CleanupPreview();
        previewInstance = Instantiate(currentPrefab);
        SetPreviewMode(previewInstance, true);
        previewInstance.SetActive(false); // Start hidden until valid position found
    }

    void UpdatePreview(Vector2 screenPos)
    {
        if (previewInstance == null || raycastManager == null) return;

        TrackableType trackableType = IsEnvironmentPrefab(currentPrefab) ?
            TrackableType.PlaneWithinPolygon : TrackableType.AllTypes;

        if (raycastManager.Raycast(screenPos, raycastHits, trackableType))
        {
            Pose pose = raycastHits[0].pose;

            // Adjust position to be on top of land colliders if not an environment object
            if (!IsEnvironmentPrefab(currentPrefab))
            {
                Vector3? adjustedPosition = AdjustPositionForLand(pose.position, screenPos);
                if (adjustedPosition.HasValue)
                {
                    previewInstance.transform.position = adjustedPosition.Value;
                    previewInstance.transform.rotation = pose.rotation;
                    previewInstance.SetActive(true);
                }
                else
                {
                    previewInstance.SetActive(false); // Hide preview if no valid land position
                }
            }
            else
            {
                previewInstance.transform.position = pose.position;
                previewInstance.transform.rotation = pose.rotation;
                previewInstance.SetActive(true);
            }
        }
        else
        {
            previewInstance.SetActive(false);
        }
    }

    void PlaceObject(Vector2 screenPos)
    {
        if (previewInstance == null || !previewInstance.activeInHierarchy)
        {
            CancelPlacement();
            return;
        }

        Vector3 finalPosition = previewInstance.transform.position;

        // For non-environment objects, ensure they're placed on top of land
        if (!IsEnvironmentPrefab(currentPrefab))
        {
            Vector3? adjustedPosition = AdjustPositionForLand(finalPosition, screenPos);
            if (adjustedPosition.HasValue)
            {
                finalPosition = adjustedPosition.Value;
            }
            else
            {
                // No valid land position found, cancel placement
                CancelPlacement();
                return;
            }
        }

        // Convert preview to real object
        GameObject placedObject = Instantiate(currentPrefab,
            finalPosition,
            previewInstance.transform.rotation);

        SetPreviewMode(placedObject, false);

        // Ensure the placed object has a PrefabIdentifier
        EnsurePrefabIdentifier(placedObject);

        // Track the object
        if (IsEnvironmentPrefab(currentPrefab))
        {
            if (currentEnvironment != null)
                Destroy(currentEnvironment);
            currentEnvironment = placedObject;

            // Hide AR planes when environment is placed
            HideARPlanes();

            // Hide environment UI when environment is placed
            HidePanel(environmentButton);
        }
        else
        {
            // Parent object to current environment if one exists
            if (currentEnvironment != null)
            {
                placedObject.transform.SetParent(currentEnvironment.transform);
                Debug.Log($"Placed {placedObject.name} as child of environment {currentEnvironment.name}");
            }
            else
            {
                Debug.Log($"Placed {placedObject.name} as independent object (no environment)");
            }
            
            // Add to spawned objects list for ObjectManipulator access
            spawnedObjects.Add(placedObject);
            HidePanel(objectsButton);
        }

        CleanupPlacement();
    }

    public void CancelPlacement()
    {
        CleanupPlacement();

        // Also deselect any selected object
        if (objectManipulator != null)
        {
            objectManipulator.DeselectCurrentObject();
        }
    }

    void CleanupPlacement()
    {
        CleanupPreview();
        currentPrefab = null;
        isDragging = false;
    }
    #endregion

    #region Object Management
    void SelectExistingObject(GameObject obj)
    {
        // Handle object selection for deletion/modification
        // Add glow effect, enable delete buttons, etc.
    }
    #endregion

    #region Helper Methods
    void SetPreviewMode(GameObject obj, bool isPreview)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (isPreview)
            {
                // Create preview materials for each material
                Material[] originalMaterials = renderer.materials;
                Material[] previewMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    Material originalMat = originalMaterials[i];
                    Material previewMat = new Material(originalMat);

                    // Try different common color properties
                    if (previewMat.HasProperty("_Color"))
                    {
                        Color color = previewMat.color;
                        color.a = previewAlpha;
                        previewMat.color = color;
                    }
                    else if (previewMat.HasProperty("_BaseColor"))
                    {
                        Color color = previewMat.GetColor("_BaseColor");
                        color.a = previewAlpha;
                        previewMat.SetColor("_BaseColor", color);
                    }
                    else if (previewMat.HasProperty("_MainColor"))
                    {
                        Color color = previewMat.GetColor("_MainColor");
                        color.a = previewAlpha;
                        previewMat.SetColor("_MainColor", color);
                    }
                    else
                    {
                        // Fallback: try to make it transparent by changing rendering mode
                        if (previewMat.HasProperty("_Mode"))
                        {
                            previewMat.SetFloat("_Mode", 3); // Transparent mode
                        }
                        if (previewMat.HasProperty("_SrcBlend"))
                        {
                            previewMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        }
                        if (previewMat.HasProperty("_DstBlend"))
                        {
                            previewMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        }
                        if (previewMat.HasProperty("_ZWrite"))
                        {
                            previewMat.SetFloat("_ZWrite", 0);
                        }

                        // Apply tint color if possible
                        previewMat.color = new Color(previewTint.r, previewTint.g, previewTint.b, previewAlpha);
                    }

                    previewMaterials[i] = previewMat;
                }

                renderer.materials = previewMaterials;
            }
            // For real objects, materials are already correct from prefab
        }

        // Disable colliders for preview
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = !isPreview;
        }
    }

    Vector3? AdjustPositionForLand(Vector3 basePosition, Vector2 screenPos)
    {
        // Shoot a ray downward from the object's position to check for land
        Vector3 rayOrigin = new Vector3(basePosition.x, basePosition.y + 5f, basePosition.z);
        Ray ray = new Ray(rayOrigin, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(ray, 10f);

        foreach (RaycastHit hit in hits)
        {
            foreach (string tag in environmentSpawnTags)
            {
                if (hit.collider.CompareTag(tag))
                {
                    // Found land, place object on top of it
                    return new Vector3(basePosition.x, hit.point.y, basePosition.z);
                }
            }
        }

        // Return null to indicate invalid position
        return null;
    }

    void EnsurePrefabIdentifier(GameObject obj)
    {
        PrefabIdentifier identifier = obj.GetComponent<PrefabIdentifier>();
        if (identifier == null)
        {
            // Add PrefabIdentifier if it doesn't exist
            identifier = obj.AddComponent<PrefabIdentifier>();
            identifier.prefabName = currentPrefab.name;
            identifier.isEnvironmentPrefab = IsEnvironmentPrefab(currentPrefab);
            identifier.isScalable = !identifier.isEnvironmentPrefab; // Environments typically shouldn't be scaled

            Debug.Log($"Added PrefabIdentifier to {obj.name}: isEnvironment={identifier.isEnvironmentPrefab}, isScalable={identifier.isScalable}");
        }
        else
        {
            // Update existing identifier
            if (string.IsNullOrEmpty(identifier.prefabName))
            {
                identifier.prefabName = currentPrefab.name;
            }
            identifier.isEnvironmentPrefab = IsEnvironmentPrefab(currentPrefab);
        }
    }

    void CleanupPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    bool IsEnvironmentPrefab(GameObject prefab)
    {
        if (prefab == null) return false;

        foreach (string tag in environmentTags)
        {
            if (prefab.CompareTag(tag))
                return true;
        }

        return false;
    }

    public bool HasEnvironment() => currentEnvironment != null;
    public GameObject GetCurrentEnvironment() => currentEnvironment;
    public bool IsPlacing => previewInstance != null || isDragging;
    #endregion

    #region Delete Functionality
    public void DeleteAllObjects()
    {
        // Remove all spawned objects
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Debug.Log($"Deleting object: {obj.name}");
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();

        Debug.Log($"Deleted {spawnedObjects.Count} objects");
    }

    public void DeleteEnvironment()
    {
        if (currentEnvironment != null)
        {
            Debug.Log($"Deleting environment: {currentEnvironment.name}");

            // Remove child objects from spawned objects list before destroying environment
            Transform[] children = currentEnvironment.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child != currentEnvironment.transform) // Don't remove the environment itself
                {
                    spawnedObjects.Remove(child.gameObject);
                }
            }

            Destroy(currentEnvironment);
            currentEnvironment = null;

            // Show AR planes again when environment is deleted
            ShowARPlanes();

            // Show environment UI again when environment is deleted
            ShowPanel(environmentButton);
        }
    }

    private void HideARPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                if (plane.TryGetComponent<ARFeatheredPlaneMeshVisualizerCompanion>(out var visualizer))
                {
                    visualizer.visualizeSurfaces = false;
                }
                else
                {
                    // Fallback for standard plane visualization
                    var meshRenderer = plane.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                        meshRenderer.enabled = false;
                }
            }
        }
    }

    private void ShowARPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                if (plane.TryGetComponent<ARFeatheredPlaneMeshVisualizerCompanion>(out var visualizer))
                {
                    visualizer.visualizeSurfaces = true;
                }
                else
                {
                    // Fallback for standard plane visualization
                    var meshRenderer = plane.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                        meshRenderer.enabled = true;
                }
            }
        }
    }

    public void DeleteEverything()
    {
        DeleteAllObjects();
        DeleteEnvironment();

        // Also cancel any current placement
        if (IsPlacing)
        {
            CancelPlacement();
        }

        Debug.Log("Deleted everything in the scene");
    }

    #region UI Management
    private void HidePanel(GameObject button)
    {
        if (button != null)
        {
            if (IsEnvironmentPrefab(button))
            {
                button.SetActive(false);
                environmentPanelToggle.HidePanel();
                Debug.Log("Hidden environment button");
            }
            else
            {
                objectsPanelToggle.HidePanel();
                Debug.Log("Hidden objects button");
            }
        }
    }

    private void ShowPanel(GameObject button)
    {
        if (IsEnvironmentPrefab(button))
        {
            environmentButton.SetActive(true);
            Debug.Log("Shown environment button");
        }
    }
    #endregion
    #endregion

    #region Utility
    public List<GameObject> GetSpawnedObjects()
    {
        // Clean up null references
        spawnedObjects.RemoveAll(obj => obj == null);
        return spawnedObjects;
    }

    public void RemoveSpawnedObject(GameObject obj)
    {
        spawnedObjects.Remove(obj);
    }
    #endregion
}