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
        public double powerPerCopy;
        public double baseCost;
        public double costMult = 1.15d;

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
