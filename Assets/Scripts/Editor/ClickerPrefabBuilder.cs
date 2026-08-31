#if UNITY_EDITOR
using Clicker;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker.EditorTools
{
    public static class ClickerPrefabBuilder
    {
        [MenuItem("Clicker/Create Prefabs")]
        public static void CreatePrefabs()
        {
            ClickerAssetMenu.CreateDefaultData();
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(ClickerPaths.ResourcesPrefabDir))
                AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.EnemyView) == null)
                CreateEnemyPrefab();
            else
                Debug.Log("Clicker: keep existing Assets/Prefabs/EnemyView.prefab (scene instances depend on its fileIDs).");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.ShopRow) == null)
                CreateShopRowPrefab();
            else
                Debug.Log("Clicker: keep existing Assets/Prefabs/ShopRow.prefab (scene instances depend on its fileIDs).");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.DamagePopup) == null)
                CreateDamagePopupPrefab();
            else
                Debug.Log("Clicker: keep existing Assets/Prefabs/DamagePopup.prefab.");

            SyncEnemyPrefabToResources();
            SyncHitVfxToResources();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Clicker: prefabs at Assets/Prefabs, data at Assets/Resources/Data.");
        }

        static void SyncEnemyPrefabToResources()
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.EnemyView);
            if (src == null)
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ClickerPaths.EnemyViewResources) != null)
                AssetDatabase.DeleteAsset(ClickerPaths.EnemyViewResources);
            AssetDatabase.CopyAsset(ClickerPaths.EnemyView, ClickerPaths.EnemyViewResources);
        }

        static void SyncHitVfxToResources()
        {
            CopyToResources(ClickerPaths.ClickHitVfx, ClickerPaths.ResourcesPrefabDir + "/ClickHit.prefab");
            CopyToResources(ClickerPaths.RewardedHitVfx, ClickerPaths.ResourcesPrefabDir + "/RewardedHit.prefab");
        }

        static void CopyToResources(string src, string dst)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(src) == null)
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) != null)
                AssetDatabase.DeleteAsset(dst);
            AssetDatabase.CopyAsset(src, dst);
        }

        static void CreateEnemyPrefab()
        {
            var root = new GameObject("EnemyView");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 5;
            var head = new GameObject("HeadAnchor");
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var view = root.AddComponent<EnemyView>();
            var so = new SerializedObject(view);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("headAnchor").objectReferenceValue = head.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ClickerPaths.EnemyView);
            Object.DestroyImmediate(root);
        }

        static void CreateShopRowPrefab()
        {
            var root = new GameObject("ShopRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.layer = 5;
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.sizeDelta = new Vector2(0f, 88f);
            rootRt.anchoredPosition = Vector2.zero;

            var bg = root.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.13f, 0.16f, 0.95f);

            var h = root.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 8, 8);
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var rootLe = root.AddComponent<LayoutElement>();
            rootLe.minHeight = 88f;
            rootLe.preferredHeight = 88f;
            rootLe.flexibleWidth = 1f;

            var iconGo = CreateChild(root.transform, "Icon");
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white;
            AddLayout(iconGo, 64f, 64f, 64f, 64f, 0f);

            var title = CreateTmp(root.transform, "Title", 22, TextAlignmentOptions.MidlineLeft);
            AddLayout(title.gameObject, 80f, 32f, 140f, 32f, 1f);

            var power = CreateTmp(root.transform, "Power", 16, TextAlignmentOptions.MidlineLeft);
            AddLayout(power.gameObject, 64f, 28f, 90f, 28f, 0f);

            var owned = CreateTmp(root.transform, "Owned", 16, TextAlignmentOptions.MidlineLeft);
            AddLayout(owned.gameObject, 40f, 28f, 56f, 28f, 0f);

            var cost = CreateTmp(root.transform, "Cost", 18, TextAlignmentOptions.MidlineRight);
            AddLayout(cost.gameObject, 64f, 28f, 88f, 28f, 0f);

            var buyGo = CreateChild(root.transform, "Buy");
            var buyImg = buyGo.AddComponent<Image>();
            buyImg.color = new Color(0.22f, 0.45f, 0.28f, 1f);
            var buy = buyGo.AddComponent<Button>();
            buy.targetGraphic = buyImg;
            AddLayout(buyGo, 120f, 44f, 132f, 56f, 0f);

            var buyLabel = CreateTmp(buyGo.transform, "Label", 20, TextAlignmentOptions.Center);
            StretchFull(buyLabel.rectTransform);
            var labelLe = buyLabel.gameObject.AddComponent<LayoutElement>();
            labelLe.ignoreLayout = true;

            var lockGo = CreateChild(root.transform, "LockOverlay");
            var lockImg = lockGo.AddComponent<Image>();
            lockImg.color = new Color(0.02f, 0.02f, 0.04f, 0.78f);
            var lockLe = lockGo.AddComponent<LayoutElement>();
            lockLe.ignoreLayout = true;
            StretchFull(lockGo.GetComponent<RectTransform>());
            lockGo.SetActive(false);

            var lockLabel = CreateTmp(lockGo.transform, "LockLabel", 22, TextAlignmentOptions.Center);
            StretchFull(lockLabel.rectTransform);
            lockLabel.enableAutoSizing = true;
            lockLabel.fontSizeMin = 12f;
            lockLabel.fontSizeMax = 26f;
            lockLabel.fontStyle = FontStyles.Bold;
            lockLabel.textWrappingMode = TextWrappingModes.Normal;
            lockLabel.text = "Разблокируйте предыдущее";

            var row = root.AddComponent<ShopRowView>();
            var so = new SerializedObject(row);
            so.FindProperty("icon").objectReferenceValue = iconImg;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("power").objectReferenceValue = power;
            so.FindProperty("owned").objectReferenceValue = owned;
            so.FindProperty("cost").objectReferenceValue = cost;
            so.FindProperty("buy").objectReferenceValue = buy;
            so.FindProperty("buyLabel").objectReferenceValue = buyLabel;
            so.FindProperty("lockOverlay").objectReferenceValue = lockGo;
            so.FindProperty("lockLabel").objectReferenceValue = lockLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ClickerPaths.ShopRow);
            Object.DestroyImmediate(root);
        }

        static void CreateDamagePopupPrefab()
        {
            var popup = DamagePopup.Create(null, null, 40f);
            popup.gameObject.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(popup.gameObject, ClickerPaths.DamagePopup);
            Object.DestroyImmediate(popup.gameObject);
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        static TextMeshProUGUI CreateTmp(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            var go = CreateChild(parent, name);
            go.AddComponent<CanvasRenderer>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.text = name;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        static void AddLayout(GameObject go, float minW, float minH, float prefW, float prefH, float flexW)
        {
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = minW;
            le.minHeight = minH;
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
#endif
