using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SaveSlotsPlayVerification
{
    private const string Key = "RSR.SlotsPlay.";
    private static RunSaveRepository root;
    private static IDisposable isolation;

    static SaveSlotsPlayVerification()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (SessionState.GetBool(Key + "Running", false) && EditorApplication.isPlaying &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetString(Key + "Error", message);
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Key + "Running", true);
        SessionState.SetInt(Key + "Stage", 0);
        SessionState.SetString(Key + "Error", "");
        SessionState.SetString(Key + "Directory", Path.Combine(Path.GetTempPath(), "RSR_SlotsPlay_" + Guid.NewGuid().ToString("N")));
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(100).ToString("O"));
        EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity");
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        MapRunState.Reset();
        Time.timeScale = 1;
        root = new RunSaveRepository(SessionState.GetString(Key + "Directory", ""), RunSaveBindings.Instance);
        isolation = RunSaveService.UseRepositoryForVerification(root);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        try
        {
            Require(DateTime.UtcNow < DateTime.Parse(SessionState.GetString(Key + "Deadline", "")).ToUniversalTime(), "Slots scenario timed out.");
            if (!EditorApplication.isPlaying) return;
            Require(SessionState.GetString(Key + "Error", "") == "", SessionState.GetString(Key + "Error", ""));
            int stage = SessionState.GetInt(Key + "Stage", 0);
            string scene = SceneManager.GetActiveScene().name;
            if ((stage == 0 || stage == 2) && scene == "Map_1F")
            {
                if (!RunSaveService.CanSave) return;
                var controls = UnityEngine.Object.FindFirstObjectByType<MapSaveControls>();
                if (controls == null || controls.SaveButton == null) return;
                controls.SaveButton.onClick.Invoke();
                if (stage == 0)
                {
                    Require(controls.Browser.PageCount == 1 && controls.Browser.SelectedSlot == 1, "Default slots are incorrect.");
                    CheckGrid(controls.Browser);
                    controls.ConfirmButton.onClick.Invoke();
                    Require(root.HasFiles, "Slot 1 save failed.");
                    controls.SaveButton.onClick.Invoke();
                    controls.Browser.AddButton.onClick.Invoke();
                    Require(controls.Browser.PageCount == 2 && controls.Browser.SelectedSlot == 11, "+ did not add ten slots.");
                    CheckGrid(controls.Browser);
                    controls.ConfirmButton.onClick.Invoke();
                    Require(RunSaveService.Slots.At(11).TryRead(out var saved, out _), "Slot 11 save failed.");
                    SessionState.SetString(Key + "RunId", saved.runId);
                }
                else
                {
                    Require(RunSaveService.SelectedSlot == 11 && MapRunState.RunId == SessionState.GetString(Key + "RunId", ""),
                        "Continue did not restore the selected slot.");
                    controls.Browser.SelectSlot(12);
                    controls.ConfirmButton.onClick.Invoke();
                    Require(RunSaveService.Slots.At(12).HasFiles, "Slot 12 save failed.");
                }
                var selectedRepository = RunSaveService.Repository;
                string before = File.ReadAllText(selectedRepository.FilePath);
                controls.LobbyButton.onClick.Invoke();
                controls.ConfirmButton.onClick.Invoke();
                Require(File.ReadAllText(selectedRepository.FilePath) == before && scene == "Map_1F", "Overwrite skipped confirmation.");
                controls.ConfirmButton.onClick.Invoke();
                SessionState.SetInt(Key + "Stage", stage + 1);
            }
            else if ((stage == 1 || stage == 3) && scene == "Lobby")
            {
                var lobby = UnityEngine.Object.FindFirstObjectByType<LobbySceneController>();
                if (lobby == null) return;
                ((Button)Field(lobby, "continueButton")).onClick.Invoke();
                var window = ((GameObject)Field(lobby, "continueWindow")).GetComponent<SaveSlotWindow>();
                CheckGrid(window.Browser);
                if (stage == 1)
                {
                    Require(window.Browser.PageCount == 2 && window.Browser.SelectedSlot == 11 && window.ResumeButton.interactable,
                        "Lobby did not reopen the selected page.");
                    window.Browser.AddButton.onClick.Invoke();
                    Require(window.Browser.PageCount == 3 && window.Browser.SelectedSlot == 21 && !window.ResumeButton.interactable,
                        "New page is not empty.");
                    window.Browser.PreviousButton.onClick.Invoke();
                    Require(window.Browser.SelectedSlot == 11, "Previous page navigation failed.");
                    window.DeleteButton.onClick.Invoke();
                    Require(window.IsConfirmingDelete, "Delete confirmation missing.");
                    window.Browser.SlotButton(1).onClick.Invoke();
                    Require(!window.IsConfirmingDelete && RunSaveService.Slots.At(11).HasFiles,
                        "Changing selection deleted a different slot.");
                    window.Browser.SlotButton(0).onClick.Invoke();
                    window.ResumeButton.onClick.Invoke();
                    SessionState.SetInt(Key + "Stage", 2);
                }
                else
                {
                    window.Browser.SelectSlot(11);
                    window.DeleteButton.onClick.Invoke();
                    window.DeleteButton.onClick.Invoke();
                    Require(!RunSaveService.Slots.At(11).HasFiles && root.HasFiles && RunSaveService.Slots.At(12).HasFiles,
                        "Selective deletion affected another slot.");
                    Require(!window.ResumeButton.interactable && !window.DeleteButton.interactable,
                        "Deleted slot actions remain enabled.");
                    Require(new RunSaveSlots(root).TryPageCount(out int pages, out _) && pages == 3,
                        "Empty added page was not retained.");
                    Finish(true, "Ten-slot grid, +ten pages, overwrite confirmation, selected-slot continue, selection cancels delete and slot-only deletion passed.");
                }
            }
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    private static void CheckGrid(SaveSlotBrowser browser)
    {
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < 10; i++)
        {
            RectTransform rect = (RectTransform)browser.SlotButton(i).transform;
            Require(rect.rect.width == 440 && rect.rect.height == 65, "Slot dimensions shifted.");
            TMP_Text text = browser.SlotButton(i).GetComponentInChildren<TMP_Text>();
            text.ForceMeshUpdate();
            Require(text.text.Contains((browser.Page * 10 + i + 1).ToString()), "Slot number is incorrect.");
            Require(text.textBounds.size.y <= text.rectTransform.rect.height + 1, "Slot text height overflow.");
        }
    }

    private static object Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Finish(bool passed, string message)
    {
        SessionState.SetBool(Key + "Running", false);
        foreach (int id in new[] { 1, 11, 12 }) RunSaveService.Slots.At(id).TryDelete(out _);
        string directory = SessionState.GetString(Key + "Directory", "");
        foreach (string name in new[] { "pages.json", "pages.json.tmp" })
            if (File.Exists(Path.Combine(directory, name))) File.Delete(Path.Combine(directory, name));
        if (Directory.Exists(directory)) Directory.Delete(directory, false);
        isolation?.Dispose();
        if (passed) Debug.Log("[Save Slots Play Verification] " + message);
        else Debug.LogError("[Save Slots Play Verification] " + message);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
