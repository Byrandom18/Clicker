using UnityEngine;

namespace Clicker
{
    [CreateAssetMenu(menuName = "Clicker/Balance", fileName = "Balance")]
    public class BalanceConfig : ScriptableObject
    {
        public int phaseCount = 12;
        public double baseClickPower = 1d;
        public double simulatedClicksPerSecond = 3d;
        public float tweenDuration = 0.6f;
        public double[] phaseHp;
        public double[] targetPhaseSeconds;
        public float[] rewardedPercentByPhase;

        public double GetPhaseHp(int phase)
        {
            if (phaseHp == null || phaseHp.Length == 0)
                return 170d;
            int i = Mathf.Clamp(phase, 0, phaseHp.Length - 1);
            return phaseHp[i];
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
    }
}
