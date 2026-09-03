#if UNITY_EDITOR
using System.IO;
using Clicker;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace Clicker.EditorTools
{
    public static class ClickerSceneSetup
    {
        [MenuItem("Clicker/Reset Editor Save")]
        public static void ResetEditorSave()
        {
            string path = Path.Combine(InfoYG.PATCH_PC_EDITOR, "SavesEditorYG2.json");
            File.WriteAllText(path,
                "{\n  \"idSave\": 1,\n  \"clickerInitialized\": false,\n  \"clickerSaveVersion\": 2,\n  \"score\": 0.0,\n  \"phaseIndex\": 0,\n  \"hpLeft\": -1.0,\n  \"pendingOverflow\": 0.0,\n  \"muted\": false,\n  \"musicMuted\": false,\n  \"autoUpgradeLeft\": 0.0,\n  \"gameWon\": false,\n  \"endlessMode\": false,\n  \"upgrades\": []\n}\n");
            ClickerSave.ResetProgress();
            AssetDatabase.Refresh();
            Debug.Log("Clicker: editor save reset. Press Play for a fresh start.");
        }

        [MenuItem("Clicker/Wire Open Scene")]
        public static void WireOpenScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var game = Object.FindFirstObjectByType<ClickerGame>(FindObjectsInactive.Include);
            if (game == null)
            {
                var root = new GameObject("GameRoot");
                game = root.AddComponent<ClickerGame>();
                Undo.RegisterCreatedObjectUndo(root, "Create GameRoot");
            }

            var slots = Object.FindFirstObjectByType<EnemySlotDirector>(FindObjectsInactive.Include);
            var shop = Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);
            var hud = Object.FindFirstObjectByType<HudView>(FindObjectsInactive.Include);
            var battle = Object.FindFirstObjectByType<BattleZoneClick>(FindObjectsInactive.Include);
            var bubble = Object.FindFirstObjectByType<SpeechBubbleView>(FindObjectsInactive.Include);
            var victory = Object.FindFirstObjectByType<VictoryView>(FindObjectsInactive.Include);
            var popups = Object.FindFirstObjectByType<DamagePopupPool>(FindObjectsInactive.Include);

            var victoryPanel = FindNamed("VictoryPanel");
            if (victory == null && victoryPanel != null)
            {
                victory = Undo.AddComponent<VictoryView>(victoryPanel);
                var title = FindTmp(victoryPanel.transform, "Text (TMP)");
                var soV = new SerializedObject(victory);
                soV.FindProperty("title").objectReferenceValue = title;
                soV.FindProperty("body").objectReferenceValue = title;
                soV.ApplyModifiedPropertiesWithoutUndo();
            }

            if (victory != null)
            {
                var soV = new SerializedObject(victory);
                var continueProp = soV.FindProperty("continueButton");
                if (continueProp != null && continueProp.objectReferenceValue == null)
                {
                    var continueGo = victory.transform.Find("ContinueButton");
                    if (continueGo != null)
                    {
                        continueProp.objectReferenceValue = continueGo.GetComponent<Button>();
                        var labelProp = soV.FindProperty("continueLabel");
                        var label = continueGo.GetComponentInChildren<TMP_Text>(true);
                        if (labelProp != null && label != null)
                            labelProp.objectReferenceValue = label;
                        soV.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            var gameSo = new SerializedObject(game);
            gameSo.FindProperty("balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceConfig>(ClickerPaths.Balance);
            gameSo.FindProperty("dialogs").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogCatalog>(ClickerPaths.Dialogs);
            if (slots != null)
                gameSo.FindProperty("slots").objectReferenceValue = slots;
            if (battle != null)
                gameSo.FindProperty("battleZone").objectReferenceValue = battle;
            if (shop != null)
                gameSo.FindProperty("shop").objectReferenceValue = shop;
            if (hud != null)
                gameSo.FindProperty("hud").objectReferenceValue = hud;
            if (bubble != null)
                gameSo.FindProperty("bubble").objectReferenceValue = bubble;
            if (victory != null)
                gameSo.FindProperty("victory").objectReferenceValue = victory;
            if (popups != null)
                gameSo.FindProperty("damagePopups").objectReferenceValue = popups;
            var vfxProp = gameSo.FindProperty("stageChangeVfxPrefab");
            if (vfxProp != null)
            {
                var vfx = AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.StageChangeVfx);
                if (vfx != null)
                    vfxProp.objectReferenceValue = vfx;
            }

            AssignPrefab(gameSo, "clickVfxPrefab", ClickerPaths.ClickHitVfx);
            AssignPrefab(gameSo, "rewardedVfxPrefab", ClickerPaths.RewardedHitVfx);

            var sfx = EnsureSfxController(game.transform);
            WireSfxController(sfx);
            var sfxProp = gameSo.FindProperty("sfx");
            if (sfxProp != null && sfx != null)
                sfxProp.objectReferenceValue = sfx;

            var music = EnsureMusicController(game.transform);
            WireMusicController(music);
            var musicProp = gameSo.FindProperty("music");
            if (musicProp != null && music != null)
                musicProp.objectReferenceValue = music;

            gameSo.ApplyModifiedPropertiesWithoutUndo();

            if (slots != null)
                WireSlots(slots);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = game.gameObject;
            Debug.Log("Clicker: scene wired (GameRoot refs, enemies, data from Assets/Resources/Data).");
        }

        static void AssignPrefab(SerializedObject so, string property, string path)
        {
            var prop = so.FindProperty(property);
            if (prop == null)
                return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prop.objectReferenceValue = prefab;
        }

        static SfxController EnsureSfxController(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<SfxController>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Sfx");
            return Undo.AddComponent<SfxController>(go);
        }

        static MusicController EnsureMusicController(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<MusicController>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var go = new GameObject("Music");
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create Music");
            return Undo.AddComponent<MusicController>(go);
        }

        static void WireMusicController(MusicController music)
        {
            if (music == null)
                return;

            var so = new SerializedObject(music);
            AssignSfxClip(so, "clip", "music");
            var sourceProp = so.FindProperty("source");
            if (sourceProp != null && sourceProp.objectReferenceValue == null)
            {
                var src = music.GetComponent<AudioSource>();
                if (src == null)
                    src = Undo.AddComponent<AudioSource>(music.gameObject);
                src.playOnAwake = false;
                src.loop = true;
                src.spatialBlend = 0f;
                src.ignoreListenerVolume = true;
                sourceProp.objectReferenceValue = src;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireSfxController(SfxController sfx)
        {
            if (sfx == null)
                return;

            var so = new SerializedObject(sfx);
            AssignClickClips(so);
            AssignSfxClip(so, "buyClip", "buy");
            AssignSfxClip(so, "phaseClip", "phase");
            AssignSfxClip(so, "explosionClip", "explosion");
            AssignSfxClip(so, "uiClip", "ui");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignClickClips(SerializedObject so)
        {
            var prop = so.FindProperty("clickClips");
            if (prop == null || prop.arraySize > 0)
                return;

            string[] names = { "click", "click_02", "click_03", "click_04", "click_05" };
            int count = 0;
            for (int i = 0; i < names.Length; i++)
            {
                if (LoadSfxClip(names[i]) != null)
                    count++;
            }

            if (count == 0)
                return;

            prop.arraySize = count;
            int index = 0;
            for (int i = 0; i < names.Length; i++)
            {
                var clip = LoadSfxClip(names[i]);
                if (clip == null)
                    continue;
                prop.GetArrayElementAtIndex(index).objectReferenceValue = clip;
                index++;
            }
        }

        static void AssignSfxClip(SerializedObject so, string property, string fileName)
        {
            var prop = so.FindProperty(property);
            if (prop == null || prop.objectReferenceValue != null)
                return;
            var clip = LoadSfxClip(fileName);
            if (clip != null)
                prop.objectReferenceValue = clip;
        }

        static AudioClip LoadSfxClip(string fileName)
        {
            string[] exts = { ".ogg", ".mp3", ".wav" };
            for (int i = 0; i < exts.Length; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    ClickerPaths.AudioDir + "/" + fileName + exts[i]);
                if (clip != null)
                    return clip;
            }

            return null;
        }

        [MenuItem("Clicker/Add World Objects To Open Scene")]
        public static void AddWorld()
        {
            ClickerPrefabBuilder.CreatePrefabs();

            var existingSlots = Object.FindFirstObjectByType<EnemySlotDirector>(FindObjectsInactive.Include);
            if (existingSlots != null)
            {
                WireOpenScene();
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.EnemyView);
            if (prefab == null)
            {
                Debug.LogError("Clicker: EnemyView prefab missing at Assets/Prefabs/EnemyView.prefab.");
                return;
            }

            var slots = new GameObject("Slots");
            Undo.RegisterCreatedObjectUndo(slots, "Create Slots");
            var director = slots.AddComponent<EnemySlotDirector>();
            var left = CreateSlot(slots.transform, "SlotLeft", new Vector3(-5.8f, -0.45f, 0f), 0.62f);
            var center = CreateSlot(slots.transform, "SlotCenter", new Vector3(-3.1f, -0.6f, 0f), 1f);
            var right = CreateSlot(slots.transform, "SlotRight", new Vector3(-0.4f, -0.45f, 0f), 0.62f);

            var a = SpawnEnemy(prefab, "EnemyA", ClickerPaths.EnemyDir + "/EnemyA.asset");
            var b = SpawnEnemy(prefab, "EnemyB", ClickerPaths.EnemyDir + "/EnemyB.asset");
            var c = SpawnEnemy(prefab, "EnemyC", ClickerPaths.EnemyDir + "/EnemyC.asset");

            var so = new SerializedObject(director);
            so.FindProperty("slotLeft").objectReferenceValue = left;
            so.FindProperty("slotCenter").objectReferenceValue = center;
            so.FindProperty("slotRight").objectReferenceValue = right;
            so.FindProperty("enemyA").objectReferenceValue = a;
            so.FindProperty("enemyB").objectReferenceValue = b;
            so.FindProperty("enemyC").objectReferenceValue = c;
            so.FindProperty("enemyPrefab").objectReferenceValue = prefab;
            so.FindProperty("duration").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();

            WireOpenScene();
        }

        static void WireSlots(EnemySlotDirector director)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.EnemyView);
            var so = new SerializedObject(director);
            so.FindProperty("enemyPrefab").objectReferenceValue = prefab;

            var a = FindEnemy("EnemyA") ?? SpawnEnemy(prefab, "EnemyA", ClickerPaths.EnemyDir + "/EnemyA.asset");
            var b = FindEnemy("EnemyB") ?? SpawnEnemy(prefab, "EnemyB", ClickerPaths.EnemyDir + "/EnemyB.asset");
            var c = FindEnemy("EnemyC") ?? SpawnEnemy(prefab, "EnemyC", ClickerPaths.EnemyDir + "/EnemyC.asset");
            so.FindProperty("enemyA").objectReferenceValue = a;
            so.FindProperty("enemyB").objectReferenceValue = b;
            so.FindProperty("enemyC").objectReferenceValue = c;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject FindNamed(string name)
        {
            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == name)
                    return transforms[i].gameObject;
            }

            return null;
        }

        static EnemyView FindEnemy(string name)
        {
            var views = Object.FindObjectsByType<EnemyView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null && views[i].gameObject.scene.IsValid() && views[i].gameObject.name == name)
                    return views[i];
            }

            return null;
        }

        static TMP_Text FindTmp(Transform root, string name)
        {
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == name)
                    return texts[i];
            }

            return texts.Length > 0 ? texts[0] : null;
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
            if (prefab == null)
                return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            Undo.RegisterCreatedObjectUndo(go, "Spawn " + name);
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
