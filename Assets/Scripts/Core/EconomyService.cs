using System;
using YG;

namespace Clicker
{
    public class EconomyService
    {
        readonly BalanceConfig _balance;

        public double ClickPower { get; private set; }
        public double IdlePerSecond { get; private set; }
        public double Score => YG2.saves.score;
        public int ShopCount => _balance.ShopCount;

        public EconomyService(BalanceConfig balance)
        {
            _balance = balance;
            Recalc();
        }

        public void Recalc()
        {
            SaveUtil.EnsureArrays(_balance.ShopCount);
            double click = _balance.baseClickPower;
            double idle = 0d;
            int n = _balance.ShopCount;
            for (int i = 0; i < n; i++)
            {
                var def = _balance.GetShop(i);
                if (def == null)
                    continue;
                int owned = YG2.saves.shopCounts[i];
                if (def.isIdle)
                    idle += owned * def.powerPerCopy;
                else
                    click += owned * def.powerPerCopy;
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

        public UpgradeDef Def(int shopIndex) => _balance.GetShop(shopIndex);

        public bool IsUnlocked(int shopIndex)
        {
            if (shopIndex < 0 || shopIndex >= ShopCount)
                return false;
            return shopIndex / 2 <= YG2.saves.phaseIndex;
        }

        public bool IsMaxed(int shopIndex)
        {
            var def = Def(shopIndex);
            if (def == null)
                return true;
            return Owned(shopIndex) >= def.MaxCopies;
        }

        public int Owned(int shopIndex)
        {
            SaveUtil.EnsureArrays(_balance.ShopCount);
            if (shopIndex < 0 || shopIndex >= YG2.saves.shopCounts.Length)
                return 0;
            return YG2.saves.shopCounts[shopIndex];
        }

        public double NextCost(int shopIndex)
        {
            var def = Def(shopIndex);
            if (def == null)
                return double.MaxValue;
            return def.CostForOwned(Owned(shopIndex));
        }

        public bool CanAfford(int shopIndex)
        {
            return IsUnlocked(shopIndex) && !IsMaxed(shopIndex) && YG2.saves.score + 0.0001d >= NextCost(shopIndex);
        }

        public bool TryBuy(int shopIndex)
        {
            if (!IsUnlocked(shopIndex) || IsMaxed(shopIndex))
                return false;
            var def = Def(shopIndex);
            if (def == null)
                return false;

            double cost = def.CostForOwned(Owned(shopIndex));
            if (YG2.saves.score < cost)
                return false;

            YG2.saves.score -= cost;
            YG2.saves.shopCounts[shopIndex]++;
            Recalc();
            return true;
        }
    }

    public static class SaveUtil
    {
        public static void EnsureArrays(int shopCount = BalanceDefaults.ShopCount)
        {
            YG2.saves.shopCounts = Resize(YG2.saves.shopCounts, shopCount);
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
