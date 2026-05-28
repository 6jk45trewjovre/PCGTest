using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class ScatterRule
{
    public string ruleName;
    public GameObject prefab;
    public bool allowUnderwater;
    public bool alignToSurfaceNormal;
    [Range(0f, 1f)] public float spawnProbability = 0.05f;
    public float minHeight = 0f;
    public float maxHeight = 50f;
    public float maxSteepness = 30f;
}

[System.Serializable]
public class BotSettings
{
    public bool spawnBots = true;
    public bool canWalkUnderwater = false;
    public int maxBots = 10;
    [Range(0f, 1f)] public float spawnChance = 0.05f;
    public float speed = 3.5f;
    public float angularSpeed = 120f;
    public float acceleration = 8f;
    public float avoidanceRadius = 0.5f;
    public float wanderRadius = 30f;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralTerrain : MonoBehaviour
{
    public int width = 200;
    public int length = 200;
    public bool generateIslands = true;
    public bool generateWater = true;
    public Transform waterPlane;
    public float seaLevel = -0.2f;
    public float islandDistance = 50f;
    [Range(0f, 1f)] public float islandSize = 0.5f;
    [Range(0f, 1f)] public float archipelagoClustering = 0.5f;
    public float maxOceanDepth = 10f;
    public Gradient heightColors;
    private Color[] colors; 
    public float noiseScale = 10f;
    public float altitudeMultiplier = 10f;
    public AnimationCurve heightCurve;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public GameObject botPrefab;
    private NavMeshSurface navMeshSurface;
    public BotSettings botSettings;
    private Transform botParent;
    public int seed = 42;
    public Vector2 offset;
    public ScatterRule[] scatterRules;
    private Transform scatterParent; 
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "Procedural Terrain";

        GenerateTerrain();
    }
    public void GenerateTerrain()
    {
        int vertexCount = (width + 1) * (length + 1);
        if (vertices == null || vertices.Length != vertexCount)
        {
            vertices = new Vector3[vertexCount];
            uvs = new Vector2[vertexCount];
            colors = new Color[vertexCount];
        }

        vertices = new Vector3[(width + 1) * (length + 1)];
        uvs = new Vector2[(width + 1) * (length + 1)]; 
        colors = new Color[(width + 1) * (length + 1)]; 

        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000) + offset.x;
        float offsetZ = prng.Next(-100000, 100000) + offset.y;

        int i = 0;
        float invWidth = 1f / width;
        float invLength = 1f / length;
        for (int z = 0; z <= length; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float islandX = (x + offsetX) / islandDistance;
                float islandZ = (z + offsetZ) / islandDistance;
                float baseContinent = GetNoise(islandX, islandZ);
                float archipelagoNoise = GetNoise(islandX * 2.5f, islandZ * 2.5f);
                float islandMask = Mathf.Lerp(baseContinent, baseContinent * archipelagoNoise * 1.5f, archipelagoClustering);
                float shoreDistance = islandMask - (1f - islandSize);
                float finalHeight = 0f;

                if (generateIslands)
                {
                    if (shoreDistance > 0f)
                    {
                        float sampleX = (x + offsetX) / noiseScale;
                        float sampleZ = (z + offsetZ) / noiseScale;
                        float rawNoise = GetFractalNoise(sampleX, sampleZ, octaves, persistence, lacunarity);
                        float terrainHeight = heightCurve.Evaluate(rawNoise) * altitudeMultiplier;
                        float beachEase = Mathf.Clamp01(shoreDistance * 4f);
                        finalHeight = terrainHeight * beachEase;
                    }
                    else
                    {
                        finalHeight = Mathf.Max(shoreDistance * maxOceanDepth * 2f, -maxOceanDepth);
                    }
                }
                else
                {

                    float sampleX = (x + offsetX) / noiseScale;
                    float sampleZ = (z + offsetZ) / noiseScale;
                    float rawNoise = GetFractalNoise(sampleX, sampleZ, octaves, persistence, lacunarity);
                    finalHeight = heightCurve.Evaluate(rawNoise) * altitudeMultiplier;
                }

                vertices[i] = new Vector3(x, finalHeight, z);
                uvs[i] = new Vector2(x * invWidth, z * invLength);
                float heightPercentage;

                if (generateWater && finalHeight <= seaLevel)
                {
                    heightPercentage = 0f;
                }
                else
                {
                    float minHeight = generateWater ? seaLevel : 0f;
                    float validMaxHeight = Mathf.Max(seaLevel + 0.1f, altitudeMultiplier);
                    heightPercentage = Mathf.InverseLerp(minHeight, validMaxHeight, finalHeight);
                }

                colors[i] = heightColors.Evaluate(heightPercentage);
                i++;
            }
        }
        int targetTrisCount = width * length * 6;
        if (triangles == null || triangles.Length != targetTrisCount)
        {
            triangles = new int[targetTrisCount];
            GenerateTriangles();
        }

        UpdateMesh();
        UpdateWater();
        if (Application.isPlaying)
        {
            ScatterObjects();
            BuildAIAndSpawn();
        }
    }
    private void GenerateTriangles()
    {
        int vert = 0; 
        int tris = 0; 

        for (int z = 0; z < length; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }
    private float GetNoise(float x, float z)
    {
        return Mathf.PerlinNoise(x, z);
    }
    private float GetFractalNoise(float x, float z, int octaves, float persistence, float lacunarity)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += GetNoise(x * frequency, z * frequency) * amplitude;
            maxAmplitude += amplitude;
            frequency *= lacunarity;
            amplitude *= persistence;
        }

        return total / maxAmplitude;
    }
    private void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        MeshCollider col = GetComponent<MeshCollider>();
        if (col == null) col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();  
    }
    private struct PreparedScatterRule
    {
        public ScatterRule rule;
        public float cosMaxSteepness;
    }
    private void ScatterObjects()
    {
        if (scatterParent != null)
        {
            Destroy(scatterParent.gameObject);
        }
        GameObject parentObj = new GameObject("Scattered Objects");
        parentObj.transform.parent = this.transform;
        scatterParent = parentObj.transform;
        Vector3[] normals = mesh.normals;
        System.Random prng = new System.Random(seed + 1);
        int ruleCount = scatterRules.Length;
        PreparedScatterRule[] preparedRules = new PreparedScatterRule[ruleCount];
        for (int r = 0; r < ruleCount; r++)
        {
            preparedRules[r] = new PreparedScatterRule
            {
                rule = scatterRules[r],
                cosMaxSteepness = Mathf.Cos(scatterRules[r].maxSteepness * Mathf.Deg2Rad)
            };
        }
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexPos = vertices[i];
            Vector3 normal = normals[i];
            float normalY = normal.y;
            for (int r = 0; r < ruleCount; r++)
            {
                PreparedScatterRule prep = preparedRules[r];
                ScatterRule rule = prep.rule;

                if (generateWater && !rule.allowUnderwater && vertexPos.y <= seaLevel)
                {
                    continue;
                }

                if (vertexPos.y < rule.minHeight || vertexPos.y > rule.maxHeight)
                {
                    continue;
                }

                if (normalY < prep.cosMaxSteepness)
                {
                    continue;
                }

                if (prng.NextDouble() < rule.spawnProbability)
                {
                    float offsetX = (float)(prng.NextDouble() * 0.8 - 0.4);
                    float offsetZ = (float)(prng.NextDouble() * 0.8 - 0.4);
                    Vector3 spawnPos = new Vector3(vertexPos.x + offsetX, vertexPos.y, vertexPos.z + offsetZ);
                    GameObject spawnedObj = Instantiate(rule.prefab, spawnPos, Quaternion.identity, scatterParent);
                    Quaternion randomYaw = Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);

                    if (rule.alignToSurfaceNormal)
                    {
                        Quaternion slopeAlignment = Quaternion.FromToRotation(Vector3.up, normal);
                        spawnedObj.transform.rotation = slopeAlignment * randomYaw;
                    }
                    else
                    {
                        spawnedObj.transform.rotation = randomYaw;
                    }
                    break;
                }
            }
        }
    }
    private void BuildAIAndSpawn()
    {
        if (botParent != null) Destroy(botParent.gameObject);
        if (!botSettings.spawnBots || botPrefab == null) return;

        botParent = new GameObject("AI Bots").transform;
        botParent.transform.parent = this.transform;

        GameObject deepWaterBlocker = null;
        if (generateWater && !botSettings.canWalkUnderwater)
        {
            deepWaterBlocker = new GameObject("DeepWaterBlocker");
            deepWaterBlocker.transform.position = transform.TransformPoint(new Vector3(width / 2f, seaLevel - 51f, length / 2f));
            BoxCollider box = deepWaterBlocker.AddComponent<BoxCollider>();
            box.size = Vector3.Scale(new Vector3(width + 50f, 100f, length + 50f), transform.lossyScale);

            NavMeshModifier mod = deepWaterBlocker.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1;
        }

        if (navMeshSurface == null) navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navMeshSurface.BuildNavMesh();

        if (deepWaterBlocker != null) DestroyImmediate(deepWaterBlocker);

        System.Random prng = new System.Random(seed + 2);
        System.Collections.Generic.List<Vector3> validSpawnPoints = new System.Collections.Generic.List<Vector3>(vertices.Length);

        for (int j = 0; j < vertices.Length; j++)
        {
            Vector3 vertexPos = vertices[j];
            if (generateWater && !botSettings.canWalkUnderwater && vertexPos.y < (seaLevel - 1f))
            {
                continue;
            }
            validSpawnPoints.Add(vertexPos);
        }

        if (validSpawnPoints.Count == 0)
        {
            if (botSettings.canWalkUnderwater || !generateWater)
            {
                validSpawnPoints.AddRange(vertices);
            }
        }

        int targetBotCount = 0;
        for (int i = 0; i < botSettings.maxBots; i++)
        {
            if (prng.NextDouble() < botSettings.spawnChance)
            {
                targetBotCount++;
            }
        }

        int spawnedBots = 0;
        while (spawnedBots < targetBotCount && validSpawnPoints.Count > 0)
        {
            int lastIndex = validSpawnPoints.Count - 1;
            int randomIndex = prng.Next(0, validSpawnPoints.Count);
            Vector3 localTestPos = validSpawnPoints[randomIndex];
            validSpawnPoints[randomIndex] = validSpawnPoints[lastIndex];
            validSpawnPoints.RemoveAt(lastIndex);
            Vector3 worldTestPos = transform.TransformPoint(localTestPos);

            if (NavMesh.SamplePosition(worldTestPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                GameObject spawnedBot = Instantiate(botPrefab, hit.position, Quaternion.identity, botParent);

                if (spawnedBot.TryGetComponent<NavMeshAgent>(out var agent))
                {
                    agent.speed = botSettings.speed;
                    agent.angularSpeed = botSettings.angularSpeed;
                    agent.acceleration = botSettings.acceleration;
                    agent.radius = botSettings.avoidanceRadius;
                }

                if (spawnedBot.TryGetComponent<BotWander>(out var wanderData))
                {
                    wanderData.wanderRadius = botSettings.wanderRadius;
                }

                spawnedBots++;
            }
        }
    }
    private void UpdateWater()
    {
        if (waterPlane == null) return;

        if (generateWater)
        {
            waterPlane.gameObject.SetActive(true);
            waterPlane.localScale = new Vector3(width / 10f, 1f, length / 10f);
            waterPlane.position = new Vector3(width / 2f, seaLevel, length / 2f);
        }
        else
        {
            waterPlane.gameObject.SetActive(false);
        }
    }
}