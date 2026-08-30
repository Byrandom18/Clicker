using System.Collections.Generic;
using YG;

namespace Clicker
{
    public sealed class EconomyService
    {
        readonly BalanceConfig _balance;
        readonly List<UpgradeDef> _defs = new List<UpgradeDef>();

        public double ClickPower { get; private set; }
        public double IdlePerSecond { get; private set; }
        public double Score => YG2.saves.score;

        public EconomyService(BalanceConfig balance)
        {
            _balance = balance;
        }

        public void SetDefinitions(IEnumerable<UpgradeDef> defs)
        {
            _defs.Clear();
            var raw = new List<UpgradeDef>();
            if (defs != null)
            {
                foreach (var def in defs)
                {
                    if (def != null && !string.IsNullOrEmpty(def.id))
                        raw.Add(def);
                }
            }

            _defs.AddRange(ClickerCatalog.ToShopOrder(raw));
            Recalc();
        }

        public IReadOnlyList<UpgradeDef> Definitions => _defs;

        public int IndexOf(UpgradeDef def)
        {
            if (def == null)
                return -1;
            for (int i = 0; i < _defs.Count; i++)
            {
                if (_defs[i] == def || (_defs[i] != null && _defs[i].id == def.id))
                    return i;
            }

            return -1;
        }

        public int GetCount(UpgradeDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.id) || YG2.saves.upgrades == null)
                return 0;
            for (int i = 0; i < YG2.saves.upgrades.Count; i++)
            {
                var row = YG2.saves.upgrades[i];
                if (row != null && row.id == def.id)
                    return row.count;
            }

            return 0;
        }

        public bool IsUnlocked(UpgradeDef def)
        {
            int index = IndexOf(def);
            if (index < 0)
                return false;
            if (index == 0)
                return true;
            return GetCount(_defs[index - 1]) >= 1;
        }

        public double GetCost(UpgradeDef def)
        {
            if (def == null)
                return double.MaxValue;
            return def.CostForOwned(GetCount(def));
        }

        public UpgradeDef FindBestValueBuy(double clicksPerSecond)
        {
            UpgradeDef best = null;
            double bestEff = 0d;
            double cps = clicksPerSecond <= 0d ? 3d : clicksPerSecond;
            for (int i = 0; i < _defs.Count; i++)
            {
                var def = _defs[i];
                if (!CanAfford(def))
                    continue;
                double cost = GetCost(def);
                if (cost <= 0d)
                    continue;
                double gain = def.kind == UpgradeKind.Idle
                    ? def.powerPerCopy
                    : def.powerPerCopy * cps;
                double eff = gain / cost;
                if (eff > bestEff)
                {
                    bestEff = eff;
                    best = def;
                }
            }

            return best;
        }

        public bool CanAfford(UpgradeDef def)
        {
            return def != null && IsUnlocked(def) && YG2.saves.score >= GetCost(def);
        }

        public bool TryBuy(UpgradeDef def)
        {
            if (def == null || !IsUnlocked(def))
                return false;

            double cost = GetCost(def);
            if (YG2.saves.score < cost)
                return false;

            YG2.saves.score -= cost;
            AddCount(def, 1);
            Recalc();
            return true;
        }

        public void AddIncome(double amount)
        {
            if (amount <= 0d)
                return;
            YG2.saves.score += amount;
        }

        public void Recalc()
        {
            EnsureSaveList();
            double click = _balance != null ? _balance.baseClickPower : 1d;
            double idle = 0d;
            for (int i = 0; i < _defs.Count; i++)
            {
                var def = _defs[i];
                int n = GetCount(def);
                if (n <= 0)
                    continue;
                if (def.kind == UpgradeKind.Idle)
                    idle += n * def.powerPerCopy;
                else
                    click += n * def.powerPerCopy;
            }

            ClickPower = click;
            IdlePerSecond = idle;
        }

        void AddCount(UpgradeDef def, int delta)
        {
            EnsureSaveList();
            for (int i = 0; i < YG2.saves.upgrades.Count; i++)
            {
                var row = YG2.saves.upgrades[i];
                if (row != null && row.id == def.id)
                {
                    row.count += delta;
                    return;
                }
            }

            YG2.saves.upgrades.Add(new UpgradeSave { id = def.id, count = delta });
        }

        static void EnsureSaveList()
        {
            if (YG2.saves.upgrades == null)
                YG2.saves.upgrades = new List<UpgradeSave>();
        }
    }
}
