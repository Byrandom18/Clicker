using System;
using UnityEngine;

namespace Clicker
{
    public enum UpgradeKind
    {
        Click,
        Idle
    }

    [CreateAssetMenu(menuName = "Clicker/Upgrade", fileName = "Upgrade")]
    public class UpgradeDef : ScriptableObject
    {
        public string id;
        public UpgradeKind kind;
        public string nameRu;
        public string nameEn;
        public double powerPerCopy = 1d;
        public double baseCost = 15d;
        public double costMult = 1.15d;
        public UpgradeDef requires;
        public Sprite icon;

        public string DisplayName => Loc.T(nameRu, nameEn);

        public double CostForOwned(int owned)
        {
            if (owned < 0)
                owned = 0;
            double mult = costMult <= 1d ? 1.15d : costMult;
            return baseCost * Math.Pow(mult, owned);
        }
    }
}
