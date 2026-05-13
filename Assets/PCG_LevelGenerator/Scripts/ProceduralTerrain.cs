using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class ScatterRule
{
    public string ruleName;
    public GameObject prefab;
    public bool allowUnderwater;
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
    public int width = 50;
    public int depth = 50;
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
    public float heightMultiplier = 10f;
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
        vertices = new Vector3[(width + 1) * (depth + 1)];
        uvs = new Vector2[(width + 1) * (depth + 1)]; 
        colors = new Color[(width + 1) * (depth + 1)]; 


        System.Random prng = new System.Random(seed);
        float offsetX = prng.Next(-100000, 100000) + offset.x;
        float offsetZ = prng.Next(-100000, 100000) + offset.y;

        int i = 0;
        for (int z = 0; z <= depth; z++)
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
                        float terrainHeight = heightCurve.Evaluate(rawNoise) * heightMultiplier;
                        float beachEase = Mathf.Clamp01(shoreDistance * 4f);
                        finalHeight = terrainHeight * beachEase;
                    }
                    else
                    {

                        finalHeight = shoreDistance * maxOceanDepth * 2f;


                        finalHeight = Mathf.Max(finalHeight, -maxOceanDepth);
                    }
                }
                else
                {

                    float sampleX = (x + offsetX) / noiseScale;
                    float sampleZ = (z + offsetZ) / noiseScale;
                    float rawNoise = GetFractalNoise(sampleX, sampleZ, octaves, persistence, lacunarity);
                    finalHeight = heightCurve.Evaluate(rawNoise) * heightMultiplier;
                }

                vertices[i] = new Vector3(x, finalHeight, z);
                uvs[i] = new Vector2((float)x / width, (float)z / depth);

                float heightPercentage;


                if (finalHeight <= seaLevel)
                {
                    heightPercentage = 0f;
                }
                else
                {

                    float validMaxHeight = Mathf.Max(seaLevel + 0.1f, heightMultiplier);
                    heightPercentage = Mathf.InverseLerp(seaLevel, validMaxHeight, finalHeight);
                }

                colors[i] = heightColors.Evaluate(heightPercentage);

                i++;
            }
        }

        GenerateTriangles();
        UpdateMesh();
        GenerateWater();
        if (Application.isPlaying)
        {
            ScatterObjects();
            BuildAIAndSpawn();
        }
    }
    private void GenerateTriangles()
    {
        triangles = new int[width * depth * 6];

        int vert = 0; 
        int tris = 0; 

        for (int z = 0; z < depth; z++)
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
    void OnValidate()
    {
        if (width < 1) width = 1;
        if (depth < 1) depth = 1;
        if (noiseScale <= 0f) noiseScale = 0.001f;
        if (mesh != null)
        {
            GenerateTerrain();
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

        if (generateWater == true)
        {
            waterPlane.gameObject.SetActive(true);
            if (waterPlane != null)
            {
                waterPlane.localScale = new Vector3(width / 10f, 1f, depth / 10f);
                waterPlane.position = new Vector3(width / 2f, seaLevel, depth / 2f);
            }
        }
        else
        {
            waterPlane.gameObject.SetActive(false);
        }
        
    }
    private void ScatterObjects()
    {
        if (scatterParent == null)
        {
            GameObject parentObj = new GameObject("Scattered Objects");
            parentObj.transform.parent = this.transform;
            scatterParent = parentObj.transform;
        }
        else
        {
            foreach (Transform child in scatterParent)
            {
                Destroy(child.gameObject);
            }
        }
        System.Random prng = new System.Random(seed + 1);
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexPos = vertices[i];
            float steepness = Vector3.Angle(Vector3.up, normals[i]);
            foreach (ScatterRule rule in scatterRules)
            {

                if (!rule.allowUnderwater && vertexPos.y <= seaLevel)
                {
                    continue;
                }

                if (vertexPos.y < rule.minHeight || vertexPos.y > rule.maxHeight || steepness > rule.maxSteepness)
                {
                    continue;
                }

                if (prng.NextDouble() < rule.spawnProbability)
                {
                    float offsetX = (float)(prng.NextDouble() * 0.8 - 0.4);
                    float offsetZ = (float)(prng.NextDouble() * 0.8 - 0.4);
                    Vector3 spawnPos = new Vector3(vertexPos.x + offsetX, vertexPos.y, vertexPos.z + offsetZ);
                    GameObject spawnedObj = Instantiate(rule.prefab, spawnPos, Quaternion.identity);
                    spawnedObj.transform.parent = scatterParent;
                    spawnedObj.transform.rotation = Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);
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
        if (!botSettings.canWalkUnderwater)
        {
            deepWaterBlocker = new GameObject("DeepWaterBlocker");
            deepWaterBlocker.transform.position = new Vector3(width / 2f, seaLevel - 51f, depth / 2f);
            BoxCollider box = deepWaterBlocker.AddComponent<BoxCollider>();
            box.size = new Vector3(width + 50f, 100f, depth + 50f);
            NavMeshModifier mod = deepWaterBlocker.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1;
        }
        if (navMeshSurface == null) navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navMeshSurface.BuildNavMesh();
        if (deepWaterBlocker != null) Destroy(deepWaterBlocker);

        System.Random prng = new System.Random(seed + 2);
        int currentSpawns = 0;

        for (int i = 0; i < botSettings.maxBots; i++)
        {
            if (prng.NextDouble() < botSettings.spawnChance)
            {
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    int randomIndex = prng.Next(0, vertices.Length);
                    Vector3 testPos = vertices[randomIndex];
                    if (!botSettings.canWalkUnderwater && testPos.y < seaLevel - 1f)
                        continue;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(testPos, out hit, 2.0f, NavMesh.AllAreas))
                    {
                        GameObject spawnedBot = Instantiate(botPrefab, hit.position, Quaternion.identity, botParent);

                        UnityEngine.AI.NavMeshAgent agent = spawnedBot.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (agent != null)
                        {
                            agent.speed = botSettings.speed;
                            agent.angularSpeed = botSettings.angularSpeed;
                            agent.acceleration = botSettings.acceleration;
                            agent.radius = botSettings.avoidanceRadius;
                        }

                        BotWander wanderData = spawnedBot.GetComponent<BotWander>();
                        if (wanderData != null) wanderData.wanderRadius = botSettings.wanderRadius;

                        currentSpawns++;
                        break;
                    }
                }
            }
        }
    }
    private void GenerateWater()
    {
        waterPlane.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);
        waterPlane.transform.position = new Vector3(width / 2f, seaLevel, depth / 2f);
    }
}