using UnityEngine;
using UnityEngine.SceneManagement;

public static class ShopSceneRouter
{
    public const string ShopPath = "Assets/Scenes/Shop.unity";
    public const string MapPath = "Assets/Scenes/Map_1F.unity";
    private static AsyncOperation transition;
    public static bool IsTransitioning => transition != null && !transition.isDone;

    public static bool EnterShop(MapNode node)
    {
        if (node == null || node.NodeType != MapNodeType.Shop ||
            !MapRunState.HasCurrentNode || MapRunState.CurrentNodeID != node.NodeID)
            return false;
        return Load(ShopPath);
    }

    public static bool ReturnToMap() => Load(MapPath);

    private static bool Load(string path)
    {
        if (IsTransitioning || !Application.CanStreamedLevelBeLoaded(path))
            return false;
        Time.timeScale = 1f;
        transition = SceneManager.LoadSceneAsync(path);
        return transition != null;
    }
}
