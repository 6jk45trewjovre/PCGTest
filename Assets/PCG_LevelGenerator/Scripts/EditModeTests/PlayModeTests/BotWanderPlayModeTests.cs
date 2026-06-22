using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AI;

public class BotWanderPlayModeTests
{
    private GameObject botGO;
    private BotWander botWander;
    private GameObject floor;

    [SetUp]
    public void Setup()
    {
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.localScale = new Vector3(10, 1, 10);
        var surface = floor.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        surface.BuildNavMesh();

        botGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        botGO.SetActive(false);

        botGO.transform.position = Vector3.zero;
        botGO.AddComponent<NavMeshAgent>();
        botWander = botGO.AddComponent<BotWander>();
        botWander.wanderRadius = 10f;

        botGO.SetActive(true);
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(floor);
        Object.Destroy(botGO);
    }

    [UnityTest]
    public IEnumerator BotWander_DestinationIsWithinWanderRadius()
    {
        NavMeshAgent agent = botGO.GetComponent<NavMeshAgent>();
        Vector3 startPos = botGO.transform.position;

        yield return new WaitForSeconds(1.5f);

        float distance = Vector3.Distance(startPos, agent.destination);
        Assert.LessOrEqual(distance, botWander.wanderRadius + 1f);
    }
}