using System.Linq;
using Randint.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SaveSlotWindow : MonoBehaviour
{
    [SerializeField] private MenuSceneLoader sceneLoader;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button deleteButton;
    private bool confirmingDelete;
    public Button ResumeButton => resumeButton;
    public Button DeleteButton => deleteButton;
    public bool IsConfirmingDelete => confirmingDelete;

    private void OnEnable()
    {
        confirmingDelete = false;
        resumeButton.onClick.AddListener(Continue);
        deleteButton.onClick.AddListener(Delete);
        Refresh();
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveListener(Continue);
        deleteButton.onClick.RemoveListener(Delete);
        confirmingDelete = false;
    }

    private void Refresh(string notice = null)
    {
        bool valid = RunSaveService.Repository.TryRead(out RunSaveSnapshot save, out RunSaveError error);
        resumeButton.interactable = valid && !sceneLoader.IsLoading;
        deleteButton.interactable = RunSaveService.Repository.HasFiles && !sceneLoader.IsLoading;
        resumeButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.continue");
        deleteButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.delete");
        if (valid)
        {
            string party = string.Join(", ", save.operatorIds.Select(id => RunSaveBindings.Instance.Operator(id).OperatorName));
            summaryText.text = string.Format(GameData.Text("save.summary"),
                System.DateTimeOffset.Parse(save.savedUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                save.layer, save.packets, save.currentNodeId, party, save.ownedCacheIds.Length);
        }
        else summaryText.text = RunSaveMessages.Error(error);
        if (!string.IsNullOrEmpty(notice)) summaryText.text += "\n" + notice;
    }

    public void Continue()
    {
        if (confirmingDelete || sceneLoader.IsLoading) return;
        if (!RunSaveService.Repository.TryRead(out RunSaveSnapshot snapshot, out RunSaveError error))
        { Refresh(RunSaveMessages.Error(error)); return; }
        if (!sceneLoader.TryContinue(snapshot)) { Refresh(RunSaveMessages.Error(RunSaveError.NotAllowed)); return; }
        resumeButton.interactable = false;
        deleteButton.interactable = false;
    }

    public void Delete()
    {
        if (sceneLoader.IsLoading || !RunSaveService.Repository.HasFiles) return;
        if (!confirmingDelete)
        {
            confirmingDelete = true;
            summaryText.text = GameData.Text("save.confirm_delete");
            resumeButton.interactable = false;
            deleteButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.delete_confirm");
            return;
        }
        confirmingDelete = false;
        bool deleted = RunSaveService.Repository.TryDelete(out RunSaveError error);
        Refresh(deleted ? GameData.Text("save.deleted") : RunSaveMessages.Error(error));
    }
}
