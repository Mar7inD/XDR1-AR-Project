using UnityEngine;
using UnityEngine.UI;

public class DeleteButton : MonoBehaviour
{
    [Header("Delete Options")]
    [SerializeField] public bool deleteObjects = true;
    [SerializeField] public bool deleteEnvironment = true;
    [SerializeField] public bool showConfirmation = false;
    
    private Button button;
    
    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnDeleteButtonClicked);
        }
    }
    
    void OnDeleteButtonClicked()
    {
        if (showConfirmation)
        {
            Debug.Log("Delete button clicked - add confirmation dialog");
        }
        
        PerformDelete();
    }
    
    void PerformDelete()
    {
        if (ARPlacementManager.Instance == null)
        {
            Debug.LogWarning("ARPlacementManager not found!");
            return;
        }
        
        if (deleteObjects && deleteEnvironment)
        {
            ARPlacementManager.Instance.DeleteEverything();
        }
        else if (deleteObjects)
        {
            ARPlacementManager.Instance.DeleteAllObjects();
        }
        else if (deleteEnvironment)
        {
            ARPlacementManager.Instance.DeleteEnvironment();
        }
    }
    
    // Public methods for UI events
    public void DeleteAll()
    {
        if (ARPlacementManager.Instance != null)
            ARPlacementManager.Instance.DeleteEverything();
    }
    
    public void DeleteObjectsOnly()
    {
        if (ARPlacementManager.Instance != null)
            ARPlacementManager.Instance.DeleteAllObjects();
    }
    
    public void DeleteEnvironmentOnly()
    {
        if (ARPlacementManager.Instance != null)
            ARPlacementManager.Instance.DeleteEnvironment();
    }
}