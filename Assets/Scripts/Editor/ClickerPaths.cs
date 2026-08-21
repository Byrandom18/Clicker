#if UNITY_EDITOR
namespace Clicker.EditorTools
{
    public static class ClickerPaths
    {
        public const string DataRoot = "Assets/Resources/Data";
        public const string ClickDir = DataRoot + "/Upgrades/Click";
        public const string IdleDir = DataRoot + "/Upgrades/Idle";
        public const string EnemyDir = DataRoot + "/Enemies";
        public const string Balance = DataRoot + "/Balance.asset";
        public const string Dialogs = DataRoot + "/Dialogs.asset";
        public const string PrefabDir = "Assets/Prefabs";
        public const string ResourcesPrefabDir = "Assets/Resources/Prefabs";
        public const string ShopRow = PrefabDir + "/ShopRow.prefab";
        public const string EnemyView = PrefabDir + "/EnemyView.prefab";
        public const string EnemyViewResources = ResourcesPrefabDir + "/EnemyView.prefab";
    }
}
#endif
