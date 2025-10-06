using UnityEngine;
using Lean.Touch;

public class DragOnGround : MonoBehaviour
{
    public float yHeight = 0f; // ground level
    private LeanDragTranslate drag;

    void Start()
    {
        drag = GetComponent<LeanDragTranslate>();
    }

    void LateUpdate()
    {
        if (drag != null)
        {
            var pos = transform.position;
            pos.y = yHeight;
            transform.position = pos;
        }
    }
}