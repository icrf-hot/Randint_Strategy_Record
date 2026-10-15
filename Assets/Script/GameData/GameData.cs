using UnityEngine;

namespace Randint.Data
{
    public static class GameData
    {
        private static GameDataCatalog catalog;
        public static GameDataCatalog Catalog => catalog ?? (catalog = LoadCatalog());

        private static GameDataCatalog LoadCatalog() => GameDataCatalog.Load(path =>
        {
            TextAsset asset = Resources.Load<TextAsset>(path);
            return asset == null ? null : asset.text;
        });

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => catalog = null;

        // 씬의 Awake보다 먼저 검증합니다. 직접 Play 씬을 실행해도 동일 경로를 사용합니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => _ = Catalog;

        public static string Text(string key) => Catalog.Text(key);
    }

    public sealed class GameTextKeyAttribute : PropertyAttribute { }
    public sealed class GameDefinitionIdAttribute : PropertyAttribute
    {
        public string Kind { get; }
        public GameDefinitionIdAttribute(string kind) => Kind = kind;
    }
}
