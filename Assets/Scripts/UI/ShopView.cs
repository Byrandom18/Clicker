using UnityEngine;

namespace Clicker
{
    public class ShopView : MonoBehaviour
    {
        [SerializeField] Transform shopRoot;

        ShopRowView[] _rows;

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
            _rows = root.GetComponentsInChildren<ShopRowView>(true);
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
    }
}
