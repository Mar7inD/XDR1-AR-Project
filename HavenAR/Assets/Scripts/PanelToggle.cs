using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject panel; // assign your panel in inspector

    private bool isVisible = false;

    public void TogglePanel()
    {
        isVisible = !isVisible;
        panel.SetActive(isVisible);
    }
}