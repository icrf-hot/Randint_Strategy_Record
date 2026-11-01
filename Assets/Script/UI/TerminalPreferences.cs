using UnityEngine;

public static class TerminalPreferences
{
    public static bool Monitor { get => PlayerPrefs.GetInt("Terminal.Monitor", 1) != 0; set { PlayerPrefs.SetInt("Terminal.Monitor", value ? 1 : 0); PlayerPrefs.Save(); } }
    public static bool Motion { get => PlayerPrefs.GetInt("Terminal.Motion", 1) != 0; set { PlayerPrefs.SetInt("Terminal.Motion", value ? 1 : 0); PlayerPrefs.Save(); } }
}
