using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MapPath : MonoBehaviour
{
    [SerializeField] private MapNode startNode;
    [SerializeField] private MapNode endNode;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void LateUpdate()
    {
        if (startNode == null ||
            endNode == null)
            return;

        line.positionCount = 2;

        line.SetPosition(
            0,
            startNode.transform.position);

        line.SetPosition(
            1,
            endNode.transform.position);
    }
}