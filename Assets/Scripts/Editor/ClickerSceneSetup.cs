#if UNITY_EDITOR
using Clicker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Clicker.EditorTools
{
    public static class ClickerSceneSetup
    {
        [MenuItem("Clicker/Add World Objects To Open Scene")]
        public static void AddWorld()
        {
            ClickerPrefabBuilder.CreatePrefabs();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Clicker/Prefabs/EnemyView.prefab");
            if (prefab == null)
            {
                Debug.LogError("Clicker: EnemyView prefab missing.");
                return;
            }

            var slots = new GameObject("Slots");
            var director = slots.AddComponent<EnemySlotDirector>();
            var left = CreateSlot(slots.transform, "SlotLeft", new Vector3(-4.2f, -0.4f, 0f), 0.62f);
            var center = CreateSlot(slots.transform, "SlotCenter", new Vector3(-1.1f, -0.6f, 0f), 1f);
            var right = CreateSlot(slots.transform, "SlotRight", new Vector3(1.6f, -0.4f, 0f), 0.62f);

            var a = SpawnEnemy(prefab, "EnemyA", "Assets/Clicker/Data/Enemies/EnemyA.asset");
            var b = SpawnEnemy(prefab, "EnemyB", "Assets/Clicker/Data/Enemies/EnemyB.asset");
            var c = SpawnEnemy(prefab, "EnemyC", "Assets/Clicker/Data/Enemies/EnemyC.asset");

            var so = new SerializedObject(director);
            so.FindProperty("slotLeft").objectReferenceValue = left;
            so.FindProperty("slotCenter").objectReferenceValue = center;
            so.FindProperty("slotRight").objectReferenceValue = right;
            so.FindProperty("enemyA").objectReferenceValue = a;
            so.FindProperty("enemyB").objectReferenceValue = b;
            so.FindProperty("enemyC").objectReferenceValue = c;
            so.FindProperty("duration").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var root = new GameObject("GameRoot");
            var game = root.AddComponent<ClickerGame>();
            var gameSo = new SerializedObject(game);
            gameSo.FindProperty("balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceConfig>("Assets/Clicker/Data/Balance.asset");
            gameSo.FindProperty("dialogs").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogCatalog>("Assets/Clicker/Data/Dialogs.asset");
            gameSo.FindProperty("slots").objectReferenceValue = director;
            gameSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Debug.Log("Clicker: world objects added. Assemble Canvas/UI yourself and drag remaining refs onto GameRoot/ClickerGame.");
        }

        static Transform CreateSlot(Transform parent, string name, Vector3 pos, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            return go.transform;
        }

        static EnemyView SpawnEnemy(GameObject prefab, string name, string defPath)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            var view = go.GetComponent<EnemyView>();
            var def = AssetDatabase.LoadAssetAtPath<EnemyDef>(defPath);
            var so = new SerializedObject(view);
            so.FindProperty("definition").objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
#endif
