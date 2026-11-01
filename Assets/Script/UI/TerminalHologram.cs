using UnityEngine;

public sealed class TerminalHologram : MonoBehaviour
{
    public UITheme.Tone tone = UITheme.Tone.Accent;
    public bool trail;
    private Material material;
    private TrailRenderer line;
    private void Start()
    {
        Shader shader = Shader.Find("Randint/TerminalHologram");
        if (shader == null) return;
        material = new Material(shader); material.SetColor("_BaseColor", UITheme.Resolve(tone));
        if (TryGetComponent<MeshRenderer>(out var renderer)) renderer.sharedMaterial = material;
        var node = GetComponentInParent<MapNode>();
        if (node != null) node.RefreshDefaultVisual(MapRunState.HasCurrentNode && MapRunState.CurrentNodeID == node.NodeID);
        if (!trail) return;
        var trailObject = new GameObject("HologramTrail", typeof(TrailRenderer)); trailObject.transform.SetParent(transform, false);
        line = trailObject.GetComponent<TrailRenderer>();
        line.sharedMaterial = material; line.time = .3f; line.startWidth = .15f; line.endWidth = 0;
        line.minVertexDistance = .04f; line.startColor = UITheme.Resolve(tone); line.endColor = UITheme.Clear;
        line.emitting = TerminalPreferences.Motion;
    }
    private void Update() { if (line != null) line.emitting = TerminalPreferences.Motion && !TransitionManager.IsBusy; }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
