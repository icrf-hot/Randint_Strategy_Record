using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapBattleSceneLoader : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset battleScene;
    private void OnValidate() { if (battleScene != null) battleScenePath = AssetDatabase.GetAssetPath(battleScene); }
#endif
    [SerializeField, HideInInspector] private string battleScenePath;
    public bool IsLoading => TransitionManager.IsBusy;
    public void LoadBattle(MapNode node)
    {
        if (IsLoading || MapRunState.IsEnded || node == null || node.NodeType != MapNodeType.Battle) return;
        if (!TransitionManager.TryLoad(battleScenePath, message: "> 전투 데이터 연결 중"))
            Debug.LogError("[Map Battle] Battle scene unavailable: " + battleScenePath, this);
    }
}
