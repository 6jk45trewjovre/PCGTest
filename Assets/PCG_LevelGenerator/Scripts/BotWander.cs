using UnityEngine;
using UnityEngine.AI;

public class BotWander : MonoBehaviour
{
    public float wanderRadius = 30f;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        InvokeRepeating("SetNewDestination", 1f, 5f);
    }

    void SetNewDestination()
    {
        Vector2 randomDir = Random.insideUnitCircle * wanderRadius;
        Vector3 randomTarget = transform.position + new Vector3(randomDir.x, 0, randomDir.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomTarget, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}