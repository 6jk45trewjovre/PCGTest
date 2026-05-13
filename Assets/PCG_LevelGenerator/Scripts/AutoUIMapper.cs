using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class AutoUIMapper : MonoBehaviour
{
    public string variableName;

    private ProceduralTerrain targetScript;

    private object _targetObject;
    private FieldInfo _targetField;

    void Start()
    {
        targetScript = FindAnyObjectByType<ProceduralTerrain>();

        if (targetScript == null)
        {
            Debug.LogError($"В сцене не найден объект со скриптом ProceduralTerrain!");
            return;
        }

        if (string.IsNullOrEmpty(variableName))
        {
            Debug.LogError($"Не вписано имя переменной на объекте {gameObject.name}");
            return;
        }

        if (!FindTargetField()) return;

        SetupSlider();
        SetupToggle();
    }

    private bool FindTargetField()
    {
        string[] paths = variableName.Split('.');
        _targetObject = targetScript;

        for (int i = 0; i < paths.Length - 1; i++)
        {
            FieldInfo info = _targetObject.GetType().GetField(paths[i], BindingFlags.Public | BindingFlags.Instance);
            if (info == null)
            {
                Debug.LogError($"Не найдено поле '{paths[i]}' в {variableName}");
                return false;
            }

            _targetObject = info.GetValue(_targetObject);

            if (_targetObject == null)
            {
                Debug.LogError($"Экземпляр '{paths[i]}' равен null!");
                return false;
            }
        }

        string finalFieldName = paths[paths.Length - 1];
        _targetField = _targetObject.GetType().GetField(finalFieldName, BindingFlags.Public | BindingFlags.Instance);

        if (_targetField == null)
        {
            Debug.LogError($"Финальная переменная '{finalFieldName}' не найдена!");
            return false;
        }

        return true;
    }

    private void SetupSlider()
    {
        Slider slider = GetComponent<Slider>();
        if (slider != null)
        {
            float startValue = Convert.ToSingle(_targetField.GetValue(_targetObject));
            slider.value = startValue;

            slider.onValueChanged.AddListener(val =>
            {
                if (_targetField.FieldType == typeof(int))
                    _targetField.SetValue(_targetObject, (int)val);
                else
                    _targetField.SetValue(_targetObject, val);
            });
        }
    }

    private void SetupToggle()
    {
        Toggle toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            bool startValue = (bool)_targetField.GetValue(_targetObject);
            toggle.isOn = startValue;

            toggle.onValueChanged.AddListener(val =>
            {
                _targetField.SetValue(_targetObject, val);
            });
        }
    }
}