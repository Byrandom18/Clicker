using UnityEngine;
using YG;

namespace Clicker
{
    public sealed class CombatService
    {
        readonly BalanceConfig _balance;

        public int PhaseIndex => YG2.saves.phaseIndex;
        public bool IsWon => YG2.saves.gameWon;
        public bool HasPendingInterlude { get; private set; }
        public double HpLeft => YG2.saves.hpLeft;
        public double HpMax => _balance != null ? _balance.GetPhaseHp(PhaseIndex) : 1d;
        public int ActiveEnemyIndex => PhaseIndex % 3;
        public int PhaseCount => _balance != null && _balance.phaseCount > 0 ? _balance.phaseCount : BalanceDefaults.PhaseCount;

        public CombatService(BalanceConfig balance)
        {
            _balance = balance;
        }

        public void InitFromSave()
        {
            int count = PhaseCount;
            if (YG2.saves.phaseIndex < 0)
                YG2.saves.phaseIndex = 0;
            if (YG2.saves.phaseIndex >= count)
            {
                YG2.saves.gameWon = true;
                YG2.saves.hpLeft = 0d;
                return;
            }

            if (!YG2.saves.clickerInitialized || YG2.saves.hpLeft < 0d)
            {
                YG2.saves.hpLeft = GetPhaseHp(YG2.saves.phaseIndex);
                YG2.saves.clickerInitialized = true;
            }

            HasPendingInterlude = false;
        }

        public double GetPhaseHp(int phase) => _balance != null ? _balance.GetPhaseHp(phase) : 170d;

        public float HpFill01
        {
            get
            {
                double max = HpMax;
                if (max <= 0d)
                    return 0f;
                return Mathf.Clamp01((float)(HpLeft / max));
            }
        }

        public int GetSpriteIndex(int enemyIndex, bool afterCurrentKill)
        {
            int stage;
            if (enemyIndex == ActiveEnemyIndex)
            {
                stage = PhaseIndex / 3;
                if (afterCurrentKill)
                    stage += 1;
            }
            else
            {
                int completed = 0;
                for (int p = 0; p < PhaseIndex; p++)
                {
                    if (p % 3 == enemyIndex)
                        completed++;
                }

                stage = completed;
            }

            return Mathf.Clamp(stage, 0, 3);
        }

        public bool ApplyDamage(double amount)
        {
            if (amount <= 0d || IsWon || HasPendingInterlude)
                return false;

            YG2.saves.hpLeft -= amount;
            if (YG2.saves.hpLeft > 0d)
                return false;

            YG2.saves.pendingOverflow = -YG2.saves.hpLeft;
            YG2.saves.hpLeft = 0d;
            HasPendingInterlude = true;
            return true;
        }

        public void AdvanceAfterInterlude()
        {
            HasPendingInterlude = false;
            if (IsWon)
                return;

            YG2.saves.phaseIndex++;
            if (YG2.saves.phaseIndex >= PhaseCount)
            {
                YG2.saves.gameWon = true;
                YG2.saves.hpLeft = 0d;
                YG2.saves.pendingOverflow = 0d;
                return;
            }

            double hp = GetPhaseHp(YG2.saves.phaseIndex);
            double overflow = YG2.saves.pendingOverflow;
            YG2.saves.pendingOverflow = 0d;
            if (overflow <= 0d)
            {
                YG2.saves.hpLeft = hp;
                return;
            }

            if (overflow < hp)
            {
                YG2.saves.hpLeft = hp - overflow;
                return;
            }

            YG2.saves.hpLeft = 0d;
            YG2.saves.pendingOverflow = overflow - hp;
            HasPendingInterlude = true;
        }
    }
}
