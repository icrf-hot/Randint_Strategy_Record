public static class ShopSceneRouter
{
    public const string ShopPath = "Assets/Scenes/Shop.unity";
    public static string MapPath => CampaignScenes.CurrentPath;
    public static bool IsTransitioning => TransitionManager.IsBusy;
    public static bool EnterShop(MapNode node)
    {
        if (node == null || node.NodeType != MapNodeType.Shop || !MapRunState.HasCurrentNode || MapRunState.CurrentNodeID != node.NodeID) return false;
        bool accepted = TransitionManager.TryLoad(ShopPath, message: "> 보급 데이터 연결 중");
        if (accepted) MapRunState.CompleteNode(node.NodeID);
        return accepted;
    }
    public static bool ReturnToMap() => TransitionManager.TryLoad(MapPath, message: "> 작전 지도 연결 중");
}
