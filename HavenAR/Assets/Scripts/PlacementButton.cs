using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlacementButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GameObject prefabToPlace;
    [Header("Drag Settings")]
    [SerializeField] private float dragThreshold = 10f; 
    
    private Button button;
    private Vector2 startPosition;
    private bool isDragging = false;
    private bool hasDraggedBeyondThreshold = false;
    
    void Start()
    {
        button = GetComponent<Button>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPosition = eventData.position;
        isDragging = false;
        hasDraggedBeyondThreshold = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Check if we've moved beyond the threshold
        float distance = Vector2.Distance(startPosition, eventData.position);
        
        if (distance > dragThreshold && !isDragging)
        {
            // Start dragging
            isDragging = true;
            hasDraggedBeyondThreshold = true;
            
            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.StartDragging(prefabToPlace);
            }
        }
        
        // Update drag position if we're dragging
        if (isDragging && ARPlacementManager.Instance != null)
        {
            ARPlacementManager.Instance.UpdateDragging(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging && ARPlacementManager.Instance != null && hasDraggedBeyondThreshold)
        {
            ARPlacementManager.Instance.EndDragging(eventData.position);
        }
        
        // Reset drag state
        isDragging = false;
        hasDraggedBeyondThreshold = false;

    }
}