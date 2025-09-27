using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

public class ObjectPlacementManager : MonoBehaviour
{
    public GameObject objectPrefab;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private GameObject selectedObject; // Currently selected object

    public Button placeButton;
    public Button removeButton;
    public Button deleteSelectedButton; // New button to delete selected object

    private ARRaycastManager raycastManager;
    private Camera arCamera;

    private bool spawningEnabled = false;
    
    // Store original materials for restoration
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    void Start()
    {
        raycastManager = FindFirstObjectByType<ARRaycastManager>();
        arCamera = Camera.main;

        placeButton.onClick.AddListener(() => EnableSpawning());
        removeButton.onClick.AddListener(RemoveAllObjects);
        deleteSelectedButton.onClick.AddListener(DeleteSelectedObject);
        
        removeButton.interactable = false;
        deleteSelectedButton.interactable = false;
    }

    void EnableSpawning()
    {
        spawningEnabled = !spawningEnabled;
        Debug.Log("Spawning Enabled: " + spawningEnabled);
    }

    void Update()
    {
        // Check for touch input
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            HandleTouch(touchPosition);
        }

        // For testing in editor with mouse
        #if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            HandleTouch(mousePosition);
        }
        #endif
    }

    void HandleTouch(Vector2 screenPosition)
    {
        // First try to select an existing object
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Check if we hit one of our spawned objects
            if (spawnedObjects.Contains(hitObject))
            {
                SelectObject(hitObject);
                return;
            }
        }
        
        // If no object was hit and spawning is enabled, try to place new object
        if (spawningEnabled)
        {
            PlaceObject(screenPosition);
        }
    }

    void SelectObject(GameObject obj)
    {
        // Deselect previous object
        if (selectedObject != null)
        {
            RestoreOriginalMaterial(selectedObject);
        }
        
        // Select new object
        selectedObject = obj;
        
        // Add glow effect to selected object
        AddGlowEffect(selectedObject);
        
        // Enable delete selected button
        deleteSelectedButton.interactable = true;
        
        Debug.Log("Selected object: " + selectedObject.name);
    }

    void AddGlowEffect(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Store original material if not already stored
            if (!originalMaterials.ContainsKey(obj))
            {
                originalMaterials[obj] = renderer.material;
            }
            
            // Create glow material
            Material glowMaterial = new Material(Shader.Find("Standard"));
            glowMaterial.color = Color.yellow;
            
            // Enable emission for glow effect
            glowMaterial.EnableKeyword("_EMISSION");
            glowMaterial.SetColor("_EmissionColor", Color.yellow * 0.8f); // Bright yellow emission
            
            // Apply glow material
            renderer.material = glowMaterial;
        }
    }

    void RestoreOriginalMaterial(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && originalMaterials.ContainsKey(obj))
        {
            renderer.material = originalMaterials[obj];
        }
    }

    void PlaceObject(Vector2 screenPosition)
    {
        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = hits[0].pose;
            GameObject newObject = Instantiate(objectPrefab, hitPose.position, hitPose.rotation);
            
            // Add collider if it doesn't exist (needed for selection)
            if (newObject.GetComponent<Collider>() == null)
            {
                newObject.AddComponent<BoxCollider>();
            }
            
            spawnedObjects.Add(newObject);
            removeButton.interactable = true;
        }
    }

    void DeleteSelectedObject()
    {
        if (selectedObject != null)
        {
            spawnedObjects.Remove(selectedObject);
            originalMaterials.Remove(selectedObject); // Clean up material reference
            Destroy(selectedObject);
            selectedObject = null;
            
            deleteSelectedButton.interactable = false;
            
            if (spawnedObjects.Count == 0)
            {
                removeButton.interactable = false;
            }
        }
    }

    void RemoveAllObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
        originalMaterials.Clear(); // Clean up all material references
        selectedObject = null;
        
        removeButton.interactable = false;
        deleteSelectedButton.interactable = false;
    }
}