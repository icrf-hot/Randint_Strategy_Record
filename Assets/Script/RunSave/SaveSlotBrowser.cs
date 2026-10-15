using System;
using System.Linq;
using Randint.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SaveSlotBrowser : MonoBehaviour
{
    private readonly Button[] slots = new Button[10];
    private Button previous, next, add;
    private TMP_Text pageLabel;
    private Action<int> selected;
    public int SelectedSlot { get; private set; } = 1;
    public int Page { get; private set; }
    public int PageCount { get; private set; } = 1;
    public RunSaveError Error { get; private set; }
    public Button AddButton => add;
    public Button NextButton => next;
    public Button PreviousButton => previous;
    public Button SlotButton(int index) => slots[index];
    private bool unlocked = true;

    public static SaveSlotBrowser Create(Transform parent, TMP_FontAsset font, Action<int> onSelected)
    {
        var go = new GameObject("SaveSlots", typeof(RectTransform), typeof(SaveSlotBrowser));
        go.transform.SetParent(parent, false);
        SlotUI.Place((RectTransform)go.transform, new Vector2(0, 35), new Vector2(900, 420));
        var browser = go.GetComponent<SaveSlotBrowser>();
        browser.selected = onSelected;
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            browser.slots[i] = SlotUI.Button(go.transform, "Slot" + (i + 1), font, "",
                new Vector2(i % 2 == 0 ? -228 : 228, 132 - i / 2 * 73), new Vector2(440, 65));
            browser.slots[i].onClick.AddListener(() => browser.SelectSlot(browser.Page * 10 + index + 1));
        }
        browser.previous = SlotUI.Button(go.transform, "PreviousPage", font, "<", new Vector2(-80, 193), new Vector2(40, 36));
        browser.next = SlotUI.Button(go.transform, "NextPage", font, ">", new Vector2(80, 193), new Vector2(40, 36));
        browser.add = SlotUI.Button(go.transform, "AddPage", font, "+", new Vector2(425, 193), new Vector2(44, 36));
        browser.pageLabel = SlotUI.Label(go.transform, "Page", font, new Vector2(0, 193), new Vector2(110, 36), 18);
        browser.previous.onClick.AddListener(() => browser.ChangePage(-1));
        browser.next.onClick.AddListener(() => browser.ChangePage(1));
        browser.add.onClick.AddListener(browser.AddPage);
        return browser;
    }

    public void Open(int slot)
    {
        RunSaveService.Slots.TryPageCount(out int pages, out RunSaveError error);
        PageCount = pages;
        Error = error;
        SelectedSlot = Mathf.Clamp(slot, 1, pages * 10);
        Page = (SelectedSlot - 1) / 10;
        Refresh();
    }

    public void SelectSlot(int slot)
    {
        if (!unlocked || slot < 1 || slot > PageCount * 10) return;
        SelectedSlot = slot;
        Page = (slot - 1) / 10;
        Refresh();
    }

    public void ChangePage(int direction)
    {
        if (!unlocked) return;
        int page = Mathf.Clamp(Page + direction, 0, PageCount - 1);
        if (page != Page) SelectSlot(page * 10 + 1);
    }

    public void AddPage()
    {
        if (!unlocked) return;
        if (!RunSaveService.Slots.TryAddPage(out int count, out RunSaveError error))
        { Error = error; selected?.Invoke(SelectedSlot); return; }
        Error = RunSaveError.None;
        PageCount = count;
        SelectSlot((count - 1) * 10 + 1);
    }

    public void SetInteractable(bool value)
    {
        unlocked = value;
        for (int i = 0; i < 10; i++) slots[i].interactable = value;
        previous.interactable = value && Page > 0;
        next.interactable = value && Page + 1 < PageCount;
        add.interactable = value && Error == RunSaveError.None;
    }

    public void Refresh()
    {
        pageLabel.text = (Page + 1) + " / " + PageCount;
        for (int i = 0; i < 10; i++)
        {
            int id = Page * 10 + i + 1;
            RunSaveRepository repository = RunSaveService.Slots.At(id);
            string state;
            if (repository.TryRead(out RunSaveSnapshot data, out RunSaveError error))
                state = string.Format(GameData.Text("save.slot_brief"), data.layer, data.packets,
                    DateTimeOffset.Parse(data.savedUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
            else state = GameData.Text(!repository.HasFiles ? "save.slot_empty" : "save.slot_unavailable");
            slots[i].GetComponentInChildren<TMP_Text>().text = string.Format(GameData.Text("save.slot"), id) + "\n" + state;
            slots[i].GetComponent<Image>().color = id == SelectedSlot ? new Color(0.72f, 0.91f, 0.84f) : new Color(0.94f, 0.95f, 0.97f);
        }
        SetInteractable(unlocked);
        selected?.Invoke(SelectedSlot);
    }
}

public static class SlotUI
{
    public static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size, int fontSize = 20)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, position, size);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12;
        text.fontSizeMax = fontSize;
        text.color = new Color(0.1f, 0.12f, 0.15f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    public static Button Button(Transform parent, string name, TMP_FontAsset font, string label, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, position, size);
        go.GetComponent<Image>().color = new Color(0.85f, 0.88f, 0.91f);
        TMP_Text text = Label(go.transform, "Label", font, Vector2.zero, size - new Vector2(16, 6));
        text.text = label;
        return go.GetComponent<Button>();
    }

    public static string Summary(int slot)
    {
        var repository = RunSaveService.Slots.At(slot);
        if (!repository.TryRead(out RunSaveSnapshot data, out RunSaveError error))
            return RunSaveMessages.Error(error);
        return string.Format(GameData.Text("save.slot_details"), slot,
            DateTimeOffset.Parse(data.savedUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            data.layer, data.packets, data.currentNodeId, data.ownedCacheIds.Length,
            string.Join(", ", data.operatorIds.Select(id => RunSaveBindings.Instance.Operator(id).OperatorName)));
    }
}
