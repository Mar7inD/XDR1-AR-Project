using UnityEngine;
using UnityEngine.UI;

public class PanelToggle : MonoBehaviour
{
    [Header("Panel Settings")]
    public GameObject panel;
        
    [Header("Button Color Change")]
    public Color buttonClickedColor = new Color(0f, 0.52f, 1f, 1f); 
    
    private bool isVisible = false;
    private Button button;
    private Color buttonDefaultColor;

    void Start()
    {
        button = GetComponent<Button>();
        buttonDefaultColor = button.colors.normalColor;
        
        // Ensure the button click is set up
        if (button != null)
        {
            button.onClick.AddListener(TogglePanel);
        }
        
        // Set initial state
        if (panel != null)
        {
            isVisible = panel.activeInHierarchy;
            UpdateButtonColor();
        }
    }

    public void TogglePanel()
    {
        if (panel == null)
        {
            Debug.LogWarning("Panel is not assigned in PanelToggle script!");
            return;
        }
        
        isVisible = !isVisible;
        panel.SetActive(isVisible);
        UpdateButtonColor();
        
        Debug.Log($"Panel {panel.name} is now {(isVisible ? "visible" : "hidden")}");
    }
    
    void UpdateButtonColor()
    {
        if (buttonClickedColor != null)
        {
            buttonDefaultColor = isVisible ? buttonDefaultColor : buttonClickedColor;
        }
    }
    
    // Public methods for manual control
    public void ShowPanel()
    {
        if (panel != null)
        {
            isVisible = true;
            panel.SetActive(isVisible);
            UpdateButtonColor();
        }
    }
    
    public void HidePanel()
    {
        if (panel != null)
        {
            isVisible = false;
            panel.SetActive(isVisible);
            UpdateButtonColor();
        }
    }
}