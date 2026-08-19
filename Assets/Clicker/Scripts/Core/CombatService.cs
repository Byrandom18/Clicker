using System;
using YG;

namespace Clicker
{
    public class CombatService
    {
        readonly BalanceConfig _balance;
        readonly EconomyService _economy;

        public bool Locked { get; private set; }
        public bool Won => YG2.saves.gameWon;
        public int PhaseIndex => YG2.saves.phaseIndex;
        public double HpLeft => YG2.saves.hpLeft;
        public double HpMax => _balance.GetPhaseHp(PhaseIndex);
        public int ActiveEnemyIndex => PhaseIndex % 3;
        public float HpNormalized => HpMax <= 0d ? 0f : (float)(HpLeft / HpMax);

        public event Action OnChanged;
        public event Action<int> OnPhaseCleared;
        public event Action OnVictory;

        public CombatService(BalanceConfig balance, EconomyService economy)
        {
            _balance = balance;
            _economy = economy;
        }

        public void InitializeNewRun()
        {
            SaveUtil.EnsureArrays();
            YG2.saves.phaseIndex = 0;
            YG2.saves.hpLeft = _balance.GetPhaseHp(0);
            YG2.saves.pendingOverflow = 0d;
            YG2.saves.gameWon = false;
            Locked = false;
            OnChanged?.Invoke();
        }

        public void Restore()
        {
            SaveUtil.EnsureArrays();
            if (Won)
            {
                Locked = true;
            }
            else
            {
                Locked = YG2.saves.hpLeft <= 0d;
            }

            OnChanged?.Invoke();
        }

        public bool HasPendingInterlude => !Won && Locked && YG2.saves.hpLeft <= 0d;

        public static int SpriteStageForEnemy(int enemyIndex, int phaseIndex)
        {
            int stage = (phaseIndex + 2 - enemyIndex) / 3;
            if (stage < 0)
                stage = 0;
            if (stage > 3)
                stage = 3;
            return stage;
        }

        public double ApplyDamage(double amount)
        {
            if (Locked || Won || amount <= 0d)
                return 0d;

            double dealt = amount;
            if (dealt > YG2.saves.hpLeft)
            {
                YG2.saves.pendingOverflow += dealt - YG2.saves.hpLeft;
                dealt = YG2.saves.hpLeft;
            }

            YG2.saves.hpLeft -= dealt;
            if (YG2.saves.hpLeft < 0d)
                YG2.saves.hpLeft = 0d;

            _economy.AddScore(dealt);
            OnChanged?.Invoke();

            if (YG2.saves.hpLeft <= 0d)
            {
                Locked = true;
                OnPhaseCleared?.Invoke(PhaseIndex);
            }

            return dealt;
        }

        public void AdvanceAfterInterlude()
        {
            if (Won)
                return;

            int next = PhaseIndex + 1;
            if (next >= _balance.phaseCount)
            {
                YG2.saves.gameWon = true;
                YG2.saves.phaseIndex = _balance.phaseCount - 1;
                YG2.saves.hpLeft = 0d;
                Locked = true;
                OnVictory?.Invoke();
                OnChanged?.Invoke();
                return;
            }

            YG2.saves.phaseIndex = next;
            YG2.saves.hpLeft = _balance.GetPhaseHp(next);
            Locked = false;

            double overflow = YG2.saves.pendingOverflow;
            YG2.saves.pendingOverflow = 0d;
            if (overflow > 0d)
                ApplyDamage(overflow);

            OnChanged?.Invoke();
        }
    }
}
