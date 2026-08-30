using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class ShopView : MonoBehaviour
    {
        [SerializeField] Transform shopRoot;
        [SerializeField] float scrollEdgeClip = 10f;

        ShopRowView[] _rows;

        void Awake()
        {
            EnsureViewportClip();
        }

        public ShopRowView[] Rows
        {
            get
            {
                if (_rows == null)
                    Collect();
                return _rows;
            }
        }

        public void Collect()
        {
            Transform root = shopRoot != null ? shopRoot : transform;
            var found = root.GetComponentsInChildren<ShopRowView>(true);
            FillMissingDefinitions(found);
            ApplyAlternatingOrder(found);
            _rows = root.GetComponentsInChildren<ShopRowView>(true);
            EnsureViewportClip();
            var content = root as RectTransform;
            if (content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        void EnsureViewportClip()
        {
            var scroll = GetComponentInChildren<ScrollRect>(true);
            if (scroll == null)
                return;
            RectTransform viewport = scroll.viewport;
            if (viewport == null)
                return;

            var mask = viewport.GetComponent<RectMask2D>();
            if (mask == null)
                mask = viewport.gameObject.AddComponent<RectMask2D>();

            float inset = Mathf.Max(0f, scrollEdgeClip);
            mask.padding = new Vector4(0f, inset, 0f, inset);
        }

        static void FillMissingDefinitions(ShopRowView[] rows)
        {
            if (rows == null || rows.Length == 0)
                return;

            bool anyMissing = false;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] != null && rows[i].Definition == null)
                {
                    anyMissing = true;
                    break;
                }
            }

            if (!anyMissing)
                return;

            var defs = new List<UpgradeDef>(ClickerCatalog.LoadUpgrades());
            int defIndex = 0;
            for (int i = 0; i < rows.Length && defIndex < defs.Count; i++)
            {
                if (rows[i] == null)
                    continue;
                if (rows[i].Definition == null)
                {
                    rows[i].SetDefinition(defs[defIndex]);
                    defIndex++;
                }
            }
        }

        public ShopRowView FindRow(UpgradeDef def)
        {
            var rows = Rows;
            if (rows == null || def == null)
                return null;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null || row.Definition == null)
                    continue;
                if (row.Definition == def || row.Definition.id == def.id)
                    return row;
            }

            return null;
        }

        public void Refresh(EconomyService economy)
        {
            var rows = Rows;
            if (rows == null)
                return;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] != null)
                    rows[i].Bind(economy);
            }
        }

        static void ApplyAlternatingOrder(ShopRowView[] rows)
        {
            if (rows == null || rows.Length == 0)
                return;

            var click = new List<ShopRowView>();
            var idle = new List<ShopRowView>();
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null || row.Definition == null)
                    continue;
                if (row.Definition.kind == UpgradeKind.Idle)
                    idle.Add(row);
                else
                    click.Add(row);
            }

            click.Sort(CompareById);
            idle.Sort(CompareById);

            int sibling = 0;
            int n = Mathf.Max(click.Count, idle.Count);
            for (int i = 0; i < n; i++)
            {
                if (i < click.Count)
                    click[i].transform.SetSiblingIndex(sibling++);
                if (i < idle.Count)
                    idle[i].transform.SetSiblingIndex(sibling++);
            }
        }

        static int CompareById(ShopRowView a, ShopRowView b)
        {
            string idA = a != null && a.Definition != null ? a.Definition.id : string.Empty;
            string idB = b != null && b.Definition != null ? b.Definition.id : string.Empty;
            return string.CompareOrdinal(idA, idB);
        }
    }
}
