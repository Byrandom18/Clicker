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
        public UpgradeDef[] clickUpgrades;
        public UpgradeDef[] idleUpgrades;

        public int UpgradeCount => 12;

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

        public UpgradeDef GetClick(int index)
        {
            if (clickUpgrades == null || index < 0 || index >= clickUpgrades.Length)
                return null;
            return clickUpgrades[index];
        }

        public UpgradeDef GetIdle(int index)
        {
            if (idleUpgrades == null || index < 0 || index >= idleUpgrades.Length)
                return null;
            return idleUpgrades[index];
        }
    }
}
