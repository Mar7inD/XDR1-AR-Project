using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

public class ObjectManipulator : MonoBehaviour
{
    [Header("Manipulation Settings")]
    [SerializeField] private float scaleSpeed = 0.0004f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float minScale = 0.001f;
    [SerializeField] private float maxScale = 5f;
    [SerializeField] private float pinchSensitivity = 0.1f;
    [SerializeField] private float minimumTouchPressure = 0.3f;

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Environment Manipulation")]
    [SerializeField] private bool manipulateObjectsWithEnvironment = true;

    private GameObject selectedObject;
    private Material[] originalMaterials;
    private Camera arCamera;
    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    // Input references
    private Touchscreen touchscreen;
    private Mouse mouse;
    private Keyboard keyboard;

    // Touch manipulation
    private float lastPinchDistance;
    private bool isPinching = false;

    // Gesture state tracking
    private bool wasThreeFingerRotating = false;
    private float gestureTransitionCooldown = 0f;
    private const float GESTURE_TRANSITION_DELAY = 0.3f; // 300ms delay

    // Drag state
    private bool isDragging = false;
    private Vector3 dragOffset;

    // Environment tracking for object manipulation
    private Vector3 lastEnvironmentPosition;
    private Quaternion lastEnvironmentRotation;
    private Vector3 lastEnvironmentScale;
    private bool isTrackingEnvironment = false;

    void Start()
    {
        arCamera = Camera.main;
        if (arCamera == null)
            arCamera = FindFirstObjectByType<Camera>();

        raycastManager = FindFirstObjectByType<ARRaycastManager>();

        // Get input devices
        touchscreen = Touchscreen.current;
        mouse = Mouse.current;
        keyboard = Keyboard.current;
    }

    void Update()
    {
        HandleInput();
        HandleKeyboardRotation();
        HandleMouseScroll();

        // Track environment changes
        TrackEnvironmentChanges();
        
        // Update gesture cooldowns
        if (gestureTransitionCooldown > 0f)
        {
            gestureTransitionCooldown -= Time.deltaTime;
        }
    }

    void TrackEnvironmentChanges()
    {
        if (!manipulateObjectsWithEnvironment) return;

        GameObject currentEnvironment = ARPlacementManager.Instance?.GetCurrentEnvironment();
        if (currentEnvironment == null)
        {
            isTrackingEnvironment = false;
            return;
        }

        // Initialize tracking if this is the first frame with an environment
        if (!isTrackingEnvironment)
        {
            lastEnvironmentPosition = currentEnvironment.transform.position;
            lastEnvironmentRotation = currentEnvironment.transform.rotation;
            lastEnvironmentScale = currentEnvironment.transform.localScale;
            isTrackingEnvironment = true;
            return;
        }

        // Check if environment has changed
        bool positionChanged = Vector3.Distance(currentEnvironment.transform.position, lastEnvironmentPosition) > 0.001f;
        bool rotationChanged = Quaternion.Angle(currentEnvironment.transform.rotation, lastEnvironmentRotation) > 0.1f;
        bool scaleChanged = Vector3.Distance(currentEnvironment.transform.localScale, lastEnvironmentScale) > 0.001f;

        if (positionChanged || rotationChanged || scaleChanged)
        {
            ApplyEnvironmentTransformationToObjects(currentEnvironment);

            // Update tracking values
            lastEnvironmentPosition = currentEnvironment.transform.position;
            lastEnvironmentRotation = currentEnvironment.transform.rotation;
            lastEnvironmentScale = currentEnvironment.transform.localScale;
        }
    }

    void ApplyEnvironmentTransformationToObjects(GameObject environment)
    {
        List<GameObject> spawnedObjects = ARPlacementManager.Instance?.GetSpawnedObjects();
        if (spawnedObjects == null || spawnedObjects.Count == 0) return;

        // Calculate transformation deltas
        Vector3 positionDelta = environment.transform.position - lastEnvironmentPosition;
        Quaternion rotationDelta = environment.transform.rotation * Quaternion.Inverse(lastEnvironmentRotation);
        Vector3 scaleRatio = new Vector3(
            environment.transform.localScale.x / lastEnvironmentScale.x,
            environment.transform.localScale.y / lastEnvironmentScale.y,
            environment.transform.localScale.z / lastEnvironmentScale.z
        );

        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null) continue;

            // Skip the selected object if it's currently being manipulated
            if (obj == selectedObject && (isDragging || isPinching)) continue;

            Transform objTransform = obj.transform;
            Vector3 environmentPosition = environment.transform.position;

            // Apply rotation around environment center
            if (rotationDelta != Quaternion.identity)
            {
                Vector3 relativePosition = objTransform.position - environmentPosition;
                Vector3 rotatedPosition = rotationDelta * relativePosition;
                objTransform.position = environmentPosition + rotatedPosition;
                objTransform.rotation = rotationDelta * objTransform.rotation;
            }

            // Apply position delta
            if (positionDelta != Vector3.zero)
            {
                objTransform.position += positionDelta;
            }

            // Apply scale
            if (scaleRatio != Vector3.one)
            {
                // Scale position relative to environment center
                Vector3 relativePosition = objTransform.position - environmentPosition;
                relativePosition.Scale(scaleRatio);
                objTransform.position = environmentPosition + relativePosition;

                // Scale the object itself
                Vector3 currentScale = objTransform.localScale;
                currentScale.Scale(scaleRatio);
                objTransform.localScale = currentScale;
            }
        }

        Debug.Log($"Applied environment transformation to {spawnedObjects.Count} objects");
    }

    void HandleInput()
    {
        // Skip input if placement manager is currently placing objects
        if (ARPlacementManager.Instance != null && ARPlacementManager.Instance.IsPlacing)
            return;

        // Handle touch input
        if (touchscreen != null)
        {
            var touches = touchscreen.touches;
            int activeTouchCount = GetActiveTouchCount(touches);

            if (activeTouchCount == 1)
            {
                // Single touch - selection and dragging
                var primaryTouch = GetPrimaryActiveTouch(touches);
                if (primaryTouch != null)
                {
                    Debug.Log("☝️ Single finger detected");
                    HandleSingleTouchInput(primaryTouch);
                }
                // Clear three-finger state when down to one finger
                wasThreeFingerRotating = false;
            }
            else if (activeTouchCount == 2 && selectedObject != null)
            {
                // Two touches - scaling only (but check for transition delay)
                if (gestureTransitionCooldown <= 0f && !wasThreeFingerRotating)
                {
                    var activeTouches = GetActiveTouches(touches);
                    if (activeTouches.Count >= 2)
                    {
                        HandlePinchGesture(activeTouches[0], activeTouches[1]);
                    }
                }
                else
                {
                    Debug.Log("🔄 Ignoring 2-finger gesture during transition cooldown");
                }
            }
            else if (activeTouchCount == 3 && selectedObject != null)
            {
                // Three touches - rotation only
                var activeTouches = GetActiveTouches(touches, 3);
                if (activeTouches.Count >= 3)
                {
                    if (!wasThreeFingerRotating)
                    {
                        wasThreeFingerRotating = true;
                        isPinching = false; // Stop any ongoing pinch
                        Debug.Log("🔄 Started 3-finger rotation");
                    }
                    HandleThreeFingerRotation(activeTouches[0], activeTouches[1], activeTouches[2]);
                }
            }
            else if (activeTouchCount >= 4)
            {
                // Four or more touches - deselect object
                DeselectObject();
                Debug.Log("🖐️ Four fingers detected - deselected object");
                wasThreeFingerRotating = false;
                gestureTransitionCooldown = 0f;
            }
            else if (activeTouchCount == 0)
            {
                // No touches - check if we were rotating and start cooldown
                if (wasThreeFingerRotating)
                {
                    Debug.Log("🔄 Ended 3-finger rotation - starting transition cooldown");
                    wasThreeFingerRotating = false;
                    gestureTransitionCooldown = GESTURE_TRANSITION_DELAY;
                    isPinching = false; // Ensure pinching is disabled
                }
            }
        }
        // Handle mouse input in editor
        else if (Application.isEditor && mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                if (!IsPointerOverUIElement(mousePos))
                {
                    HandleMouseBegan(mousePos);
                }
            }
            else
            {
                HandleMouseInput();
            }
        }
    }

    private int GetActiveTouchCount(UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.Controls.TouchControl> touches)
    {
        int count = 0;
        for (int i = 0; i < touches.Count; i++)
        {
            var phase = touches[i].phase.ReadValue();
            var pressure = touches[i].pressure.ReadValue();
            
            // Only count touches that are actively pressed or moving AND have sufficient pressure
            if ((phase == UnityEngine.InputSystem.TouchPhase.Began || 
                phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                phase == UnityEngine.InputSystem.TouchPhase.Stationary) &&
                pressure > minimumTouchPressure)
            {
                count++;
            }
        }
        return count;
    }

    private UnityEngine.InputSystem.Controls.TouchControl GetPrimaryActiveTouch(UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.Controls.TouchControl> touches)
    {
        for (int i = 0; i < touches.Count; i++)
        {
            var phase = touches[i].phase.ReadValue();
            var pressure = touches[i].pressure.ReadValue();
            
            // Only return touches that are actively pressed or moving AND have sufficient pressure
            if ((phase == UnityEngine.InputSystem.TouchPhase.Began || 
                phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                phase == UnityEngine.InputSystem.TouchPhase.Stationary) &&
                pressure > minimumTouchPressure)
            {
                return touches[i];
            }
        }
        return null;
    }

    private List<UnityEngine.InputSystem.Controls.TouchControl> GetActiveTouches(UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.Controls.TouchControl> touches, int maxTouches = 2)
    {
        List<UnityEngine.InputSystem.Controls.TouchControl> activeTouches = new List<UnityEngine.InputSystem.Controls.TouchControl>();
        for (int i = 0; i < touches.Count && activeTouches.Count < maxTouches; i++)
        {
            var phase = touches[i].phase.ReadValue();
            var pressure = touches[i].pressure.ReadValue();
            
            // Only add touches that are actively pressed or moving AND have sufficient pressure
            if ((phase == UnityEngine.InputSystem.TouchPhase.Began || 
                phase == UnityEngine.InputSystem.TouchPhase.Moved || 
                phase == UnityEngine.InputSystem.TouchPhase.Stationary) &&
                pressure > minimumTouchPressure) 
            {
                activeTouches.Add(touches[i]);
            }
        }
        return activeTouches;
    }

    private void HandleSingleTouchInput(UnityEngine.InputSystem.Controls.TouchControl touch)
    {
        Vector2 touchPos = touch.position.ReadValue();
        var phase = touch.phase.ReadValue();

        // Check if touch is over UI
        if (IsPointerOverUIElement(touchPos))
            return;

        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                HandleTouchBegan(touchPos);
                break;

            case UnityEngine.InputSystem.TouchPhase.Moved:
                if (isDragging && selectedObject != null)
                    UpdateDragging(touchPos);
                break;

            case UnityEngine.InputSystem.TouchPhase.Ended:
            case UnityEngine.InputSystem.TouchPhase.Canceled:
                EndDragging();
                break;
        }
    }

    private void HandleThreeFingerRotation(UnityEngine.InputSystem.Controls.TouchControl touch1, UnityEngine.InputSystem.Controls.TouchControl touch2, UnityEngine.InputSystem.Controls.TouchControl touch3)
    {
        if (selectedObject == null) return;

        var phase1 = touch1.phase.ReadValue();
        var phase2 = touch2.phase.ReadValue();
        var phase3 = touch3.phase.ReadValue();

        // Only rotate when fingers are moving
        if (phase1 == UnityEngine.InputSystem.TouchPhase.Moved || 
            phase2 == UnityEngine.InputSystem.TouchPhase.Moved || 
            phase3 == UnityEngine.InputSystem.TouchPhase.Moved)
        {
            Vector2 pos1 = touch1.position.ReadValue();
            Vector2 pos2 = touch2.position.ReadValue();
            Vector2 pos3 = touch3.position.ReadValue();

            Vector2 delta1 = touch1.delta.ReadValue();
            Vector2 delta2 = touch2.delta.ReadValue();
            Vector2 delta3 = touch3.delta.ReadValue();

            // Calculate the centroid (center point) of the three touches
            Vector2 centroid = (pos1 + pos2 + pos3) / 3f;

            // Calculate rotation based on how the touches move relative to the centroid
            float rotation = 0f;
            int validTouches = 0;

            // For each touch, calculate its contribution to rotation
            if (delta1.magnitude > 0.1f)
            {
                Vector2 relativePos = pos1 - centroid;
                Vector2 perpendicular = new Vector2(-relativePos.y, relativePos.x).normalized;
                rotation += Vector2.Dot(delta1, perpendicular);
                validTouches++;
            }

            if (delta2.magnitude > 0.1f)
            {
                Vector2 relativePos = pos2 - centroid;
                Vector2 perpendicular = new Vector2(-relativePos.y, relativePos.x).normalized;
                rotation += Vector2.Dot(delta2, perpendicular);
                validTouches++;
            }

            if (delta3.magnitude > 0.1f)
            {
                Vector2 relativePos = pos3 - centroid;
                Vector2 perpendicular = new Vector2(-relativePos.y, relativePos.x).normalized;
                rotation += Vector2.Dot(delta3, perpendicular);
                validTouches++;
            }

            // Average the rotation and apply it
            if (validTouches > 0)
            {
                float averageRotation = rotation / validTouches;
                float rotationSensitivity = 1.0f; // Adjust this value to control rotation speed
                selectedObject.transform.Rotate(0, averageRotation * rotationSensitivity, 0);
                
                Debug.Log($"🔄 Rotating with 3 fingers: {averageRotation * rotationSensitivity}");
            }
        }
    }

    void HandlePinchGesture(UnityEngine.InputSystem.Controls.TouchControl touch1, UnityEngine.InputSystem.Controls.TouchControl touch2)
    {
        if (selectedObject == null) return;

        // Additional safety check - don't start pinching if we just finished rotating
        if (gestureTransitionCooldown > 0f)
        {
            Debug.Log("🤏 Pinch blocked - transition cooldown active");
            return;
        }

        Vector2 pos1 = touch1.position.ReadValue();
        Vector2 pos2 = touch2.position.ReadValue();
        float currentDistance = Vector2.Distance(pos1, pos2);

        var phase1 = touch1.phase.ReadValue();
        var phase2 = touch2.phase.ReadValue();

        // Initialize pinch
        if (!isPinching)
        {
            if (phase1 == UnityEngine.InputSystem.TouchPhase.Began || phase2 == UnityEngine.InputSystem.TouchPhase.Began)
            {
                isPinching = true;
                lastPinchDistance = currentDistance;
                Debug.Log("🤏 Started scaling with 2 fingers");
                return;
            }
        }

        // Handle pinch scaling
        if (isPinching && (phase1 == UnityEngine.InputSystem.TouchPhase.Moved || phase2 == UnityEngine.InputSystem.TouchPhase.Moved))
        {
            float deltaDistance = currentDistance - lastPinchDistance;
            float scaleDelta = deltaDistance * pinchSensitivity * Time.deltaTime * 10f;
            ScaleSelectedObject(scaleDelta);
            lastPinchDistance = currentDistance;
            
            Debug.Log($"🤏 Scaling: {scaleDelta}");
        }

        // End pinching when touches end
        if (phase1 == UnityEngine.InputSystem.TouchPhase.Ended ||
            phase1 == UnityEngine.InputSystem.TouchPhase.Canceled ||
            phase2 == UnityEngine.InputSystem.TouchPhase.Ended ||
            phase2 == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            isPinching = false;
            Debug.Log("🤏 Ended scaling");
        }
    }

    private void HandleTouchBegan(Vector2 screenPos)
    {
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            GameObject rootObject = FindRootPrefabObject(hitObject);

            if (IsScalableObject(rootObject))
            {
                if (selectedObject == null)
                {
                    // No object selected, select this one
                    SelectObject(rootObject);
                }
                else if (rootObject == selectedObject)
                {
                    // Clicked on the selected object, start dragging
                    StartDragging(screenPos);
                }
                else
                {
                    // Clicked on a different object, select it instead
                    SelectObject(rootObject);
                }
            }
        }
    }

    private void HandleMouseBegan(Vector2 screenPos)
    {
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            GameObject rootObject = FindRootPrefabObject(hitObject);

            if (IsScalableObject(rootObject))
            {
                if (selectedObject == null)
                {
                    // No object selected, select this one
                    SelectObject(rootObject);
                }
                else if (rootObject == selectedObject)
                {
                    // Clicked on the selected object, start dragging
                    StartDragging(screenPos);
                }
                else
                {
                    // Clicked on a different object, select it instead
                    SelectObject(rootObject);
                }
            }
            else
            {
                // Clicked on non-scalable object, deselect
                DeselectObject();
            }
        }
        else
        {
            // Clicked on empty space, deselect
            DeselectObject();
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
        // Strategy 1: Check for PrefabIdentifier component (your custom component)
        if (obj.GetComponent<PrefabIdentifier>() != null)
        {
            return true;
        }

        // Strategy 2: Check for specific tags that indicate prefab roots
        if (obj.CompareTag("Environment"))
        {
            return true;
        }

        // Strategy 3: Check naming conventions
        if (obj.name.StartsWith("Prefab_") || obj.name.EndsWith("(Clone)"))
        {
            return true;
        }

        // Strategy 4: Check if object is at a reasonable hierarchy level
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
            obj.name.Contains("Camera") ||
            obj.name.Contains("plane") ||
            obj.name.Contains("trackable") ||
            obj.name.Contains("ar ") ||
            obj.name.Contains("xr ") ||
            obj.name.StartsWith("ar") ||
            obj.name.StartsWith("xr"))
        {
            return false;
        }

        // Check for AR components
        if (obj.GetComponent<ARPlane>() != null ||
            obj.GetComponent<ARFeatheredPlaneMeshVisualizerCompanion>() != null ||
            obj.GetComponent<ARPlaneMeshVisualizer>() != null ||
            obj.GetComponent<ARAnchor>() != null ||
            obj.GetComponent<ARPointCloud>() != null)
        {
            return false;
        }

        // Check PrefabIdentifier - ALLOW environments now
        PrefabIdentifier identifier = obj.GetComponent<PrefabIdentifier>();
        if (identifier != null)
        {
            // Allow both environment and non-environment prefabs
            return true;
        }

        // Check for specific scalable tags
        if (obj.CompareTag("Environment") ||
            obj.CompareTag("Scalable"))
        {
            return true;
        }

        // If no specific tags, allow objects that seem to be prefab instances
        if (obj.name.EndsWith("(Clone)"))
        {
            return true;
        }

        // Default to allowing scaling for most objects that have renderers
        return obj.GetComponent<Renderer>() != null || obj.GetComponentsInChildren<Renderer>().Length > 0;
    }

    void HandleMouseInput()
    {
        Vector2 mousePos = mouse.position.ReadValue();

        if (mouse.leftButton.isPressed && isDragging && selectedObject != null)
        {
            UpdateDragging(mousePos);
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndDragging();
        }

        // Right click to deselect
        if (mouse.rightButton.wasPressedThisFrame)
        {
            DeselectObject();
        }
    }

    void HandleMouseScroll()
    {
        if (selectedObject == null || mouse == null) return;

        Vector2 scroll = mouse.scroll.ReadValue();
        if (scroll.y != 0)
        {
            float scaleDelta = scroll.y * scaleSpeed;
            ScaleSelectedObject(scaleDelta);
        }
    }

    void HandleKeyboardRotation()
    {
        if (selectedObject == null || keyboard == null) return;

        float rotationInput = 0f;

        if (keyboard.eKey.isPressed)
            rotationInput = -1f;
        else if (keyboard.rKey.isPressed)
            rotationInput = 1f;

        if (rotationInput != 0f)
        {
            selectedObject.transform.Rotate(0, rotationInput * rotationSpeed * Time.deltaTime, 0);
        }
    }

    void SelectObject(GameObject obj)
    {
        if (selectedObject == obj) return;

        DeselectObject(); // Deselect previous object

        selectedObject = obj;
        AddSelectionOutline();

        // Reset environment tracking when selecting a new object
        // This prevents objects from jumping when we start manipulating them
        GameObject currentEnvironment = ARPlacementManager.Instance?.GetCurrentEnvironment();
        if (currentEnvironment != null)
        {
            lastEnvironmentPosition = currentEnvironment.transform.position;
            lastEnvironmentRotation = currentEnvironment.transform.rotation;
            lastEnvironmentScale = currentEnvironment.transform.localScale;
        }

        // Get additional info from PrefabIdentifier if available
        PrefabIdentifier identifier = obj.GetComponent<PrefabIdentifier>();
        string objectInfo = identifier != null ?
            $"{identifier.prefabName} (scalable: {identifier.isScalable})" :
            obj.name;

        Debug.Log($"Selected object: {objectInfo}");
    }

    void DeselectObject()
    {
        if (selectedObject == null) return;

        Debug.Log($"Deselecting object: {selectedObject.name}");
        
        RemoveSelectionOutline();
        
        // End any ongoing interactions
        isDragging = false;
        isPinching = false;
        dragOffset = Vector3.zero;
        lastPinchDistance = 0f;
        
        selectedObject = null;

        Debug.Log("Object deselected successfully");
    }

    void AddSelectionOutline()
    {
        if (selectedObject == null) return;

        Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"No renderers found on selected object: {selectedObject.name}");
            return;
        }

        // Store original materials properly
        List<Material> originals = new List<Material>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            // Store the ORIGINAL material (not the current one)
            Material originalMat = renderers[i].sharedMaterial;
            originals.Add(originalMat);

            // Create a tinted version for selection
            if (originalMat != null && originalMat.HasProperty("_Color"))
            {
                Material tintedMat = new Material(originalMat);
                Color originalColor = originalMat.color;
                Color tintedColor = Color.Lerp(originalColor, selectedColor, 0.5f);
                tintedMat.color = tintedColor;
                renderers[i].material = tintedMat;
            }
            else
            {
                // Fallback: create simple colored material
                Material outlineMat = new Material(Shader.Find("Unlit/Color"));
                outlineMat.color = selectedColor;
                renderers[i].material = outlineMat;
            }
        }

        // Store the original materials
        originalMaterials = originals.ToArray();

        Debug.Log($"Added selection outline to {selectedObject.name} with {renderers.Length} renderers");
    }

    void RemoveSelectionOutline()
    {
        if (selectedObject == null) return;

        Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>();

        // Restore original materials
        if (originalMaterials != null)
        {
            for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
            {
                if (renderers[i] != null && originalMaterials[i] != null)
                {
                    if (renderers[i].material != originalMaterials[i])
                    {
                        DestroyImmediate(renderers[i].material);
                    }
                    
                    renderers[i].sharedMaterial = originalMaterials[i];
                }
            }
        }

        // Clear the stored materials
        originalMaterials = null;

        Debug.Log($"Removed selection outline from {selectedObject?.name}");
    }

    void StartDragging(Vector2 screenPos)
    {
        if (selectedObject == null) return;

        isDragging = true;

        // Check if it's an environment object
        bool isEnvironment = false;
        PrefabIdentifier identifier = selectedObject.GetComponent<PrefabIdentifier>();
        if (identifier != null)
        {
            isEnvironment = identifier.isEnvironmentPrefab;
        }

        if (isEnvironment)
        {
            // For environments, calculate offset based on AR plane hit
            if (raycastManager != null && raycastManager.Raycast(screenPos, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                Vector3 hitPosition = raycastHits[0].pose.position;
                dragOffset = selectedObject.transform.position - hitPosition;
            }
            else
            {
                // If no AR plane hit, use default offset calculation
                Vector3 objectScreenPos = arCamera.WorldToScreenPoint(selectedObject.transform.position);
                Vector3 touchScreenPos = new Vector3(screenPos.x, screenPos.y, objectScreenPos.z);
                Vector3 touchWorldPos = arCamera.ScreenToWorldPoint(touchScreenPos);
                dragOffset = selectedObject.transform.position - touchWorldPos;
            }
        }
        else
        {
            // For regular objects, calculate offset based on land hit
            Ray ray = arCamera.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            
            bool foundLand = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Land"))
                {
                    dragOffset = selectedObject.transform.position - hit.point;
                    foundLand = true;
                    break;
                }
            }
            
            if (!foundLand)
            {
                // Fallback to screen-to-world calculation if no land found
                Vector3 objectScreenPos = arCamera.WorldToScreenPoint(selectedObject.transform.position);
                Vector3 touchWorldPos = arCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, objectScreenPos.z));
                dragOffset = selectedObject.transform.position - touchWorldPos;
            }
        }

        Debug.Log($"Started dragging {selectedObject.name} (Environment: {isEnvironment})");
    }

    void UpdateDragging(Vector2 screenPos)
    {
        if (!isDragging || selectedObject == null) return;

        // Check if the selected object is an environment
        bool isEnvironment = false;
        PrefabIdentifier identifier = selectedObject.GetComponent<PrefabIdentifier>();
        if (identifier != null)
        {
            isEnvironment = identifier.isEnvironmentPrefab;
        }

        if (isEnvironment)
        {
            // For environments, drag only on AR planes
            UpdateEnvironmentDragging(screenPos);
        }
        else
        {
            // For regular objects, drag only on objects with "Land" tag
            UpdateObjectDragging(screenPos);
        }
    }

    void UpdateEnvironmentDragging(Vector2 screenPos)
    {
        if (selectedObject == null || raycastManager == null) return;

        // Only allow dragging on AR planes for environments
        if (raycastManager.Raycast(screenPos, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 hitPosition = raycastHits[0].pose.position;
            Vector3 newPosition = hitPosition + dragOffset;
            
            selectedObject.transform.position = newPosition;
            
            Debug.Log($"Environment dragged to AR plane position: {newPosition}");
        }
        else
        {
            Debug.Log("Environment can only be dragged on AR planes");
        }
    }

    void UpdateObjectDragging(Vector2 screenPos)
    {
        if (selectedObject == null || arCamera == null) return;

        // Cast a ray from the camera through the screen position
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        
        // Raycast to find objects with "Land" tag
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Land"))
            {
                Vector3 hitPosition = hit.point;
                Vector3 newPosition = hitPosition + dragOffset;
                
                selectedObject.transform.position = newPosition;
                
                Debug.Log($"Object dragged to land position: {newPosition}");
                return;
            }
        }
        
        Debug.Log("Object can only be dragged on objects with 'Land' tag");
    }

    void EndDragging()
    {
        isDragging = false;
        dragOffset = Vector3.zero;
    }

    void ScaleSelectedObject(float scaleDelta)
    {
        if (selectedObject == null) return;

        // Check if scaling is allowed for this object
        PrefabIdentifier identifier = selectedObject.GetComponent<PrefabIdentifier>();
        if (identifier != null && !identifier.isScalable)
        {
            Debug.Log($"Scaling disabled for {selectedObject.name}");
            return;
        }

        Vector3 currentScale = selectedObject.transform.localScale;
        
        // Apply the scale delta to each axis
        Vector3 newScale = new Vector3(
            Mathf.Clamp(currentScale.x + scaleDelta, minScale, maxScale),
            Mathf.Clamp(currentScale.y + scaleDelta, minScale, maxScale),
            Mathf.Clamp(currentScale.z + scaleDelta, minScale, maxScale)
        );

        selectedObject.transform.localScale = newScale;
    }

    #region Public Methods for UI Events
    public void DeselectCurrentObject()
    {
        DeselectObject();
    }

    public GameObject GetSelectedObject()
    {
        return selectedObject;
    }

    public bool HasSelectedObject()
    {
        return selectedObject != null;
    }

    public void SetManipulateObjectsWithEnvironment(bool enabled)
    {
        manipulateObjectsWithEnvironment = enabled;
    }
    #endregion
}