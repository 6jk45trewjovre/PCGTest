using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BiomePreset
{
    public string presetName;
    public AnimationCurve curve;
    [Range(0f, 1f)] public float sandPercent = 0.3f;
    [Range(0f, 1f)] public float grassPercent = 0.5f;
    [Range(0f, 1f)] public float rockPercent = 0.2f;
}

[System.Serializable]
public class UiReferences
{
    public TMP_InputField seedInputField;
    public TMP_Dropdown biomeDropdown;
    public TMP_Dropdown ruleSelectDropdown;
    public TMP_Dropdown prefabDropdown;
    public Toggle randomSeedToggle;
    public Toggle ruleUnderwaterToggle;
    public Slider sandSlider;
    public Slider grassSlider;
    public Slider rockSlider;
    public Slider probSlider;
    public Slider minHeightSlider;
    public Slider maxHeightSlider;
    public Slider maxSteepnessSlider;
    public Button addRuleButton;
    public Button saveBiomeButton;
}

public class TerrainUIController : MonoBehaviour
{
    public ProceduralTerrain terrainGenerator;
    public Button generateButton;
    public List<BiomePreset> biomePresets = new List<BiomePreset>();
    public UiReferences ui;
    public GameObject[] availablePrefabs;

    private int currentRuleIndex = 0;
    private float[] matValues = new float[3] { 0.3f, 0.5f, 0.2f };
    private bool isUpdatingSliders = false;

    void Start()
    {
        ui.randomSeedToggle.isOn = true;
        ui.seedInputField.interactable = false;
        ui.randomSeedToggle.onValueChanged.AddListener((isOn) => { ui.seedInputField.interactable = !isOn; });

        ui.prefabDropdown.onValueChanged.AddListener((val) => SaveRuleFromUI());
        ui.probSlider.onValueChanged.AddListener((val) => SaveRuleFromUI());
        ui.minHeightSlider.onValueChanged.AddListener((val) => SaveRuleFromUI());
        ui.maxHeightSlider.onValueChanged.AddListener((val) => SaveRuleFromUI());
        ui.maxSteepnessSlider.onValueChanged.AddListener((val) => SaveRuleFromUI());
        ui.sandSlider.onValueChanged.AddListener((val) => OnSliderChanged(0, val));
        ui.grassSlider.onValueChanged.AddListener((val) => OnSliderChanged(1, val));
        ui.rockSlider.onValueChanged.AddListener((val) => OnSliderChanged(2, val));

        PopulatePrefabDropdown();
        RefreshRuleDropdown();
        PopulateBiomeDropdown();
        ui.ruleSelectDropdown.onValueChanged.AddListener(LoadRuleIntoUI);
        ui.ruleUnderwaterToggle.onValueChanged.AddListener((val) => SaveRuleFromUI());
        ui.biomeDropdown.onValueChanged.AddListener(LoadBiomeIntoUI);
        ui.addRuleButton.onClick.AddListener(CreateNewRule);
        ui.saveBiomeButton.onClick.AddListener(SaveCurrentAsBiome);

        generateButton.onClick.AddListener(OnGenerateClicked);
        if (biomePresets.Count > 0) LoadBiomeIntoUI(0);
    }
    void PopulatePrefabDropdown()
    {
        ui.prefabDropdown.ClearOptions();
        ui.prefabDropdown.AddOptions(availablePrefabs.Select(go => go.name).ToList());
    }
    void PopulateBiomeDropdown()
    {
        ui.biomeDropdown.ClearOptions();
        ui.biomeDropdown.AddOptions(biomePresets.Select(p => p.presetName).ToList());
    }
    void RefreshRuleDropdown()
    {
        ui.ruleSelectDropdown.ClearOptions();

        ui.ruleSelectDropdown.AddOptions(terrainGenerator.scatterRules.Select(r => r.ruleName).ToList());

        LoadRuleIntoUI(0);
    }
    void OnSliderChanged(int changedIndex, float newValue)
    {
        if (isUpdatingSliders) return;

        isUpdatingSliders = true;

        float oldValue = matValues[changedIndex];
        float delta = newValue - oldValue;

        float sumOthers = 0f;
        for (int i = 0; i < 3; i++) if (i != changedIndex) sumOthers += matValues[i];

        matValues[changedIndex] = newValue;

        for (int i = 0; i < 3; i++)
        {
            if (i != changedIndex)
            {
                if (sumOthers > 0.001f)
                    matValues[i] -= delta * (matValues[i] / sumOthers);
                else
                    matValues[i] -= delta / 2f;

                matValues[i] = Mathf.Clamp01(matValues[i]);
            }
        }

        float total = matValues[0] + matValues[1] + matValues[2];
        if (total > 0)
        {
            for (int i = 0; i < 3; i++) matValues[i] /= total;
        }

        ui.sandSlider.value = matValues[0];
        ui.grassSlider.value = matValues[1];
        ui.rockSlider.value = matValues[2];

        isUpdatingSliders = false;
    }
    void SaveRuleFromUI()
    {
        if (isUpdatingSliders || terrainGenerator.scatterRules.Length == 0) return;

        ScatterRule rule = terrainGenerator.scatterRules[currentRuleIndex];
        rule.allowUnderwater = ui.ruleUnderwaterToggle.isOn;
        rule.spawnProbability = ui.probSlider.value;
        rule.minHeight = ui.minHeightSlider.value;
        rule.maxHeight = ui.maxHeightSlider.value;
        rule.maxSteepness = ui.maxSteepnessSlider.value;
        rule.prefab = availablePrefabs[ui.prefabDropdown.value];
    }
    void LoadRuleIntoUI(int index)
    {
        if (terrainGenerator.scatterRules.Length == 0) return;

        currentRuleIndex = index;
        ScatterRule rule = terrainGenerator.scatterRules[index];

        ui.ruleUnderwaterToggle.isOn = rule.allowUnderwater;
        ui.probSlider.SetValueWithoutNotify(rule.spawnProbability);
        ui.minHeightSlider.SetValueWithoutNotify(rule.minHeight);
        ui.maxHeightSlider.SetValueWithoutNotify(rule.maxHeight);
        ui.maxSteepnessSlider.SetValueWithoutNotify(rule.maxSteepness);

        int prefabIndex = Array.IndexOf(availablePrefabs, rule.prefab);
        if (prefabIndex >= 0)
        {
            ui.prefabDropdown.SetValueWithoutNotify(prefabIndex);
        }
    }

    void LoadBiomeIntoUI(int index)
    {
        if (index < 0 || index >= biomePresets.Count) return;

        BiomePreset activeBiome = biomePresets[index];

        matValues[0] = activeBiome.sandPercent;
        matValues[1] = activeBiome.grassPercent;
        matValues[2] = activeBiome.rockPercent;

        ui.sandSlider.SetValueWithoutNotify(matValues[0]);
        ui.grassSlider.SetValueWithoutNotify(matValues[1]);
        ui.rockSlider.SetValueWithoutNotify(matValues[2]);
    }
    void CreateNewRule()
    {
        List<ScatterRule> currentRules = new List<ScatterRule>(terrainGenerator.scatterRules);
        ScatterRule newRule = new ScatterRule();
        newRule.ruleName = "Rule " + (currentRules.Count + 1);
        if (availablePrefabs.Length > 0) newRule.prefab = availablePrefabs[0];

        currentRules.Add(newRule);
        terrainGenerator.scatterRules = currentRules.ToArray();

        RefreshRuleDropdown();
        ui.ruleSelectDropdown.value = currentRules.Count - 1;
    }
    void SaveCurrentAsBiome()
    {
        BiomePreset newPreset = new BiomePreset();
        newPreset.presetName = "Custom Biome " + (biomePresets.Count + 1);
        newPreset.sandPercent = matValues[0];
        newPreset.grassPercent = matValues[1];
        newPreset.rockPercent = matValues[2];

        newPreset.curve = biomePresets[ui.biomeDropdown.value].curve;

        biomePresets.Add(newPreset);
        PopulateBiomeDropdown();
        ui.biomeDropdown.value = biomePresets.Count - 1;
    }
    public void OnGenerateClicked()
    {
        if (ui.randomSeedToggle.isOn)
        {
            terrainGenerator.seed = UnityEngine.Random.Range(-99999, 99999);
            ui.seedInputField.text = terrainGenerator.seed.ToString();
        }
        else
        {
            if (int.TryParse(ui.seedInputField.text, out int customSeed)) terrainGenerator.seed = customSeed;
            else terrainGenerator.seed = 0;
        }

        if (biomePresets.Count > 0)
        {
            terrainGenerator.heightCurve = biomePresets[ui.biomeDropdown.value].curve;
        }

        Gradient dynamicGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[6];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };

        float endSand = matValues[0];
        float endGrass = matValues[0] + matValues[1];

        colorKeys[0] = new GradientColorKey(Color.red, 0f);
        colorKeys[1] = new GradientColorKey(Color.red, endSand);
        colorKeys[2] = new GradientColorKey(Color.green, endSand + 0.001f);
        colorKeys[3] = new GradientColorKey(Color.green, endGrass);
        colorKeys[4] = new GradientColorKey(Color.blue, endGrass + 0.001f);
        colorKeys[5] = new GradientColorKey(Color.blue, 1f);

        dynamicGradient.SetKeys(colorKeys, alphaKeys);
        terrainGenerator.heightColors = dynamicGradient;

        terrainGenerator.GenerateTerrain();
    }
}