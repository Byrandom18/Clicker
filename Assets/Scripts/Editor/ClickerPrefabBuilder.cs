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
        const string PrefabDir = "Assets/Clicker/Prefabs";

        [MenuItem("Clicker/Create Prefabs")]
        public static void CreatePrefabs()
        {
            ClickerAssetMenu.CreateDefaultData();
            if (!AssetDatabase.IsValidFolder("Assets/Clicker"))
                AssetDatabase.CreateFolder("Assets", "Clicker");
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets/Clicker", "Prefabs");

            CreateEnemyPrefab();
            CreateShopRowPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Clicker: prefabs saved to Assets/Clicker/Prefabs. Duplicate ShopRow into ClickList/IdleList and assign UpgradeDef on each row.");
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
            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/EnemyView.prefab");
            Object.DestroyImmediate(root);
        }

        static void CreateShopRowPrefab()
        {
            var root = new GameObject("ShopRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.sizeDelta = new Vector2(0f, 88f);
            rootRt.anchoredPosition = Vector2.zero;
            var bg = root.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.13f, 0.16f, 0.95f);

            var icon = CreateUi("Icon", root.transform, new Vector2(8, 12), new Vector2(72, 76));
            var iconImg = icon.AddComponent<Image>();
            iconImg.color = Color.white;

            var title = CreateTmp("Title", root.transform, new Vector2(84, 48), new Vector2(280, 80), 22, TextAlignmentOptions.MidlineLeft);
            var power = CreateTmp("Power", root.transform, new Vector2(84, 16), new Vector2(220, 46), 16, TextAlignmentOptions.MidlineLeft);
            var owned = CreateTmp("Owned", root.transform, new Vector2(300, 16), new Vector2(380, 46), 16, TextAlignmentOptions.MidlineLeft);
            var cost = CreateTmp("Cost", root.transform, new Vector2(390, 48), new Vector2(500, 80), 18, TextAlignmentOptions.MidlineRight);

            var buyGo = CreateUi("Buy", root.transform, new Vector2(-132, 16), new Vector2(-12, 72));
            var buyRt = buyGo.GetComponent<RectTransform>();
            StretchRight(buyRt, 12f, 16f, 132f, 16f);
            var buyImg = buyGo.AddComponent<Image>();
            buyImg.color = new Color(0.22f, 0.45f, 0.28f, 1f);
            var buy = buyGo.AddComponent<Button>();
            buy.targetGraphic = buyImg;
            var buyLabel = CreateTmp("Label", buyGo.transform, Vector2.zero, Vector2.zero, 20, TextAlignmentOptions.Center);
            StretchFull(buyLabel.rectTransform);

            var row = root.AddComponent<ShopRowView>();
            var so = new SerializedObject(row);
            so.FindProperty("icon").objectReferenceValue = iconImg;
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("power").objectReferenceValue = power;
            so.FindProperty("owned").objectReferenceValue = owned;
            so.FindProperty("cost").objectReferenceValue = cost;
            so.FindProperty("buy").objectReferenceValue = buy;
            so.FindProperty("buyLabel").objectReferenceValue = buyLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            var le = root.AddComponent<LayoutElement>();
            le.minHeight = 88f;
            le.preferredHeight = 88f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/ShopRow.prefab");
            Object.DestroyImmediate(root);
        }

        static GameObject CreateUi(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.offsetMin = min;
            rt.offsetMax = max;
            return go;
        }

        static TextMeshProUGUI CreateTmp(string name, Transform parent, Vector2 min, Vector2 max, float size, TextAlignmentOptions align)
        {
            var go = CreateUi(name, parent, min, max);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.text = name;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void StretchRight(RectTransform rt, float leftFromRight, float bottom, float width, float top)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(-leftFromRight - width, bottom);
            rt.offsetMax = new Vector2(-leftFromRight, -top);
        }
    }
}
#endif
