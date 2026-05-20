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
    private float _lastValue;

    void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.onValueChanged.AddListener(UpdateText);
    }
    void Start()
    {
        _lastValue = _slider.value;
        UpdateText(_lastValue);
    }

    void LateUpdate()
    {
        if (!Mathf.Approximately(_slider.value, _lastValue))
        {
            _lastValue = _slider.value;
            UpdateText(_lastValue);
        }
    }

    private void UpdateText(float value)
    {
        if (targetText != null)
        {
            targetText.text = $"{prefix}: {value.ToString(numberFormat)}";
        }
    }
}
