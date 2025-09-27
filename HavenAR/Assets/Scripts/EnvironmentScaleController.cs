using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnvironmentScaleController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider scaleSlider;
    [SerializeField] private Button resetButton;
    [SerializeField] private GameObject scalePanel;
    [SerializeField] private TextMeshProUGUI scaleValueText;

    [Header("Scale Settings")]
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 5.0f;

    private Vector3 spawnScale = Vector3.one; // The actual spawn size
    private float spawnScaleValue; // Single float representing the spawn scale

    void Start()
    {
        // Setup slider
        if (scaleSlider != null)
        {
            scaleSlider.minValue = minScale;
            scaleSlider.maxValue = maxScale;
            scaleSlider.value = 1.0f;
            scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        }

        // Setup reset button
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetScale);
        }

        // Hide panel initially
        if (scalePanel != null)
        {
            scalePanel.SetActive(false);
        }

        UpdateScaleText();
    }

    void Update()
    {
        if (!scalePanel.activeSelf)
        {
            // Update scale text in real-time if panel is visible
            scaleSlider.value = spawnScaleValue;
            UpdateScaleText();
        }
        // Show/hide scale panel based on whether there's an environment
        if (scalePanel != null)
        {
            bool hasEnvironment = SelectorManager.Instance != null && SelectorManager.Instance.HasActiveEnvironment();
            scalePanel.SetActive(hasEnvironment);
        }
    }

    private void OnScaleChanged(float value)
    {
        if (SelectorManager.Instance != null)
        {
            GameObject currentEnv = SelectorManager.Instance.GetCurrentEnvironment();
            if (currentEnv != null)
            {
                // Apply the slider value directly as uniform scale
                currentEnv.transform.localScale = Vector3.one * value;
                UpdateScaleText();
            }
        }
    }

    private void ResetScale()
    {
        if (scaleSlider != null)
        {
            // Reset to the original spawn scale value
            scaleSlider.value = spawnScaleValue;
        }
    }

    private void UpdateScaleText()
    {
        if (scaleValueText != null && scaleSlider != null)
        {
            scaleValueText.text = $"Scale: {scaleSlider.value:F3}";
        }
    }

    // Call this when a new environment is spawned
    public void OnEnvironmentSpawned(GameObject environment)
    {
        if (environment != null)
        {
            // Capture the actual spawn scale
            spawnScale = environment.transform.localScale;

            // Get the scale value (assuming uniform scaling, use x component)
            spawnScaleValue = spawnScale.x;

            // Set slider to match the actual spawn scale
            if (scaleSlider != null)
            {
                // Make sure the slider range can accommodate this scale
                if (spawnScaleValue < scaleSlider.minValue)
                {
                    scaleSlider.minValue = spawnScaleValue * 0.5f; // Allow scaling down further
                }
                if (spawnScaleValue > scaleSlider.maxValue)
                {
                    scaleSlider.maxValue = spawnScaleValue * 2f; // Allow scaling up further
                }

                scaleSlider.value = spawnScaleValue;
            }

            UpdateScaleText();

            Debug.Log($"Captured spawn scale: {spawnScale} (value: {spawnScaleValue}) for environment: {environment.name}");
        }
    }

    // Method to get the current scale value
    public float GetCurrentScaleValue()
    {
        return scaleSlider != null ? scaleSlider.value : 1.0f;
    }

    // Method to get the spawn scale value
    public float GetSpawnScaleValue()
    {
        return spawnScaleValue;
    }

    // Method to set a specific scale value
    public void SetScaleValue(float scaleValue)
    {
        if (scaleSlider != null)
        {
            scaleSlider.value = Mathf.Clamp(scaleValue, scaleSlider.minValue, scaleSlider.maxValue);
        }
    }
    
    public void OnEnvironmentRemoved()
    {
        // Reset to default values
        spawnScale = Vector3.one;
        spawnScaleValue = 1.0f;
        
        if (scaleSlider != null)
        {
            scaleSlider.value = 1.0f;
        }
        
        UpdateScaleText();
        Debug.Log("Environment removed - scale controller reset");
    }
}