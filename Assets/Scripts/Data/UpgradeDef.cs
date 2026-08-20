using System;
using UnityEngine;

namespace Clicker
{
    [Serializable]
    public class UpgradeDef
    {
        public string id;
        public string nameRu;
        public string nameEn;
        public bool isIdle;
        public double powerPerCopy;
        public double baseCost;
        public double costMult = 1.18d;
        [Tooltip("0 = без лимита. 12 копий хватает, чтобы золото шло в новые улучшения, а не в сотни копий первой.")]
        public int maxCopies = 12;

        public string DisplayName => Loc.T(nameRu, nameEn);
        public int MaxCopies => maxCopies <= 0 ? int.MaxValue : maxCopies;

        public double CostForOwned(int owned)
        {
            if (owned < 0)
                owned = 0;
            double mult = costMult <= 1d ? 1.18d : costMult;
            return baseCost * Math.Pow(mult, owned);
        }
    }
}
