using System;
using System.Collections;
using Randint.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class TitleSceneController : MonoBehaviour
{
    [SerializeField] private MenuSceneLoader sceneLoader;
    [SerializeField] private Button enterButton;
    [SerializeField] private string lobbyScenePath = "Assets/Scenes/Lobby.unity";
    private bool ready;
    private void OnEnable() => enterButton.onClick.AddListener(EnterLobby);
    private void OnDisable() => enterButton.onClick.RemoveListener(EnterLobby);
    private IEnumerator Start()
    {
        ready = false; enterButton.interactable = false;
        Transform root = enterButton.transform.parent;
        var count = root.Find("Context")?.GetComponent<TMPro.TMP_Text>();
        var progress = root.Find("BootProgress")?.GetComponent<TerminalProgressBar>();
        var log = root.Find("BootLog")?.GetComponent<TMPro.TMP_Text>();
        if (count != null) { count.font = UITheme.Mono; count.text = "0000000 / 0000003"; }
        float started = Time.unscaledTime;
        string[] lines = { "> 기록 무결성 확인...", "> 작전 데이터 복원...", "> 접근 권한 대기" };
        Action[] checks = {
            () => { if (GameData.Catalog == null) throw new InvalidOperationException("게임 데이터 검증 실패"); },
            () => { if (!PreparationSceneController.TryGetRoster(out _) || RunSaveBindings.Instance.Progression == null || !RunSaveBindings.Instance.Progression.IsValid) throw new InvalidOperationException("편성 또는 작전 데이터 오류"); },
            () => { if (!Application.CanStreamedLevelBeLoaded(lobbyScenePath)) throw new InvalidOperationException("관리 화면이 등록되지 않았습니다"); }
        };
        for (int i = 0; i < checks.Length; i++) {
            if (log != null) log.text += (i > 0 ? "\n" : "") + lines[i];
            yield return null; // render the real stage before checking it
            Exception error = null; try { checks[i](); } catch (Exception e) { error = e; }
            if (error != null) {
                if (log != null) { log.text += "\n> " + error.Message; log.color = UITheme.Danger; }
                Debug.LogError("[Terminal boot] " + error.Message, this); yield break;
            }
            if (count != null) count.text = (i + 1).ToString("D7") + " / " + checks.Length.ToString("D7");
            if (progress != null) progress.SetValue((i + 1f) / checks.Length);
            yield return null;
        }
        // Progress is already complete; only the explicitly requested title intro remains.
        while (TerminalPreferences.Motion && Time.unscaledTime - started < UITheme.GlitchTime) yield return null;
        ready = true; enterButton.interactable = true;
    }
    public void EnterLobby()
    {
        if (!ready || !enterButton.IsInteractable()) return;
        if (sceneLoader.TryLoadScene(lobbyScenePath)) enterButton.interactable = false;
    }
}
