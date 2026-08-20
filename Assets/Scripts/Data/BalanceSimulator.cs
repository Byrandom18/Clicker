using System;
using UnityEngine;

namespace Clicker
{
    public static class BalanceSimulator
    {
        public struct PhaseReport
        {
            public double seconds;
            public double targetSeconds;
            public double dpsAtEnd;
            public double clickPower;
            public double idlePower;
        }

        public struct Report
        {
            public PhaseReport[] phases;
            public double totalSeconds;
            public double targetTotal;
            public int[] shopOwned;
        }

        public static Report Run(BalanceConfig config, double clicksPerSecond = 3d, double dt = 0.25d)
        {
            if (config == null)
                config = BalanceDefaults.CreateBalance();

            int n = Mathf.Max(1, config.phaseCount);
            int shopN = Mathf.Max(1, config.ShopCount);
            var owned = new int[shopN];
            double score = 0d;
            double clickPower = config.baseClickPower;
            double idlePower = 0d;
            double time = 0d;
            var phases = new PhaseReport[n];
            Recalc(config, owned, out clickPower, out idlePower);

            for (int phase = 0; phase < n; phase++)
            {
                double hp = config.GetPhaseHp(phase);
                double t0 = time;
                int guard = 0;
                while (hp > 0d && guard < 2_000_000)
                {
                    double dps = clickPower * clicksPerSecond + idlePower;
                    if (dps < 0.0001d)
                        dps = 0.0001d;
                    double earned = dps * dt;
                    score += earned;
                    hp -= earned;
                    time += dt;
                    TryBuys(config, clicksPerSecond, phase, ref score, owned, ref clickPower, ref idlePower);
                    guard++;
                }

                phases[phase] = new PhaseReport
                {
                    seconds = time - t0,
                    targetSeconds = config.GetTargetSeconds(phase),
                    dpsAtEnd = clickPower * clicksPerSecond + idlePower,
                    clickPower = clickPower,
                    idlePower = idlePower
                };
            }

            double targetTotal = 0d;
            if (config.targetPhaseSeconds != null)
            {
                for (int i = 0; i < config.targetPhaseSeconds.Length; i++)
                    targetTotal += config.targetPhaseSeconds[i];
            }

            return new Report
            {
                phases = phases,
                totalSeconds = time,
                targetTotal = targetTotal,
                shopOwned = owned
            };
        }

        static void Recalc(BalanceConfig config, int[] owned, out double clickPower, out double idlePower)
        {
            clickPower = config.baseClickPower;
            idlePower = 0d;
            for (int i = 0; i < owned.Length; i++)
            {
                var def = config.GetShop(i);
                if (def == null)
                    continue;
                if (def.isIdle)
                    idlePower += owned[i] * def.powerPerCopy;
                else
                    clickPower += owned[i] * def.powerPerCopy;
            }
        }

        static bool Unlocked(int shopIndex, int phase) => shopIndex / 2 <= phase;

        static bool Maxed(UpgradeDef def, int ownedCount)
        {
            return def != null && ownedCount >= def.MaxCopies;
        }

        static void TryBuys(
            BalanceConfig config,
            double cps,
            int phase,
            ref double score,
            int[] owned,
            ref double clickPower,
            ref double idlePower)
        {
            for (int n = 0; n < 48; n++)
            {
                int nextNew = -1;
                for (int i = 0; i < owned.Length; i++)
                {
                    if (Unlocked(i, phase) && owned[i] == 0)
                    {
                        nextNew = i;
                        break;
                    }
                }

                if (nextNew >= 0)
                {
                    var def = config.GetShop(nextNew);
                    if (def != null && !Maxed(def, owned[nextNew]))
                    {
                        double cost = def.CostForOwned(owned[nextNew]);
                        if (score >= cost)
                        {
                            score -= cost;
                            owned[nextNew]++;
                            Recalc(config, owned, out clickPower, out idlePower);
                            continue;
                        }
                    }
                }

                int best = -1;
                double bestCost = 0d;
                double bestEff = 0d;
                for (int i = 0; i < owned.Length; i++)
                {
                    if (!Unlocked(i, phase))
                        continue;
                    var def = config.GetShop(i);
                    if (def == null || Maxed(def, owned[i]))
                        continue;
                    double cost = def.CostForOwned(owned[i]);
                    if (score < cost)
                        continue;
                    double dpsGain = def.isIdle ? def.powerPerCopy : def.powerPerCopy * cps;
                    double eff = dpsGain / cost;
                    if (eff > bestEff)
                    {
                        bestEff = eff;
                        best = i;
                        bestCost = cost;
                    }
                }

                if (best < 0)
                    break;

                score -= bestCost;
                owned[best]++;
                Recalc(config, owned, out clickPower, out idlePower);
            }
        }

        public static void FitHpBase(BalanceConfig config, double clicksPerSecond = 3d)
        {
            if (config == null || config.targetPhaseSeconds == null || config.targetPhaseSeconds.Length == 0)
                return;

            double target = 0d;
            for (int i = 0; i < config.targetPhaseSeconds.Length; i++)
                target += config.targetPhaseSeconds[i];
            if (target <= 0d)
                return;

            double lo = Math.Max(1d, config.hpBase / 1000d);
            double hi = config.hpBase * 1000d;
            for (int i = 0; i < 28; i++)
            {
                double mid = Math.Sqrt(lo * hi);
                config.hpBase = mid;
                var report = Run(config, clicksPerSecond);
                if (report.totalSeconds < target)
                    lo = mid;
                else
                    hi = mid;
            }

            config.hpBase = Math.Sqrt(lo * hi);
        }
    }
}
