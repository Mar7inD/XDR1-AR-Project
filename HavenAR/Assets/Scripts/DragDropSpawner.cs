using UnityEngine;
using UnityEngine.EventSystems;

public class DragDropController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject prefab; // assign in Inspector

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Tell SelectorManager to start dragging this prefab
        SelectorManager.Instance.StartDragging(prefab);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // While dragging, update preview position based on pointer
        SelectorManager.Instance.UpdateDragging(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Drop prefab in world
        SelectorManager.Instance.EndDragging(eventData.position);
    }
}
