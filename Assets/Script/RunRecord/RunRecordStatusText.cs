using Randint.Data;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class RunRecordStatusText : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private bool showIntegrity = true;
    private int displayedValue = -1;

    private void Awake()
    {
        if (targetText == null) targetText = GetComponent<TMP_Text>();
        targetText.enableAutoSizing = true;
        targetText.fontSizeMin = 14;
        targetText.fontSizeMax = targetText.fontSize;
        targetText.raycastTarget = false;
    }

    private void LateUpdate()
    {
        int value = showIntegrity ? MapRunState.RecordHealth.Integrity : MapRunState.RecordHealth.Consistency;
        if (displayedValue == value) return;
        displayedValue = value;
        targetText.text = string.Format(GameData.Text(showIntegrity ? "ui.map.integrity_value" : "ui.map.consistency_value"), value);
    }
}
