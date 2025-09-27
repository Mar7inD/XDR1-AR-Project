using UnityEngine;

public class PrefabButton : MonoBehaviour
{
    public GameObject prefabToSpawn;

    public void OnClick()
    {
        // Tell a manager which prefab was selected
        SelectorManager.Instance.StartDragging(prefabToSpawn);
    }
}
