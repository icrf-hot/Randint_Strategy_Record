using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns all scene activation, input blocking and terminal/audio transitions.</summary>
public sealed class TransitionManager : MonoBehaviour
{
    private static TransitionManager instance;
    public static bool IsBusy => instance != null && instance.busy;
    private bool busy;
    private UnityEngine.UI.Image cover;
    private TMPro.TMP_Text log;
    private GameObject overlay;
    private RectTransform scanEdge;
    private readonly Dictionary<AudioSource, float> volumes = new Dictionary<AudioSource, float>();
    private readonly Dictionary<UnityEngine.UI.Selectable, bool> controls = new Dictionary<UnityEngine.UI.Selectable, bool>();
    public static TransitionManager Instance {
        get {
            if (instance == null) {
                var owner = new GameObject("TransitionManager"); instance = owner.AddComponent<TransitionManager>();
                DontDestroyOnLoad(owner);
            }
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Canvas canvas = TerminalUI.Canvas("TerminalTransition", 32760); canvas.transform.SetParent(transform, false);
        cover = TerminalUI.Panel("ScanWipe", canvas.transform, Vector2.zero, Vector2.zero, UITheme.Tone.Base);
        TerminalUI.Stretch(cover.rectTransform); cover.raycastTarget = true;
        var blocker = canvas.gameObject.AddComponent<UnityEngine.UI.Image>(); blocker.color = UITheme.Clear; blocker.raycastTarget = true;
        log = TerminalUI.Label("Log", cover.transform, "> 내부 데이터 접근 중", new Vector2(0, -20), new Vector2(1120, 50), 19, UITheme.Tone.Accent, true);
        scanEdge = TerminalUI.Panel("ScanEdge", cover.transform, Vector2.zero, new Vector2(0, 2), UITheme.Tone.Accent).rectTransform;
        scanEdge.anchorMin = new Vector2(0, 1); scanEdge.anchorMax = Vector2.one; scanEdge.pivot = new Vector2(.5f, 1);
        scanEdge.sizeDelta = new Vector2(0, 2); scanEdge.anchoredPosition = Vector2.zero;
        overlay = canvas.gameObject; overlay.SetActive(false);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (busy) CaptureAudio(true); }
    public static bool TryLoad(string path, Action beforeActivate = null, Action failed = null, string message = "> 내부 데이터 접근 중")
    {
        if (IsBusy || string.IsNullOrWhiteSpace(path) || !Application.CanStreamedLevelBeLoaded(path)) return false;
        var manager = Instance;
        manager.busy = true; manager.StartCoroutine(manager.Load(path, beforeActivate, failed, message)); return true;
    }
    public static IEnumerator Entry()
    {
        // A source transition owns the reveal; standalone scene entry uses the same wipe.
        var manager = Instance;
        if (!manager.busy) { manager.busy = true; yield return manager.DirectEntry(); }
        else while (manager.busy) yield return null;
    }
    private IEnumerator DirectEntry()
    {
        overlay.SetActive(true); cover.rectTransform.anchorMax = Vector2.one;
        Time.timeScale = 0; yield return null;
        yield return Animate(false, false);
        Time.timeScale = 1; overlay.SetActive(false); busy = false;
    }
    private IEnumerator Load(string path, Action commit, Action failed, string message)
    {
        float previousScale = Time.timeScale;
        controls.Clear();
        foreach (var item in FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsSortMode.None)) controls[item] = item.interactable;
        CaptureAudio(false);
        overlay.SetActive(true); log.text = message; Time.timeScale = 0;
        yield return Animate(true, true);
        AsyncOperation operation = null; Exception error = null;
        try { operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single); }
        catch (Exception e) { error = e; }
        if (operation == null) {
            Debug.LogError("[Transition] Scene load failed: " + (error?.Message ?? path));
            yield return Animate(false, false);
            foreach (var item in controls) if (item.Key != null) item.Key.interactable = item.Value;
            RestoreAudio(); Time.timeScale = previousScale; overlay.SetActive(false); busy = false; failed?.Invoke(); yield break;
        }
        operation.allowSceneActivation = false;
        while (operation.progress < .9f) yield return null;
        try { commit?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
        Time.timeScale = 1;
        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        yield return null; yield return null; // destination Awake/Start creates its audio/UI
        CaptureAudio(true);
        Time.timeScale = 0;
        yield return Animate(false, true);
        RestoreAudio(); controls.Clear(); overlay.SetActive(false); Time.timeScale = 1; busy = false;
    }
    private void CaptureAudio(bool muteNew)
    {
        if (!muteNew) volumes.Clear();
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) {
            if (!volumes.ContainsKey(source)) { volumes[source] = source.volume; if (muteNew) source.volume = 0; }
        }
    }
    private void RestoreAudio() { foreach (var item in volumes) if (item.Key != null) item.Key.volume = item.Value; volumes.Clear(); }
    private IEnumerator Animate(bool covering, bool audio)
    {
        float elapsed = 0, duration = Mathf.Max(UITheme.TransitionTime, audio ? UITheme.AudioTime : 0);
        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / UITheme.TransitionTime);
            cover.rectTransform.anchorMax = new Vector2(1, covering ? t : 1 - t);
            cover.rectTransform.offsetMin = cover.rectTransform.offsetMax = Vector2.zero;
            if (audio) foreach (var item in volumes) if (item.Key != null)
                item.Key.volume = item.Value * (covering ? 1 - Mathf.Clamp01(elapsed / UITheme.AudioTime) : Mathf.Clamp01(elapsed / UITheme.AudioTime));
            yield return null;
        }
        cover.rectTransform.anchorMax = covering ? Vector2.one : new Vector2(1, 0);
    }
    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; if (instance == this) { RestoreAudio(); instance = null; } }
}
