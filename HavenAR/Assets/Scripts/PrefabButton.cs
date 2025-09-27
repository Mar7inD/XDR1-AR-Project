using UnityEngine;
using UnityEngine.UI;

public class PrefabButton : MonoBehaviour
{
    public GameObject prefabToSpawn;

    public void OnClick()
    {
        // Tell a manager which prefab was selected
        SelectorManager.Instance.StartDragging(prefabToSpawn);
    }
}
