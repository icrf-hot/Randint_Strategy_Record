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
    private SaveSlotBrowser browser;
    public Button ResumeButton => resumeButton;
    public Button DeleteButton => deleteButton;
    public bool IsConfirmingDelete => confirmingDelete;
    public SaveSlotBrowser Browser => browser;

    private void OnEnable()
    {
        confirmingDelete = false;
        EnsureBrowser();
        resumeButton.onClick.AddListener(Continue);
        deleteButton.onClick.AddListener(Delete);
        browser.SetInteractable(true);
        browser.Open(RunSaveService.SelectedSlot);
    }

    private void EnsureBrowser()
    {
        if (browser != null) return;
        var dialog = (RectTransform)summaryText.transform.parent;
        dialog.sizeDelta = new Vector2(960, 640);
        Transform titleBar = dialog.Find("TitleBar");
        if (titleBar != null) SlotUI.Place((RectTransform)titleBar, new Vector2(0, 297), new Vector2(954, 42));
        SlotUI.Place(summaryText.rectTransform, new Vector2(0, -211), new Vector2(900, 72));
        summaryText.fontSizeMax = 20;
        SlotUI.Place((RectTransform)resumeButton.transform, new Vector2(-300, -280), new Vector2(160, 44));
        SlotUI.Place((RectTransform)deleteButton.transform, new Vector2(0, -280), new Vector2(160, 44));
        Transform close = dialog.Find("CloseButton");
        if (close != null) SlotUI.Place((RectTransform)close, new Vector2(300, -280), new Vector2(160, 44));
        browser = SaveSlotBrowser.Create(dialog, summaryText.font, _ =>
        {
            confirmingDelete = false;
            Refresh();
        });
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveListener(Continue);
        deleteButton.onClick.RemoveListener(Delete);
        confirmingDelete = false;
    }

    private void Refresh(string notice = null)
    {
        var repository = RunSaveService.Slots.At(browser.SelectedSlot);
        bool valid = repository.TryRead(out _, out _);
        resumeButton.interactable = valid && !sceneLoader.IsLoading;
        deleteButton.interactable = repository.HasFiles && !sceneLoader.IsLoading;
        resumeButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.continue");
        deleteButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.delete");
        summaryText.text = browser.Error == RunSaveError.None ? SlotUI.Summary(browser.SelectedSlot) : RunSaveMessages.Error(browser.Error);
        if (!string.IsNullOrEmpty(notice)) summaryText.text += "\n" + notice;
    }

    public void Continue()
    {
        if (confirmingDelete || sceneLoader.IsLoading) return;
        var repository = RunSaveService.Slots.At(browser.SelectedSlot);
        if (!repository.TryRead(out RunSaveSnapshot snapshot, out RunSaveError error))
        { Refresh(RunSaveMessages.Error(error)); return; }
        int previous = RunSaveService.SelectedSlot;
        if (!RunSaveService.SelectSlot(browser.SelectedSlot) || !sceneLoader.TryContinue(snapshot))
        { RunSaveService.SelectSlot(previous); Refresh(RunSaveMessages.Error(RunSaveError.NotAllowed)); return; }
        resumeButton.interactable = false;
        deleteButton.interactable = false;
        browser.SetInteractable(false);
    }

    public void Delete()
    {
        var repository = RunSaveService.Slots.At(browser.SelectedSlot);
        if (sceneLoader.IsLoading || !repository.HasFiles) return;
        if (!confirmingDelete)
        {
            confirmingDelete = true;
            summaryText.text = string.Format(GameData.Text("save.confirm_delete_slot"), browser.SelectedSlot);
            resumeButton.interactable = false;
            deleteButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.delete_confirm");
            return;
        }
        confirmingDelete = false;
        bool deleted = repository.TryDelete(out RunSaveError error);
        browser.Refresh();
        Refresh(deleted ? GameData.Text("save.deleted") : RunSaveMessages.Error(error));
    }
}
