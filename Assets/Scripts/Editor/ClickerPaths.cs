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
        public const string AudioDir = "Assets/Resources/Audio";
        public const string PrefabDir = "Assets/Prefabs";
        public const string ResourcesPrefabDir = "Assets/Resources/Prefabs";
        public const string ShopRow = PrefabDir + "/ShopRow.prefab";
        public const string DamagePopup = PrefabDir + "/DamagePopup.prefab";
        public const string EnemyView = PrefabDir + "/EnemyView.prefab";
        public const string EnemyViewResources = ResourcesPrefabDir + "/EnemyView.prefab";
        public const string StageChangeVfx =
            "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab";
        public const string ClickHitVfx =
            "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab";
        public const string RewardedHitVfx =
            "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR3 Hit Fire B (Air).prefab";
    }
}
#endif
