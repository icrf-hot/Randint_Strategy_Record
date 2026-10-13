using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneRouter : MonoBehaviour
{
    [SerializeField] private string mapScenePath = "Assets/Scenes/Map_1F.unity";
    [SerializeField] private RunProgressionSettings progressionSettings;

    private bool isLoading;

    private IEnumerator Start()
    {
        yield return null;
        StartNewRun();
    }

    public void StartNewRun()
    {
        if (isLoading)
            return;

        if (progressionSettings == null || !progressionSettings.IsValid)
        {
            Debug.LogError("[Title Router] Run progression settings are missing or invalid.", this);
            return;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(mapScenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[Title Router] Map scene is not enabled in Build Settings: {mapScenePath}", this);
            return;
        }

        AsyncOperation loadOperation;
        try
        {
            loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Title Router] Could not load map: {exception.Message}", this);
            return;
        }

        if (loadOperation == null)
        {
            Debug.LogError("[Title Router] Map load did not start.", this);
            return;
        }

        loadOperation.allowSceneActivation = false;
        isLoading = true;
        MapRunState.StartNewRun(progressionSettings);
        Time.timeScale = 1f;
        loadOperation.allowSceneActivation = true;
    }
}
