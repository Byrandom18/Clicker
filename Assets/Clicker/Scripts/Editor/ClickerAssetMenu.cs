using Clicker;
using UnityEditor;
using UnityEngine;

namespace Clicker.EditorTools
{
    public static class ClickerAssetMenu
    {
        const string ResourcesDir = "Assets/Clicker/Resources/Clicker";

        [MenuItem("Clicker/Create Default Data Assets")]
        public static void CreateDefaults()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Clicker"))
                AssetDatabase.CreateFolder("Assets", "Clicker");
            if (!AssetDatabase.IsValidFolder("Assets/Clicker/Resources"))
                AssetDatabase.CreateFolder("Assets/Clicker", "Resources");
            if (!AssetDatabase.IsValidFolder(ResourcesDir))
                AssetDatabase.CreateFolder("Assets/Clicker/Resources", "Clicker");

            WriteAsset(ResourcesDir + "/BalanceConfig.asset", BalanceDefaults.CreateBalance());
            WriteAsset(ResourcesDir + "/EnemyCatalog.asset", BalanceDefaults.CreateEnemies());
            WriteAsset(ResourcesDir + "/DialogCatalog.asset", BalanceDefaults.CreateDialogs());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Clicker", "Default data assets written to Assets/Clicker/Resources/Clicker", "OK");
        }

        static void WriteAsset<T>(string path, T asset) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(asset);
                return;
            }

            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
