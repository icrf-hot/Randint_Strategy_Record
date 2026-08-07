using UnityEngine;

[ExecuteAlways]
public class FitCollider : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private BoxCollider2D targetCollider;

    private void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
        targetCollider = GetComponent<BoxCollider2D>();
    }

    private void OnValidate()
    {
        Fit();
    }

    private void Awake()
    {
        Fit();
    }

    public void Fit()
    {
        if (targetRenderer == null || targetCollider == null)
            return;

        Bounds spriteBounds = targetRenderer.bounds;

        Vector3 localCenter = transform.InverseTransformPoint(spriteBounds.center);
        Vector3 localSize = transform.InverseTransformVector(spriteBounds.size);

        targetCollider.offset = localCenter;
        targetCollider.size = new Vector2(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y)
        );
    }
}