using System.Collections;
using TMPro;
using UnityEngine;

public sealed class TerminalTitleEffect : MonoBehaviour
{
    private IEnumerator Start()
    {
        if (!TerminalPreferences.Motion) yield break;
        var source = GetComponent<TMP_Text>(); if (source == null) yield break;
        Vector3 rest = transform.localPosition;
        var echo = TerminalUI.Label("GlitchEcho", transform, source.text, Vector2.zero,
            source.rectTransform.rect.size, source.fontSize, UITheme.Tone.Danger);
        echo.font = source.font; echo.alignment = source.alignment;
        var cyan = TerminalUI.Label("GlitchCyanEcho", transform, source.text, Vector2.zero,
            source.rectTransform.rect.size, source.fontSize, UITheme.Tone.Accent);
        cyan.font = source.font; cyan.alignment = source.alignment;
        float start = Time.unscaledTime;
        for (int i = 0; i < 3; i++) {
            echo.rectTransform.anchoredPosition = new Vector2(i % 2 == 0 ? 4 : -4, 0);
            cyan.rectTransform.anchoredPosition = -echo.rectTransform.anchoredPosition;
            transform.localPosition = rest + Vector3.right * (i % 2 == 0 ? -2 : 2);
            yield return null;
        }
        Destroy(echo.gameObject); Destroy(cyan.gameObject); transform.localPosition = rest;
        var scan = TerminalUI.Panel("TitleScan", transform, Vector2.zero, new Vector2(source.rectTransform.rect.width, 2), UITheme.Tone.Accent);
        scan.color = UITheme.Alpha(UITheme.Accent, UITheme.GlowAlpha);
        while (Time.unscaledTime - start < UITheme.GlitchTime) {
            float t = (Time.unscaledTime - start) / UITheme.GlitchTime;
            scan.rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(source.rectTransform.rect.height / 2, -source.rectTransform.rect.height / 2, t));
            yield return null;
        }
        Destroy(scan.gameObject);
    }
}
