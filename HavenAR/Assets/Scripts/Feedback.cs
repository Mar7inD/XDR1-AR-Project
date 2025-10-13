using Lean.Common;
using Lean.Touch;
using UnityEngine;

public class SelectFeedback : MonoBehaviour
{
    private LeanSelectable selectable;

    void Awake()
    {
        selectable = GetComponent<LeanSelectable>();
        selectable.OnSelected.AddListener(OnSelect);
        selectable.OnDeselected.AddListener(OnDeselect);
    }

    void OnSelect(LeanSelect select)
    {
        Debug.Log($"{gameObject.name} selected");
    }

    void OnDeselect(LeanSelect select)
    {
        Debug.Log($"{gameObject.name} deselected");
    }
}