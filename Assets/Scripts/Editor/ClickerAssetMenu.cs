#if UNITY_EDITOR
using System.IO;
using Clicker;
using UnityEditor;
using UnityEngine;

namespace Clicker.EditorTools
{
    public static class ClickerAssetMenu
    {
        const string DataRoot = "Assets/Clicker/Data";
        const string ClickDir = DataRoot + "/Upgrades/Click";
        const string IdleDir = DataRoot + "/Upgrades/Idle";
        const string EnemyDir = DataRoot + "/Enemies";

        [MenuItem("Clicker/Create Default Data Assets")]
        public static void CreateDefaultData()
        {
            EnsureFolder("Assets/Clicker");
            EnsureFolder(DataRoot);
            EnsureFolder(DataRoot + "/Upgrades");
            EnsureFolder(ClickDir);
            EnsureFolder(IdleDir);
            EnsureFolder(EnemyDir);

            var balance = LoadOrCreate<BalanceConfig>(DataRoot + "/Balance.asset");
            BalanceDefaults.ApplyTo(balance);
            EditorUtility.SetDirty(balance);

            var dialogs = LoadOrCreate<DialogCatalog>(DataRoot + "/Dialogs.asset");
            BalanceDefaults.FillDialogs(dialogs);
            EditorUtility.SetDirty(dialogs);

            CreateEnemy("EnemyA.asset", "enemy_a", "Алый", "Scarlet", new Color(0.85f, 0.25f, 0.28f));
            CreateEnemy("EnemyB.asset", "enemy_b", "Лазурный", "Azure", new Color(0.25f, 0.45f, 0.9f));
            CreateEnemy("EnemyC.asset", "enemy_c", "Янтарный", "Amber", new Color(0.92f, 0.7f, 0.2f));

            CreateChain(BalanceDefaults.ClickSpecs(), UpgradeKind.Click, ClickDir, "Click");
            CreateChain(BalanceDefaults.IdleSpecs(), UpgradeKind.Idle, IdleDir, "Idle");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Clicker: default data assets created under Assets/Clicker/Data.");
        }

        static void CreateEnemy(string file, string id, string ru, string en, Color color)
        {
            var def = LoadOrCreate<EnemyDef>(EnemyDir + "/" + file);
            BalanceDefaults.FillEnemy(def, id, ru, en, color);
            EditorUtility.SetDirty(def);
        }

        static void CreateChain(BalanceDefaults.UpgradeSpec[] specs, UpgradeKind kind, string dir, string prefix)
        {
            UpgradeDef prev = null;
            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                string path = $"{dir}/{prefix}_{i:00}.asset";
                var def = LoadOrCreate<UpgradeDef>(path);
                BalanceDefaults.FillUpgrade(def, spec, kind, prev);
                EditorUtility.SetDirty(def);
                prev = def;
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
