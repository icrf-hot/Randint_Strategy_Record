using UnityEngine;

public sealed class TerminalMapCursor : MonoBehaviour
{
    private MapPlayerPositionFeedback feedback;
    private Transform marker;
    private bool placed;
    private void Start()
    {
        feedback = FindFirstObjectByType<MapPlayerPositionFeedback>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = "TerminalPlayerCursor";
        cube.transform.SetParent(transform); cube.transform.localScale = Vector3.one * .2f;
        Destroy(cube.GetComponent<Collider>());
        var holo = cube.AddComponent<TerminalHologram>(); holo.tone = UITheme.Tone.Ok; holo.trail = true;
        marker = cube.transform;
    }
    private void LateUpdate()
    {
        if (marker == null || feedback == null || feedback.CurrentNode == null) return;
        Vector3 target = feedback.CurrentNode.transform.position + Vector3.up * .65f;
        marker.position = !placed || !TerminalPreferences.Motion ? target : Vector3.Lerp(marker.position, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 12));
        placed = true;
    }
}
