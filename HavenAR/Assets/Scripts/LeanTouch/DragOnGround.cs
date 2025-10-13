using UnityEngine;
using Lean.Touch;
using UnityEngine.XR.ARFoundation;

public class DragOnGround : MonoBehaviour
{
    public float yOffset = 0.0005f;
    public LayerMask landLayer = -1;
    public bool alwaysStayOnLand = true;
    public float minHeightAboveLand = 0.0001f; // Minimum distance to consider "above land"
    public float maxHeightAboveLand = 0.001f; // Maximum allowed height above land before pulling down
    
    private LeanDragTranslate drag;
    private bool isEnvironmentObject = false;
    private ARPlaneManager planeManager;
    private Vector3 lastPosition;
    private bool hasMovedThisFrame = false;

    void Start()
    {
        drag = GetComponent<LeanDragTranslate>();
        planeManager = FindFirstObjectByType<ARPlaneManager>();
        lastPosition = transform.position;
        
        PrefabIdentifier identifier = GetComponent<PrefabIdentifier>();
        if (identifier != null)
        {
            isEnvironmentObject = identifier.isEnvironmentPrefab;
        }
        else
        {
            isEnvironmentObject = CompareTag("Environment");
        }

        if (!isEnvironmentObject)
        {
            StayOnTopOfLand();
        }
    }

    void Update()
    {
        hasMovedThisFrame = drag.isActiveAndEnabled;
        
        if (isEnvironmentObject)
        {
            var pos = transform.position;
            pos.y = GetPlaneHeight() + yOffset;
            transform.position = pos;
            hasMovedThisFrame = true;

        }
        else if (alwaysStayOnLand)
        {
            // Always check if properly positioned, regardless of movement
            if (!IsProperlyPositionedAboveLand())
            {
                StayOnTopOfLand();
                hasMovedThisFrame = true;
            }
            // Also check for position changes
            else if (Vector3.Distance(transform.position, lastPosition) > 0.001f)
            {
                StayOnTopOfLand();
                lastPosition = transform.position;
                hasMovedThisFrame = true;
            }
        }
    }

    // // This is for final checking
    // void LateUpdate()
    // {
    //     if (!isEnvironmentObject && alwaysStayOnLand && !hasMovedThisFrame)
    //     {
    //         if (!IsProperlyPositionedAboveLand())
    //         {
    //             StayOnTopOfLand();
    //         }
    //     }
    // }

    bool IsProperlyPositionedAboveLand()
    {
        Vector3 currentPos = transform.position;
        Ray rayDown = new Ray(currentPos, Vector3.down);
        
        // Increase raycast distance to handle high objects
        if (landLayer != -1 && Physics.Raycast(rayDown, out RaycastHit hit, 100f, landLayer))
        {
            float heightAboveLand = currentPos.y - hit.point.y;
            return heightAboveLand >= minHeightAboveLand && heightAboveLand <= maxHeightAboveLand;
        }
        
        // Check with Land tag - also increase distance
        if (Physics.Raycast(rayDown, out hit, 100f))
        {
            if (hit.collider.CompareTag("Land"))
            {
                float heightAboveLand = currentPos.y - hit.point.y;
                return heightAboveLand >= minHeightAboveLand && heightAboveLand <= maxHeightAboveLand;
            }
        }
        
        // If no land found below, the object is definitely not properly positioned
        return false;
    }

    float GetPlaneHeight()
    {
        if (planeManager != null && planeManager.trackables.count > 0)
        {
            // Find the closest plane to this object's XZ position
            float closestDistance = float.MaxValue;
            float closestPlaneY = yOffset;
            
            foreach (ARPlane plane in planeManager.trackables)
            {
                Vector3 planePos = plane.transform.position;
                float distance = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(planePos.x, planePos.z)
                );
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlaneY = planePos.y;
                }
            }
            
            return closestPlaneY;
        }
        
        // Fallback to small offset if no planes detected
        return yOffset;
    }
    
    void StayOnTopOfLand()
    {
        Vector3 currentPos = transform.position;
        Ray rayDown = new Ray(currentPos + Vector3.up * 0.1f, Vector3.down); // Start slightly above

        if (landLayer != -1 && Physics.Raycast(rayDown, out RaycastHit hit, 51f, landLayer))
        {
            transform.position = new Vector3(currentPos.x, hit.point.y + yOffset, currentPos.z);
        }
        else if (Physics.Raycast(rayDown, out hit, 51f))
        {
            if (hit.collider.CompareTag("Land"))
            {
                transform.position = new Vector3(currentPos.x, hit.point.y + yOffset, currentPos.z);
            }
        }
        else
        {
            // Try upward ray as fallback
            Ray rayUp = new Ray(currentPos, Vector3.up);
            if (landLayer != -1 && Physics.Raycast(rayUp, out hit, 50f, landLayer))
            {
                transform.position = new Vector3(currentPos.x, hit.point.y + yOffset, currentPos.z);
            }
            else if (Physics.Raycast(rayUp, out hit, 50f) && hit.collider.CompareTag("Land"))
            {
                transform.position = new Vector3(currentPos.x, hit.point.y + yOffset, currentPos.z);
            }
            else
            {
                var pos = transform.position;
                pos.y = GetPlaneHeight() + yOffset;
                transform.position = pos;
            }
        }
    }
}