using System;
using UnityEngine;

namespace Clicker
{
    [CreateAssetMenu(menuName = "Clicker/Balance Config", fileName = "BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        public int phaseCount = 12;
        public double hpBase = 120d;
        public double hpGrowth = 5d;
        public double baseClickPower = 1d;
        public double simulatedClicksPerSecond = 3d;
        public double[] targetPhaseSeconds;
        public float[] rewardedPercentByPhase;
        [Tooltip("Единый магазин: клик, пассив, клик, пассив...")]
        public UpgradeDef[] shop;

        public int ShopCount => shop != null ? shop.Length : 0;

        public double GetPhaseHp(int phase)
        {
            if (phase < 0)
                phase = 0;
            return hpBase * Math.Pow(hpGrowth, phase);
        }

        public float GetRewardedPercent(int phase)
        {
            if (rewardedPercentByPhase == null || rewardedPercentByPhase.Length == 0)
                return 0.1f;
            int i = Mathf.Clamp(phase, 0, rewardedPercentByPhase.Length - 1);
            return rewardedPercentByPhase[i];
        }

        public double GetTargetSeconds(int phase)
        {
            if (targetPhaseSeconds == null || phase < 0 || phase >= targetPhaseSeconds.Length)
                return 0d;
            return targetPhaseSeconds[phase];
        }

        public UpgradeDef GetShop(int index)
        {
            if (shop == null || index < 0 || index >= shop.Length)
                return null;
            return shop[index];
        }
    }
}
