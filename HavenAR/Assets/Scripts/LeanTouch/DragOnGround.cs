using UnityEngine;
using Lean.Touch;

public class DragOnGround : MonoBehaviour
{
    public float yHeight = 0.01f; // ground level for environments
    public LayerMask landLayer = -1; // Layer mask for land detection
    
    private LeanDragTranslate drag;
    private bool isEnvironmentObject = false;

    void Start()
    {
        drag = GetComponent<LeanDragTranslate>();
        
        // Check if this is an environment object
        PrefabIdentifier identifier = GetComponent<PrefabIdentifier>();
        if (identifier != null)
        {
            isEnvironmentObject = identifier.isEnvironmentPrefab;
        }
        else
        {
            // Fallback: check if object has Environment tag
            isEnvironmentObject = CompareTag("Environment");
        }
    }

    void LateUpdate()
    {
        if (drag != null)
        {
            if (isEnvironmentObject)
            {
                // Environment objects stay at ground level
                var pos = transform.position;
                pos.y = yHeight;
                transform.position = pos;
            }
            else
            {
                // Non-environment objects should stay on top of land
                StayOnTopOfLand();
            }
        }
    }

    void StayOnTopOfLand()
    {
        Vector3 currentPos = transform.position;

        // Cast a ray downwards from the object's current position to find land surface
        Ray rayDown = new Ray(currentPos, Vector3.down);

        // First try with layer mask
        if (landLayer != -1 && Physics.Raycast(rayDown, out RaycastHit hit, 50f, landLayer))
        {
            transform.position = new Vector3(currentPos.x, hit.point.y, currentPos.z);
        }
        // Then try with any collider and check for Land tag
        else if (Physics.Raycast(rayDown, out hit, 50f))
        {
            if (hit.collider.CompareTag("Land"))
            {
                transform.position = new Vector3(currentPos.x, hit.point.y, currentPos.z);
            }
        }
        // If no land found below, try casting upwards to check if we're inside/below land
        else
        {
            Ray rayUp = new Ray(currentPos, Vector3.up);

            if (landLayer != -1 && Physics.Raycast(rayUp, out hit, 50f, landLayer))
            {
                transform.position = new Vector3(currentPos.x, hit.point.y, currentPos.z);
            }
            else if (Physics.Raycast(rayUp, out hit, 50f))
            {
                if (hit.collider.CompareTag("Land"))
                {
                    transform.position = new Vector3(currentPos.x, hit.point.y, currentPos.z);
                }
            }
            else
            {
                // Fallback to ground level if no land found at all
                var pos = transform.position;
                pos.y = yHeight;
                transform.position = pos;
            }
        }
    }
}