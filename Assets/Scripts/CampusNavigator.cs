using UnityEngine;
using UnityEngine.AI;

public class CampusNavigator : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] destinations;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform arrow;

    public void SetDestination(int index)
    {
        if (destinations == null || destinations.Length == 0) return;
        if (index < 0 || index >= destinations.Length) return;

        agent.SetDestination(destinations[index].position);
        UpdatePath();
    }

    private void Update()
    {
        if (agent == null || lineRenderer == null) return;
        if (!agent.hasPath) return;

        UpdatePath();
    }

    private void UpdatePath()
    {
        var corners = agent.path.corners;
        if (corners == null || corners.Length == 0)
        {
            lineRenderer.positionCount = 0;
            if (arrow != null) arrow.gameObject.SetActive(false);
            return;
        }

        lineRenderer.positionCount = corners.Length;
        lineRenderer.SetPositions(corners);

        if (arrow != null && corners.Length > 0)
        {
            arrow.position = corners[0];

            if (corners.Length > 1)
            {
                Vector3 direction = (corners[1] - corners[0]).normalized;
                if (direction.sqrMagnitude > 0f)
                {
                    arrow.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }

            arrow.gameObject.SetActive(true);
        }
    }
}