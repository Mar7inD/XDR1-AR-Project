using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SelectorManager : MonoBehaviour
{
    public static SelectorManager Instance;

    [Header("Preview Settings")]
    [SerializeField] private float previewAlpha = 0.5f;
    [SerializeField] private Color previewTint = Color.white;

    [Header("Environment Management")]
    [SerializeField] private bool allowMultipleEnvironments = false;

    [Header("Scale Controller")]
    [SerializeField] private EnvironmentScaleController scaleController;

    [Header("Environment Button")]
    [SerializeField] private GameObject environmentButton;
    [SerializeField] private PanelToggle panelToggle;


    private GameObject prefabToSpawn;
    private GameObject previewInstance;
    private GameObject currentEnvironment; // Track the spawned environment
    private Dictionary<Renderer, MaterialInfo> originalMaterials = new Dictionary<Renderer, MaterialInfo>();

    private Camera cam;
    private ARRaycastManager raycastManager;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Store original material info
    private struct MaterialInfo
    {
        public Material originalMaterial;
        public Color originalColor;
        public float originalAlpha;
        public string colorPropertyName;
    }

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
        cam = Camera.main;
        raycastManager = FindObjectOfType<ARRaycastManager>();
        
        if (raycastManager == null)
        {
            Debug.LogError("ARRaycastManager not found! Make sure AR Session Origin has ARRaycastManager component.");
        }
    }

    public void StartDragging(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Trying to drag null prefab");
            return;
        }

        // Check if an environment already exists and multiple environments are not allowed
        if (!allowMultipleEnvironments && HasActiveEnvironment())
        {
            Debug.LogWarning("Cannot spawn new environment. Remove the current environment first.");
            return;
        }

        prefabToSpawn = prefab;
        CleanupPreview();

        previewInstance = Instantiate(prefab);
        SetPreviewMode(previewInstance, true);
    }

    public void UpdateDragging(Vector2 screenPos)
    {
        if (previewInstance == null || raycastManager == null) return;

        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;
            previewInstance.transform.position = pose.position;
            previewInstance.transform.rotation = pose.rotation;
            
            // Make preview visible if it was hidden
            if (!previewInstance.activeInHierarchy)
                previewInstance.SetActive(true);
        }
        else
        {
            // Hide preview when not over a valid surface
            if (previewInstance.activeInHierarchy)
                previewInstance.SetActive(false);
        }
    }

    public void EndDragging(Vector2 screenPos)
    {
        if (prefabToSpawn == null || raycastManager == null) return;

        // Check again before spawning
        if (!allowMultipleEnvironments && HasActiveEnvironment())
        {
            Debug.LogWarning("Cannot spawn new environment. Remove the current environment first.");
            CleanupPreview();
            prefabToSpawn = null;
            return;
        }

        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;
            GameObject spawnedObject = Instantiate(prefabToSpawn, pose.position, pose.rotation);

            // Store reference to the spawned environment
            if (!allowMultipleEnvironments)
            {
                currentEnvironment = spawnedObject;

                // Add a component to handle destruction events
                var envTracker = spawnedObject.AddComponent<EnvironmentTracker>();
                envTracker.Initialize(this);

                // IMPORTANT: Notify scale controller about new environment
                if (scaleController != null)
                {
                    Debug.Log($"Notifying scale controller about spawned environment: {spawnedObject.name}");
                    scaleController.OnEnvironmentSpawned(spawnedObject);
                }
                else
                {
                    Debug.LogWarning("Scale controller is null! Make sure it's assigned in the inspector.");
                }

                DisableEnvironmentButton();
                CloseEnvironmentPanel();
            }
            
            Debug.Log($"Spawned {prefabToSpawn.name} at {pose.position}");
        }

        CleanupPreview();
        prefabToSpawn = null;
    }

        private void DisableEnvironmentButton()
    {
        environmentButton.SetActive(false);
        
        Debug.Log("Environment button disabled");
    }

    private void CloseEnvironmentPanel()
    {
        if (panelToggle != null)
        {
            panelToggle.TogglePanel();
        }
        
        Debug.Log("Environment panel closed");
    }

    // Method to get current environment for the scale controller
    public GameObject GetCurrentEnvironment()
    {
        return currentEnvironment;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // Method to remove current environment
    public void RemoveCurrentEnvironment()
    {
        if (currentEnvironment != null)
        {
            // Notify scale controller that environment is being removed
            if (scaleController != null)
            {
                scaleController.OnEnvironmentRemoved();
            }
            
            Destroy(currentEnvironment);
            currentEnvironment = null;
            
            // Re-enable environment button when environment is removed
            EnableEnvironmentButton();
            
            Debug.Log("Environment removed");
        }
    }

    private void EnableEnvironmentButton()
    {
        environmentButton.SetActive(true);
        
        Debug.Log("Environment button enabled");
    }

    // Check if there's an active environment
    public bool HasActiveEnvironment()
    {
        return currentEnvironment != null;
    }

    // Called by EnvironmentTracker when the environment is destroyed
    internal void OnEnvironmentDestroyed(GameObject destroyedEnvironment)
    {
        if (currentEnvironment == destroyedEnvironment)
        {
            currentEnvironment = null;
        }
    }

    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            RestoreOriginalMaterials();
            Destroy(previewInstance);
            previewInstance = null;
        }
        originalMaterials.Clear();
    }

    private void SetPreviewMode(GameObject obj, bool isPreview)
    {
        if (obj == null) return;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            if (isPreview)
            {
                ApplyPreviewMaterial(renderer);
            }
            else
            {
                RestoreOriginalMaterial(renderer);
            }
        }
    }

    private void ApplyPreviewMaterial(Renderer renderer)
    {
        // Store original material info
        var materialInfo = new MaterialInfo();
        materialInfo.originalMaterial = renderer.material;
        
        // Find the correct color property
        string[] colorProperties = { "_BaseColor", "_Color", "_WaterColor", "_MainColor", "_Tint", "_Albedo" };
        string validProperty = null;
        Color originalColor = Color.white;

        foreach (string property in colorProperties)
        {
            if (renderer.material.HasProperty(property))
            {
                validProperty = property;
                originalColor = renderer.material.GetColor(property);
                break;
            }
        }

        materialInfo.colorPropertyName = validProperty;
        materialInfo.originalColor = originalColor;
        materialInfo.originalAlpha = originalColor.a;

        originalMaterials[renderer] = materialInfo;

        // Apply preview effect
        if (validProperty != null)
        {
            Color previewColor = originalColor * previewTint;
            previewColor.a = previewAlpha;
            renderer.material.SetColor(validProperty, previewColor);
        }

        // Handle transparency if material supports it
        SetMaterialTransparent(renderer.material);
    }

    private void RestoreOriginalMaterial(Renderer renderer)
    {
        if (originalMaterials.TryGetValue(renderer, out MaterialInfo info))
        {
            if (info.colorPropertyName != null)
            {
                renderer.material.SetColor(info.colorPropertyName, info.originalColor);
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                RestoreOriginalMaterial(kvp.Key);
            }
        }
    }

    private void SetMaterialTransparent(Material material)
    {
        // Try to set render mode for transparency (URP/Built-in specific)
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1); // Transparent
        }
        else if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3); // Transparent mode for Standard shader
        }

        // Set render queue
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void OnDestroy()
    {
        CleanupPreview();
    }

    // Optional: Public method to cancel current dragging operation
    public void CancelDragging()
    {
        CleanupPreview();
        prefabToSpawn = null;
    }

    public bool IsDragging => previewInstance != null;
}

// Helper component to track when environment is destroyed
public class EnvironmentTracker : MonoBehaviour
{
    private SelectorManager selectorManager;

    public void Initialize(SelectorManager manager)
    {
        selectorManager = manager;
    }

    void OnDestroy()
    {
        if (selectorManager != null)
        {
            selectorManager.OnEnvironmentDestroyed(gameObject);
        }
    }
}