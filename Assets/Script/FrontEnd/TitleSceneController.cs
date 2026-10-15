using UnityEngine;
using UnityEngine.UI;

public sealed class TitleSceneController : MonoBehaviour
{
    [SerializeField] private MenuSceneLoader sceneLoader;
    [SerializeField] private Button enterButton;
    [SerializeField] private string lobbyScenePath = "Assets/Scenes/Lobby.unity";

    private void OnEnable() => enterButton.onClick.AddListener(EnterLobby);
    private void OnDisable() => enterButton.onClick.RemoveListener(EnterLobby);

    public void EnterLobby()
    {
        if (sceneLoader.TryLoadScene(lobbyScenePath)) enterButton.interactable = false;
    }
}
