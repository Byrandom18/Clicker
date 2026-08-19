using System;
using UnityEngine;
using YG;

namespace Clicker
{
    public class EconomyService
    {
        readonly BalanceConfig _balance;

        public double ClickPower { get; private set; }
        public double IdlePerSecond { get; private set; }
        public double Score => YG2.saves.score;

        public EconomyService(BalanceConfig balance)
        {
            _balance = balance;
            Recalc();
        }

        public void Recalc()
        {
            SaveUtil.EnsureArrays();
            double click = _balance.baseClickPower;
            double idle = 0d;
            int n = _balance.UpgradeCount;
            for (int i = 0; i < n; i++)
            {
                var clickDef = _balance.GetClick(i);
                if (clickDef != null)
                    click += YG2.saves.clickCounts[i] * clickDef.powerPerCopy;
                var idleDef = _balance.GetIdle(i);
                if (idleDef != null)
                    idle += YG2.saves.idleCounts[i] * idleDef.powerPerCopy;
            }

            ClickPower = click;
            IdlePerSecond = idle;
        }

        public void AddScore(double amount)
        {
            if (amount <= 0d)
                return;
            YG2.saves.score += amount;
        }

        public bool IsUnlocked(bool idle, int index)
        {
            if (index <= 0)
                return true;
            SaveUtil.EnsureArrays();
            int[] counts = idle ? YG2.saves.idleCounts : YG2.saves.clickCounts;
            return counts[index - 1] >= 1;
        }

        public int Owned(bool idle, int index)
        {
            SaveUtil.EnsureArrays();
            int[] counts = idle ? YG2.saves.idleCounts : YG2.saves.clickCounts;
            if (index < 0 || index >= counts.Length)
                return 0;
            return counts[index];
        }

        public UpgradeDef Def(bool idle, int index)
        {
            return idle ? _balance.GetIdle(index) : _balance.GetClick(index);
        }

        public double NextCost(bool idle, int index)
        {
            var def = Def(idle, index);
            if (def == null)
                return double.MaxValue;
            return def.CostForOwned(Owned(idle, index));
        }

        public bool CanAfford(bool idle, int index)
        {
            return IsUnlocked(idle, index) && YG2.saves.score + 0.0001d >= NextCost(idle, index);
        }

        public bool TryBuy(bool idle, int index)
        {
            if (!IsUnlocked(idle, index))
                return false;
            var def = Def(idle, index);
            if (def == null)
                return false;

            double cost = def.CostForOwned(Owned(idle, index));
            if (YG2.saves.score < cost)
                return false;

            YG2.saves.score -= cost;
            if (idle)
                YG2.saves.idleCounts[index]++;
            else
                YG2.saves.clickCounts[index]++;
            Recalc();
            return true;
        }
    }

    public static class SaveUtil
    {
        public static void EnsureArrays()
        {
            YG2.saves.clickCounts = Resize(YG2.saves.clickCounts, BalanceDefaults.UpgradeCount);
            YG2.saves.idleCounts = Resize(YG2.saves.idleCounts, BalanceDefaults.UpgradeCount);
        }

        static int[] Resize(int[] source, int length)
        {
            if (source != null && source.Length == length)
                return source;
            var next = new int[length];
            if (source != null)
                Array.Copy(source, next, Math.Min(source.Length, length));
            return next;
        }
    }
}
