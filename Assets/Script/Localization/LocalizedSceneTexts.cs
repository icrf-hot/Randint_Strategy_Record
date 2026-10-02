using System;
using Randint.Data;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class LocalizedSceneTexts : MonoBehaviour
{
    [Serializable]
    public sealed class Binding
    {
        public TMP_Text target;
        [GameTextKey] public string key;
    }

    [SerializeField] private Binding[] bindings;

    private void Awake()
    {
        // 비활성 UI도 처음에 설정합니다. 이후 숫자·결과·타이핑은 각 담당 코드가 갱신합니다.
        // OnEnable마다 덮어쓰지 않아 전투 중 바뀐 내용을 초기 문구로 되돌리지 않습니다.
        foreach (Binding binding in bindings ?? Array.Empty<Binding>())
        {
            if (binding == null || binding.target == null)
            {
                Debug.LogError("[GameData] LocalizedSceneTexts의 TMP 참조가 누락되었습니다.", this);
                continue;
            }
            binding.target.text = GameData.Text(binding.key);
        }
    }
}
