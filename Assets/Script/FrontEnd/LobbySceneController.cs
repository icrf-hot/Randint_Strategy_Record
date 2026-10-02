using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LobbySceneController : MonoBehaviour
{
    [SerializeField] private MenuSceneLoader sceneLoader;
    [SerializeField] private string mapScenePath = "Assets/Scenes/Map_1F.unity";
    [SerializeField] private CanvasGroup mainWindow;
    [SerializeField] private GameObject settingsWindow;
    [SerializeField] private GameObject continueWindow;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button settingsCloseButton;
    [SerializeField] private Button continueCloseButton;

    private Button returnFocus;
    public bool IsModalOpen { get; private set; }

    private void Awake()
    {
        settingsWindow.SetActive(false);
        continueWindow.SetActive(false);
    }

    private void OnEnable()
    {
        settingsButton.onClick.AddListener(OpenSettings);
        continueButton.onClick.AddListener(OpenContinue);
        newGameButton.onClick.AddListener(StartNewGame);
        settingsCloseButton.onClick.AddListener(CloseWindows);
        continueCloseButton.onClick.AddListener(CloseWindows);
    }

    private void OnDisable()
    {
        settingsButton.onClick.RemoveListener(OpenSettings);
        continueButton.onClick.RemoveListener(OpenContinue);
        newGameButton.onClick.RemoveListener(StartNewGame);
        settingsCloseButton.onClick.RemoveListener(CloseWindows);
        continueCloseButton.onClick.RemoveListener(CloseWindows);
    }

    public void OpenSettings() => OpenWindow(settingsWindow, settingsButton, settingsCloseButton);
    public void OpenContinue() => OpenWindow(continueWindow, continueButton, continueCloseButton);

    private void OpenWindow(GameObject window, Button source, Button close)
    {
        if (sceneLoader.IsLoading || IsModalOpen) return;
        IsModalOpen = true;
        returnFocus = source;
        mainWindow.interactable = false;
        window.SetActive(true);
        close.Select();
    }

    public void CloseWindows()
    {
        settingsWindow.SetActive(false);
        continueWindow.SetActive(false);
        IsModalOpen = false;
        mainWindow.interactable = !sceneLoader.IsLoading;
        if (returnFocus != null) returnFocus.Select();
    }

    public void StartNewGame()
    {
        if (IsModalOpen) return;
        // 저장 정책이 미정이므로 디스크 저장/삭제 없이 현재 메모리의 맵 위치만 리셋합니다.
        if (sceneLoader.TryLoadScene(mapScenePath, resetMapRun: true))
        {
            mainWindow.interactable = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
