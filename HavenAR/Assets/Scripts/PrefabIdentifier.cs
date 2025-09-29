using UnityEngine;

public class PrefabIdentifier : MonoBehaviour
{
    [Header("Prefab Info")]
    public string prefabName;
    public bool isScalable = true;
    public bool isEnvironmentPrefab = false;
    
    void Start()
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = gameObject.name.Replace("(Clone)", "");
        }
    }
}