using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlacementButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GameObject prefabToPlace;
    [Header("Drag Settings")]
    [SerializeField] private float dragThreshold = 10f;
    [Header("Button Settings")]
    [SerializeField] private bool isObjectButton = false; // Set this to true for object buttons in inspector
    [Header("Visual Settings")]
    [SerializeField] private float disabledAlpha = 0.5f; // How transparent when disabled
    [SerializeField] private Color disabledTint = Color.gray; // Tint color when disabled
    
    private Button button;
    private Image buttonImage;
    private Vector2 startPosition;
    private bool isDragging = false;
    private bool hasDraggedBeyondThreshold = false;
    private bool hasBeenUsed = false;
    
    // Store original visual state
    private Color originalColor;
    private float originalAlpha;
    
    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
        // Store original appearance
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
            originalAlpha = originalColor.a;
        }
        
        // Register this button with the ARPlacementManager if it's an object button
        if (isObjectButton && ARPlacementManager.Instance != null)
        {
            ARPlacementManager.Instance.RegisterObjectButton(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Don't allow dragging if button is disabled
        if (button != null && !button.interactable)
            return;
            
        startPosition = eventData.position;
        isDragging = false;
        hasDraggedBeyondThreshold = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Don't allow dragging if button is disabled
        if (button != null && !button.interactable)
            return;
            
        // Check if we've moved beyond the threshold
        float distance = Vector2.Distance(startPosition, eventData.position);
        
        if (distance > dragThreshold && !isDragging)
        {
            // Start dragging
            isDragging = true;
            hasDraggedBeyondThreshold = true;
            
            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.StartDragging(prefabToPlace, this);
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
    
    // Method to disable this specific button after use
    public void MarkAsUsed()
    {
        hasBeenUsed = true;
        if (button != null)
        {
            button.interactable = false;
        }
        
        // Update visual appearance
        UpdateVisualState();
    }
    
    // Method to reset this button (re-enable it)
    public void ResetButton()
    {
        hasBeenUsed = false;
        if (button != null)
        {
            button.interactable = true;
        }
        
        // Restore original appearance
        UpdateVisualState();
    }
    
    private void UpdateVisualState()
    {
        if (buttonImage == null) return;
        
        if (hasBeenUsed)
        {
            // Apply disabled appearance
            Color disabledColor = disabledTint;
            disabledColor.a = disabledAlpha;
            buttonImage.color = disabledColor;
        }
        else
        {
            // Restore original appearance
            buttonImage.color = originalColor;
        }
    }
    
    public bool IsObjectButton()
    {
        return isObjectButton;
    }
    
    public bool HasBeenUsed()
    {
        return hasBeenUsed;
    }
    
    public GameObject GetPrefab()
    {
        return prefabToPlace;
    }
}