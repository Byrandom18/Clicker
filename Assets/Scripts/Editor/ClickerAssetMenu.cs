using Clicker;
using UnityEditor;
using UnityEngine;

namespace Clicker.EditorTools
{
    public static class ClickerAssetMenu
    {
        const string ResourcesDir = "Assets/Resources/Clicker";

        [MenuItem("Clicker/Create Default Data Assets")]
        public static void CreateDefaults()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ResourcesDir))
                AssetDatabase.CreateFolder("Assets/Resources", "Clicker");

            WriteAsset(ResourcesDir + "/BalanceConfig.asset", BalanceDefaults.CreateBalance());
            WriteAsset(ResourcesDir + "/EnemyCatalog.asset", BalanceDefaults.CreateEnemies());
            WriteAsset(ResourcesDir + "/DialogCatalog.asset", BalanceDefaults.CreateDialogs());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Clicker", "Данные записаны в Assets/Resources/Clicker", "OK");
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
