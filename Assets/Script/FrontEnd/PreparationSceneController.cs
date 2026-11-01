using System;
using Randint.Data;
using TMPro;
using UnityEngine;

public sealed class PreparationSceneController : MonoBehaviour
{
    public const int DataLossLevel = 0;
    [SerializeField] private MenuSceneLoader sceneLoader;
    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private UnityEngine.UI.Button[] changeButtons;
    [SerializeField] private TMP_Text[] operatorNames;
    [SerializeField] private TMP_Text dataLossText;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string mapScenePath = "Assets/Scenes/Map_1F.unity";
    [SerializeField] private string lobbyScenePath = "Assets/Scenes/Lobby.unity";

    private void Start()
    {
        dataLossText.text = string.Format(GameData.Text("preparation.loss_level"), DataLossLevel);
        warningText.text = "";
        bool valid = TryGetRoster(out OperatorData[] roster);
        startButton.interactable = valid;
        for (int i = 0; i < operatorNames.Length; i++)
            operatorNames[i].text = valid && i < roster.Length ? roster[i].OperatorName : GameData.Text("ui.common.unavailable");
        if (!valid) warningText.text = GameData.Text("preparation.invalid_roster");
    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(StartRun);
        backButton.onClick.AddListener(ReturnToLobby);
        foreach (var button in changeButtons) button.onClick.AddListener(ShowLockedWarning);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(StartRun);
        backButton.onClick.RemoveListener(ReturnToLobby);
        foreach (var button in changeButtons) button.onClick.RemoveListener(ShowLockedWarning);
    }

    public static bool TryGetRoster(out OperatorData[] roster)
    {
        roster = Array.Empty<OperatorData>();
        RunSaveBindings bindings = RunSaveBindings.Instance;
        if (bindings == null) return false;
        string[] ids = bindings.DefaultPartyIds;
        if (ids == null || ids.Length != 3) return false;
        var result = new OperatorData[3];
        OperatorPosition[] positions = { OperatorPosition.Front, OperatorPosition.Middle, OperatorPosition.Back };
        foreach (string id in ids)
        {
            OperatorData data = bindings.Operator(id);
            if (data == null) return false;
            int index = Array.IndexOf(positions, data.Position);
            if (index < 0 || result[index] != null) return false;
            result[index] = data;
        }
        if (Array.Exists(result, data => data == null)) return false;
        roster = result;
        return true;
    }

    public void ShowLockedWarning()
    {
        if (sceneLoader.IsLoading) return;
        warningText.text = GameData.Text("preparation.roster_locked");
        Debug.LogWarning("[Preparation] " + warningText.text, this);
    }

    public void StartRun()
    {
        if (sceneLoader.IsLoading) return;
        if (!TryGetRoster(out _))
        { warningText.text = GameData.Text("preparation.invalid_roster"); return; }
        if (sceneLoader.TryLoadScene(mapScenePath, resetMapRun: true))
        {
            Debug.Log("[Preparation] Start confirmed: default FRONT/MIDDLE/BACK roster, data loss level 0.", this);
            DisableActions();
        }
    }

    public void ReturnToLobby()
    {
        if (sceneLoader.TryLoadScene(lobbyScenePath)) DisableActions();
    }

    private void DisableActions()
    {
        startButton.interactable = false;
        backButton.interactable = false;
        foreach (var button in changeButtons) button.interactable = false;
    }
}
