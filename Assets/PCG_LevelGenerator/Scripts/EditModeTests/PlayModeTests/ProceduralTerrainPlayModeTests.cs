using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AI;

public class ProceduralTerrainPlayModeTests
{
    private GameObject terrainGO;
    private ProceduralTerrain terrain;
    private GameObject botPrefab;
    private GameObject scatterPrefab;

    [SetUp]
    public void Setup()
    {
        terrainGO = new GameObject("PlayMode Terrain");
        terrainGO.SetActive(false);

        terrain = terrainGO.AddComponent<ProceduralTerrain>();
        terrain.width = 10;
        terrain.length = 10;

        terrain.heightColors = new Gradient();
        terrain.heightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        terrain.waterPlane = new GameObject("Water Plane").transform;
        terrain.botSettings = new BotSettings();

        botPrefab = new GameObject("Test Bot Template");
        botPrefab.SetActive(false);
        botPrefab.AddComponent<NavMeshAgent>();
        botPrefab.AddComponent<BotWander>();
        terrain.botPrefab = botPrefab;

        scatterPrefab = new GameObject("Test Object Template");
        scatterPrefab.SetActive(false);
    }

    [TearDown]
    public void Teardown()
    {
        if (terrainGO != null) Object.DestroyImmediate(terrainGO);
        if (botPrefab != null) Object.DestroyImmediate(botPrefab);
        if (scatterPrefab != null) Object.DestroyImmediate(scatterPrefab);
        if (terrain.waterPlane != null) Object.DestroyImmediate(terrain.waterPlane.gameObject);
    }

    [UnityTest]
    public IEnumerator ScatterObjects_RespectsSteepnessLimits()
    {
        terrain.scatterRules = new ScatterRule[]
        {
            new ScatterRule
            {
                ruleName = "FlatOnly",
                prefab = scatterPrefab,
                spawnProbability = 1f,
                minHeight = -100f,
                maxHeight = 100f,
                maxSteepness = 1f,
                allowUnderwater = true
            }
        };
        terrain.altitudeMultiplier = 50f;

        terrainGO.SetActive(true);
        terrain.GenerateTerrain();

        MethodInfo scatterMethod = typeof(ProceduralTerrain).GetMethod("ScatterObjects", BindingFlags.NonPublic | BindingFlags.Instance);
        scatterMethod.Invoke(terrain, null);

        yield return null;

        Transform scatterParent = terrainGO.transform.Find("Scattered Objects");
        Assert.IsNotNull(scatterParent);

        foreach (Transform child in scatterParent)
        {
            Ray ray = new Ray(child.position + Vector3.up * 2f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                Assert.LessOrEqual(angle, 5f, $"Объект заспавнился на слишком крутом склоне: {angle} градусов.");
            }
        }
    }

    [UnityTest]
    public IEnumerator BuildAIAndSpawn_RespectsWaterDepthThreshold()
    {
        terrain.generateWater = true;
        terrain.seaLevel = 10f;

        terrain.botSettings = new BotSettings
        {
            spawnBots = true,
            canWalkUnderwater = false,
            maxBots = 10,
            spawnChance = 1f
        };

        terrain.altitudeMultiplier = 5f;
        terrain.GenerateTerrain();

        MethodInfo buildAIMethod = typeof(ProceduralTerrain).GetMethod("BuildAIAndSpawn", BindingFlags.NonPublic | BindingFlags.Instance);
        buildAIMethod.Invoke(terrain, null);

        yield return null;

        Transform botsParent = terrainGO.transform.Find("AI Bots");
        if (botsParent != null)
        {
            foreach (Transform bot in botsParent)
            {
                float allowedDepthLimit = terrain.seaLevel - 1f;
                Assert.GreaterOrEqual(bot.position.y, allowedDepthLimit,
                    $"Бот заспавнился слишком глубоко под водой: Y={bot.position.y}, порог={allowedDepthLimit}");
            }
        }
    }
}