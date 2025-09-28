using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleController : MonoBehaviour
{
    [Header("Parent Reference")]
    [SerializeField] private Transform parentToWatch;
    [SerializeField] private bool scaleRecursively = false;
    
    private Vector3 lastParentScale;
    
    void Start()
    {
        // Auto-assign parent if not set
        if (parentToWatch == null)
        {
            parentToWatch = transform.parent;
        }
        
        if (parentToWatch != null)
        {
            lastParentScale = parentToWatch.localScale;
        }
    }
    
    void Update()
    {
        // Watch parent scale changes
        if (parentToWatch != null)
        {
            if (parentToWatch.localScale != lastParentScale && IsValidScale(parentToWatch.localScale))
            {
                ApplyParentScale();
                lastParentScale = parentToWatch.localScale;
            }
        }
    }
    
    void ApplyParentScale()
    {
        if (parentToWatch == null) return;
        
        Vector3 parentScale = SafeScale(parentToWatch.localScale);
        
        // Apply parent scale to this object
        transform.localScale = parentScale;
        
        // Apply to children
        if (scaleRecursively)
        {
            ScaleChildrenRecursively(transform, parentScale);
        }
        else
        {
            ScaleDirectChildren(parentScale);
        }
    }
    
    void ScaleDirectChildren(Vector3 scale)
    {
        foreach (Transform child in transform)
        {
            child.localScale = SafeScale(scale);
        }
    }
    
    void ScaleChildrenRecursively(Transform parent, Vector3 scale)
    {
        foreach (Transform child in parent)
        {
            child.localScale = SafeScale(scale);
            ScaleChildrenRecursively(child, scale);
        }
    }
    
    bool IsValidScale(Vector3 scale)
    {
        return !float.IsInfinity(scale.x) && !float.IsInfinity(scale.y) && !float.IsInfinity(scale.z) &&
               !float.IsNaN(scale.x) && !float.IsNaN(scale.y) && !float.IsNaN(scale.z) &&
               scale.x != 0f && scale.y != 0f && scale.z != 0f;
    }
    
    Vector3 SafeScale(Vector3 scale)
    {
        // Ensure scale values are within reasonable bounds
        scale.x = Mathf.Clamp(scale.x, 0.001f, 1000f);
        scale.y = Mathf.Clamp(scale.y, 0.001f, 1000f);
        scale.z = Mathf.Clamp(scale.z, 0.001f, 1000f);
        
        // Replace any invalid values with 1
        if (float.IsInfinity(scale.x) || float.IsNaN(scale.x)) scale.x = 1f;
        if (float.IsInfinity(scale.y) || float.IsNaN(scale.y)) scale.y = 1f;
        if (float.IsInfinity(scale.z) || float.IsNaN(scale.z)) scale.z = 1f;
        
        return scale;
    }
    
    // Public methods
    public void SetParentToWatch(Transform parent)
    {
        parentToWatch = parent;
        if (parent != null)
        {
            lastParentScale = parent.localScale;
        }
    }
    
    public void SetScaleRecursively(bool enable)
    {
        scaleRecursively = enable;
    }
    
    public void ForceApplyParentScale()
    {
        if (parentToWatch != null && IsValidScale(parentToWatch.localScale))
        {
            ApplyParentScale();
        }
    }
}