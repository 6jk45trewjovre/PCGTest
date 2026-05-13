using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderTextUpdater : MonoBehaviour
{
    public TMPro.TextMeshProUGUI targetText;
    public string prefix = "Value";
    public string numberFormat = "F2";
    private Slider _slider;

    void Awake()
    {
        _slider = GetComponent<Slider>();

        _slider.onValueChanged.AddListener(UpdateText);
    }
    void Start()
    {
        UpdateText(_slider.value);
    }

    private void UpdateText(float value)
    {
        if (targetText != null)
        {
            targetText.text = $"{prefix}: {value.ToString(numberFormat)}";
        }
    }
}
