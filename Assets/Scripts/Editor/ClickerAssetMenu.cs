#if UNITY_EDITOR
using System.IO;
using Clicker;
using UnityEditor;
using UnityEngine;

namespace Clicker.EditorTools
{
    public static class ClickerAssetMenu
    {
        [MenuItem("Clicker/Create Default Data Assets")]
        public static void CreateDefaultData()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(ClickerPaths.DataRoot);
            EnsureFolder(ClickerPaths.DataRoot + "/Upgrades");
            EnsureFolder(ClickerPaths.ClickDir);
            EnsureFolder(ClickerPaths.IdleDir);
            EnsureFolder(ClickerPaths.EnemyDir);

            var balance = LoadOrCreate<BalanceConfig>(ClickerPaths.Balance);
            BalanceDefaults.ApplyTo(balance);
            EditorUtility.SetDirty(balance);

            var dialogs = LoadOrCreate<DialogCatalog>(ClickerPaths.Dialogs);
            BalanceDefaults.FillDialogs(dialogs);
            EditorUtility.SetDirty(dialogs);

            CreateEnemy("EnemyA.asset", "enemy_a", "Алый", "Scarlet", new Color(0.85f, 0.25f, 0.28f));
            CreateEnemy("EnemyB.asset", "enemy_b", "Лазурный", "Azure", new Color(0.25f, 0.45f, 0.9f));
            CreateEnemy("EnemyC.asset", "enemy_c", "Янтарный", "Amber", new Color(0.92f, 0.7f, 0.2f));

            CreateChain(BalanceDefaults.ClickSpecs(), UpgradeKind.Click, ClickerPaths.ClickDir, "Click");
            CreateChain(BalanceDefaults.IdleSpecs(), UpgradeKind.Idle, ClickerPaths.IdleDir, "Idle");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Clicker: default data assets created under Assets/Resources/Data.");
        }

        static void CreateEnemy(string file, string id, string ru, string en, Color color)
        {
            var def = LoadOrCreate<EnemyDef>(ClickerPaths.EnemyDir + "/" + file);
            BalanceDefaults.FillEnemy(def, id, ru, en, color);
            EditorUtility.SetDirty(def);
        }

        static void CreateChain(BalanceDefaults.UpgradeSpec[] specs, UpgradeKind kind, string dir, string prefix)
        {
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                string path = $"{dir}/{prefix}_{i:00}.asset";
                var def = LoadOrCreate<UpgradeDef>(path);
                BalanceDefaults.FillUpgrade(def, spec, kind, null);
                EditorUtility.SetDirty(def);
            }
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
