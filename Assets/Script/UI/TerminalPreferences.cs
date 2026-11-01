using UnityEngine;

public static class TerminalPreferences
{
    public static int CombatSpeed { get => Mathf.Clamp(PlayerPrefs.GetInt("Combat.Speed", 1), 1, 3); set { PlayerPrefs.SetInt("Combat.Speed", Mathf.Clamp(value, 1, 3)); PlayerPrefs.Save(); } }
    public static float ShakeStrength { get => Mathf.Clamp01(PlayerPrefs.GetFloat("Combat.Shake", .6f)); set { PlayerPrefs.SetFloat("Combat.Shake", Mathf.Clamp01(value)); PlayerPrefs.Save(); } }
    public static bool HitStop { get => PlayerPrefs.GetInt("Combat.HitStop", 1) != 0; set { PlayerPrefs.SetInt("Combat.HitStop", value ? 1 : 0); PlayerPrefs.Save(); } }
    public static bool ReduceFlash { get => PlayerPrefs.GetInt("Combat.ReduceFlash", 0) != 0; set { PlayerPrefs.SetInt("Combat.ReduceFlash", value ? 1 : 0); PlayerPrefs.Save(); } }
    public static bool Monitor { get => PlayerPrefs.GetInt("Terminal.Monitor", 1) != 0; set { PlayerPrefs.SetInt("Terminal.Monitor", value ? 1 : 0); PlayerPrefs.Save(); } }
    public static bool Motion { get => PlayerPrefs.GetInt("Terminal.Motion", 1) != 0; set { PlayerPrefs.SetInt("Terminal.Motion", value ? 1 : 0); PlayerPrefs.Save(); } }
}
