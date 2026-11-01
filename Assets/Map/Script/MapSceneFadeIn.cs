using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>Compatibility facade for map entry and save readiness.</summary>
public class MapSceneFadeIn : MonoBehaviour
{
    [SerializeField] private CanvasGroup entryCanvasGroup;
    [SerializeField] private CanvasGroup loadingTextCanvasGroup;
    [SerializeField] private TMP_Text loadingText;
    public bool IsFadeComplete { get; private set; }
    private void Awake()
    {
        if (entryCanvasGroup != null) { entryCanvasGroup.alpha = 0; entryCanvasGroup.blocksRaycasts = false; entryCanvasGroup.gameObject.SetActive(false); }
        if (loadingTextCanvasGroup != null) loadingTextCanvasGroup.alpha = 0;
        Time.timeScale = 0;
    }
    private IEnumerator Start()
    {
        yield return TransitionManager.Entry();
        MapRunState.CompleteMapEntry(); IsFadeComplete = true; Time.timeScale = 1;
    }
}
