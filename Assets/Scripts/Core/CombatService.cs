using System;
using UnityEngine;
using YG;

namespace Clicker
{
    public sealed class CombatService
    {
        public const int CampaignSpriteMax = 5;
        public const int EndlessSpriteMax = 5;
        public const int CampaignStagesPerEnemy = 5;

        readonly BalanceConfig _balance;

        public int PhaseIndex => YG2.saves.phaseIndex;
        public bool IsEndless => YG2.saves.endlessMode;
        public bool IsWon => YG2.saves.gameWon && !YG2.saves.endlessMode;
        public bool HasPendingInterlude { get; private set; }
        public double HpLeft => YG2.saves.hpLeft;
        public double HpMax => GetPhaseHp(IsWon ? PhaseCount - 1 : PhaseIndex);
        public int ActiveEnemyIndex => PhaseIndex % 3;
        public int ActiveStageIndex => Mathf.Clamp(PhaseIndex / 3, 0, CampaignStagesPerEnemy - 1);
        public int PhaseCount => _balance != null && _balance.phaseCount > 0 ? _balance.phaseCount : BalanceDefaults.PhaseCount;

        public int CompletedStagesFor(int enemyIndex)
        {
            if (IsWon || IsEndless)
                return CampaignStagesPerEnemy;
            int n = 0;
            int phase = PhaseIndex;
            for (int p = 0; p < phase; p++)
            {
                if (p % 3 == enemyIndex)
                    n++;
            }

            return Mathf.Clamp(n, 0, CampaignStagesPerEnemy);
        }

        public CombatService(BalanceConfig balance)
        {
            _balance = balance;
        }

        public void InitFromSave()
        {
            if (YG2.saves.phaseIndex < 0)
                YG2.saves.phaseIndex = 0;

            if (YG2.saves.endlessMode)
            {
                YG2.saves.gameWon = true;
                if (YG2.saves.phaseIndex < PhaseCount)
                    YG2.saves.phaseIndex = PhaseCount;
                if (!YG2.saves.clickerInitialized || YG2.saves.hpLeft < 0d)
                    YG2.saves.hpLeft = GetPhaseHp(YG2.saves.phaseIndex);
                YG2.saves.clickerInitialized = true;
                HasPendingInterlude = false;
                return;
            }

            if (YG2.saves.phaseIndex >= PhaseCount)
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

        public double GetPhaseHp(int phase)
        {
            int count = PhaseCount;
            if (_balance == null)
                return 170d;
            if (phase < count)
                return _balance.GetPhaseHp(phase);

            double last = _balance.GetPhaseHp(count - 1);
            double mult = _balance.endlessHpMult > 1d ? _balance.endlessHpMult : BalanceDefaults.EndlessHpMult;
            int extra = phase - (count - 1);
            return last * Math.Pow(mult, extra);
        }

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
            int max = IsEndless ? EndlessSpriteMax : CampaignSpriteMax;
            if (!IsEndless && PhaseIndex >= PhaseCount)
                return CampaignSpriteMax;

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

            return Mathf.Clamp(stage, 0, max);
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
            if (YG2.saves.phaseIndex >= PhaseCount && !YG2.saves.endlessMode)
            {
                YG2.saves.gameWon = true;
                YG2.saves.hpLeft = 0d;
                return;
            }

            ApplyHpForCurrentPhase();
        }

        public void StartEndless()
        {
            YG2.saves.endlessMode = true;
            YG2.saves.gameWon = true;
            HasPendingInterlude = false;
            if (YG2.saves.phaseIndex < PhaseCount)
                YG2.saves.phaseIndex = PhaseCount;
            ApplyHpForCurrentPhase();
        }

        void ApplyHpForCurrentPhase()
        {
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
