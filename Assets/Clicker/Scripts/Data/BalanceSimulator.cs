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
            public int[] clickOwned;
            public int[] idleOwned;
        }

        public static Report Run(BalanceConfig config, double clicksPerSecond = 3d, double dt = 0.25d)
        {
            if (config == null)
                config = BalanceDefaults.CreateBalance();

            int n = Mathf.Max(1, config.phaseCount);
            var clickOwned = new int[config.UpgradeCount];
            var idleOwned = new int[config.UpgradeCount];
            double score = 0d;
            double clickPower = config.baseClickPower;
            double idlePower = 0d;
            double time = 0d;

            var phases = new PhaseReport[n];

            Recalc(config, clickOwned, idleOwned, out clickPower, out idlePower);

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
                    TryBuys(config, clicksPerSecond, ref score, clickOwned, idleOwned, ref clickPower, ref idlePower);
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
                clickOwned = clickOwned,
                idleOwned = idleOwned
            };
        }

        static void Recalc(BalanceConfig config, int[] clickOwned, int[] idleOwned, out double clickPower, out double idlePower)
        {
            clickPower = config.baseClickPower;
            idlePower = 0d;
            int count = config.UpgradeCount;
            for (int i = 0; i < count; i++)
            {
                var click = config.GetClick(i);
                if (click != null)
                    clickPower += clickOwned[i] * click.powerPerCopy;
                var idle = config.GetIdle(i);
                if (idle != null)
                    idlePower += idleOwned[i] * idle.powerPerCopy;
            }
        }

        static void TryBuys(
            BalanceConfig config,
            double cps,
            ref double score,
            int[] clickOwned,
            int[] idleOwned,
            ref double clickPower,
            ref double idlePower)
        {
            for (int n = 0; n < 48; n++)
            {
                int bestKind = -1;
                int bestIndex = -1;
                double bestCost = 0d;
                double bestEff = 0d;

                for (int i = 0; i < config.UpgradeCount; i++)
                {
                    if (i > 0 && clickOwned[i - 1] < 1)
                        break;
                    var def = config.GetClick(i);
                    if (def == null)
                        continue;
                    double cost = def.CostForOwned(clickOwned[i]);
                    if (score < cost)
                        continue;
                    double eff = def.powerPerCopy * cps / cost;
                    if (eff > bestEff)
                    {
                        bestEff = eff;
                        bestKind = 0;
                        bestIndex = i;
                        bestCost = cost;
                    }
                }

                for (int i = 0; i < config.UpgradeCount; i++)
                {
                    if (i > 0 && idleOwned[i - 1] < 1)
                        break;
                    var def = config.GetIdle(i);
                    if (def == null)
                        continue;
                    double cost = def.CostForOwned(idleOwned[i]);
                    if (score < cost)
                        continue;
                    double eff = def.powerPerCopy / cost;
                    if (eff > bestEff)
                    {
                        bestEff = eff;
                        bestKind = 1;
                        bestIndex = i;
                        bestCost = cost;
                    }
                }

                if (bestKind < 0)
                    break;

                score -= bestCost;
                if (bestKind == 0)
                    clickOwned[bestIndex]++;
                else
                    idleOwned[bestIndex]++;
                Recalc(config, clickOwned, idleOwned, out clickPower, out idlePower);
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
