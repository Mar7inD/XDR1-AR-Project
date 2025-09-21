using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TouchController : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    
    [Header("Spawn Settings")]
    public GameObject objectToSpawn;
    
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private bool spawned = false;

    // Start is called before the first frame update
    void Start()
    {
        // Auto-find AR components if not assigned
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
        
        if (arCamera == null)
            arCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        // Handle touch input using new Input System
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            Debug.Log("Touch detected at: " + touchPosition);
            
            // Perform AR raycast
            PerformARRaycast(touchPosition);
        }
        
        // Handle mouse input for testing in Unity editor using new Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Debug.Log("Mouse click detected at: " + mousePos);
            
            // Perform AR raycast (or regular raycast if not in AR)
            PerformARRaycast(mousePos);
        }
    }
    
    private void PerformARRaycast(Vector2 screenPosition)
    {
        // Check if we have the necessary AR components
        if (raycastManager == null)
        {
            Debug.LogWarning("ARRaycastManager not found! Performing regular raycast instead.");
            PerformRegularRaycast(screenPosition);
            return;
        }
        
        // Perform AR raycast to detect planes
        if (raycastManager.Raycast(screenPosition, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            // Get the first hit point
            ARRaycastHit hit = raycastHits[0];
            
            Debug.Log("AR Plane hit at position: " + hit.pose.position);
            
            // Spawn object at hit position
            SpawnObject(hit.pose.position, hit.pose.rotation);
        }
        else
        {
            Debug.Log("No AR plane detected at touch position");
        }
    }
    
    private void PerformRegularRaycast(Vector2 screenPosition)
    {
        // Create a ray from camera through screen position
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        
        // Perform raycast
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Regular raycast hit: " + hit.collider.name + " at position: " + hit.point);
            
            // Check if hit object has a specific tag or component (e.g., "Plane")
            if (hit.collider.CompareTag("Plane") || hit.collider.name.ToLower().Contains("plane"))
            {
                SpawnObject(hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            Debug.Log("No object hit by raycast");
        }
    }

    private void SpawnObject(Vector3 position, Quaternion rotation)
    {
        if (!spawned)
        {
            spawned = true; // Add this line to prevent multiple spawns
            
            if (objectToSpawn != null)
            {
                // Offset the position slightly above the plane to ensure visibility
                Vector3 spawnPosition = position + Vector3.up * 0.01f;

                GameObject spawnedObject = Instantiate(objectToSpawn, spawnPosition, rotation);

                // Add some debug info
                Debug.Log("Object spawned at: " + spawnPosition);
                Debug.Log("Object scale after setting: " + spawnedObject.transform.localScale);
                Debug.Log("Object name: " + spawnedObject.name);

                // Check if the object has renderers
                Renderer[] renderers = spawnedObject.GetComponentsInChildren<Renderer>();
                Debug.Log("Number of renderers found: " + renderers.Length);

                if (renderers.Length == 0)
                {
                    Debug.LogWarning("No renderers found on spawned object! Object might not be visible.");
                }
                else
                {
                    // Log renderer info
                    foreach (Renderer renderer in renderers)
                    {
                        Debug.Log("Renderer: " + renderer.name + ", Enabled: " + renderer.enabled);
                        if (renderer.material != null)
                        {
                            Debug.Log("Material: " + renderer.material.name);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("No object to spawn assigned! Please assign an object in the inspector.");

                // Create a simple cube as fallback
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = position + Vector3.up * 0.05f;
                cube.transform.rotation = rotation;
                cube.transform.localScale = Vector3.one * 0.1f; // Small cube
                cube.name = "SpawnedCube";

                // Make it bright red for visibility
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                cubeRenderer.material.color = Color.red;

                Debug.Log("Created fallback cube at: " + position);
            }
        }
        else
        {
            Debug.Log("Object has already been spawned. Only one instance allowed.");
        }
    }
}
