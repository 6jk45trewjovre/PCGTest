using NUnit.Framework;
using UnityEngine;

public class ProceduralTerrainEditModeTests
{
    private GameObject terrainGO;
    private ProceduralTerrain terrain;

    [SetUp]
    public void Setup()
    {
        terrainGO = new GameObject("EditMode Terrain");
        terrain = terrainGO.AddComponent<ProceduralTerrain>();
        terrain.width = 10;
        terrain.length = 10;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(terrainGO);
        if (terrain.waterPlane != null) Object.DestroyImmediate(terrain.waterPlane.gameObject);
    }

    [Test]
    public void GetFractalNoise_ReturnsValuesNormalizedWithinRange()
    {
        for (float x = -100f; x <= 100f; x += 35.5f)
        {
            for (float z = -100f; z <= 100f; z += 35.5f)
            {
                float val = terrain.GetFractalNoise(x, z, terrain.octaves, terrain.persistence, terrain.lacunarity);
                Assert.GreaterOrEqual(val, 0f, $"Шум на координатах {x}, {z} меньше нуля");
                Assert.LessOrEqual(val, 1f, $"Шум на координатах {x}, {z} больше единицы");
            }
        }
    }


    [Test]
    public void Triangles_AreGeneratedWithClockwiseWinding()
    {
        terrain.GenerateTerrain();
        Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        Assert.Greater(triangles.Length, 0, "Индексы треугольников пустые");

        Vector3 v0 = vertices[triangles[0]];
        Vector3 v1 = vertices[triangles[1]];
        Vector3 v2 = vertices[triangles[2]];

        Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

        Assert.Greater(normal.y, 0f, "Направление обхода вершин (winding order) неверное. Нормали смотрят вниз.");
    }

    [Test]
    public void UV_Coordinates_AreMappedCorrectlyToGridCorners()
    {
        terrain.GenerateTerrain();
        Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
        Vector2[] uvs = mesh.uv;

        int vertCount = mesh.vertexCount;

        Assert.AreEqual(new Vector2(0f, 0f), uvs[0]);
        Assert.AreEqual(new Vector2(1f, 0f), uvs[terrain.width]);
        Assert.AreEqual(new Vector2(0f, 1f), uvs[vertCount - 1 - terrain.width]);
        Assert.AreEqual(new Vector2(1f, 1f), uvs[vertCount - 1]);
    }

    [Test]
    public void GetFractalNoise_ExtremeCoordinates_DoesNotReturnNaNOrInfinity()
    {
        float extremeX = 9999999f;
        float extremeZ = -9999999f;

        float result = terrain.GetFractalNoise(extremeX, extremeZ, terrain.octaves, terrain.persistence, terrain.lacunarity);

        Assert.IsFalse(float.IsNaN(result), "Математика шума вернула NaN на больших координатах");
        Assert.IsFalse(float.IsInfinity(result), "Математика шума вернула Infinity на больших координатах");
        Assert.IsTrue(result >= 0f && result <= 1f, "Значение вышло за рамки нормализованного диапазона [0, 1]");
    }
}