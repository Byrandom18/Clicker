using System.Collections.Generic;
using UnityEngine;

namespace Clicker
{
    public static class ClickerCatalog
    {
        const string BalancePath = "Data/Balance";
        const string DialogsPath = "Data/Dialogs";
        const string UpgradesPath = "Data/Upgrades";
        const string EnemiesPath = "Data/Enemies";
        const string EnemyPrefabPath = "Prefabs/EnemyView";
        const string ClickHitVfxPath = "Prefabs/ClickHit";
        const string RewardedHitVfxPath = "Prefabs/RewardedHit";

        public static BalanceConfig LoadBalance()
        {
            return Resources.Load<BalanceConfig>(BalancePath);
        }

        public static DialogCatalog LoadDialogs()
        {
            return Resources.Load<DialogCatalog>(DialogsPath);
        }

        public static GameObject LoadEnemyPrefab()
        {
            return Resources.Load<GameObject>(EnemyPrefabPath);
        }

        public static GameObject LoadClickHitVfx()
        {
            return Resources.Load<GameObject>(ClickHitVfxPath);
        }

        public static GameObject LoadRewardedHitVfx()
        {
            return Resources.Load<GameObject>(RewardedHitVfxPath);
        }

        public static UpgradeDef[] LoadUpgrades()
        {
            var loaded = Resources.LoadAll<UpgradeDef>(UpgradesPath);
            var list = new List<UpgradeDef>(loaded != null ? loaded.Length : 0);
            if (loaded != null)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null && !string.IsNullOrEmpty(loaded[i].id))
                        list.Add(loaded[i]);
                }
            }

            list.Sort(CompareById);
            return ToShopOrder(list).ToArray();
        }

        public static List<UpgradeDef> ToShopOrder(IList<UpgradeDef> source)
        {
            var click = new List<UpgradeDef>();
            var idle = new List<UpgradeDef>();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    var def = source[i];
                    if (def == null)
                        continue;
                    if (def.kind == UpgradeKind.Idle)
                        idle.Add(def);
                    else
                        click.Add(def);
                }
            }

            click.Sort(CompareById);
            idle.Sort(CompareById);

            var mixed = new List<UpgradeDef>(click.Count + idle.Count);
            int n = Mathf.Max(click.Count, idle.Count);
            for (int i = 0; i < n; i++)
            {
                if (i < click.Count)
                    mixed.Add(click[i]);
                if (i < idle.Count)
                    mixed.Add(idle[i]);
            }

            return mixed;
        }

        public static EnemyDef[] LoadEnemies()
        {
            var loaded = Resources.LoadAll<EnemyDef>(EnemiesPath);
            var list = new List<EnemyDef>(loaded != null ? loaded.Length : 0);
            if (loaded != null)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null)
                        list.Add(loaded[i]);
                }
            }

            list.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            return list.ToArray();
        }

        public static EnemyDef FindEnemy(string id)
        {
            var all = LoadEnemies();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].id == id)
                    return all[i];
            }

            return all.Length > 0 ? all[0] : null;
        }

        static int CompareById(UpgradeDef a, UpgradeDef b)
        {
            string idA = a != null ? a.id : string.Empty;
            string idB = b != null ? b.id : string.Empty;
            return string.CompareOrdinal(idA, idB);
        }
    }
}
