using System;
using System.Collections.Generic;

namespace Clicker
{
    public sealed class BalanceSimReport
    {
        public double[] phaseSeconds;
        public double[] phaseHp;
        public double[] avgDps;
        public double totalSeconds;
        public string summary;
    }

    public static class BalanceSimulator
    {
        public static BalanceSimReport Run(BalanceConfig config, IReadOnlyList<UpgradeDef> click, IReadOnlyList<UpgradeDef> idle, double clicksPerSecond = 3d)
        {
            var report = new BalanceSimReport();
            if (config == null)
            {
                report.summary = "No balance config.";
                return report;
            }

            int phases = config.phaseCount > 0 ? config.phaseCount : BalanceDefaults.PhaseCount;
            report.phaseSeconds = new double[phases];
            report.phaseHp = new double[phases];
            report.avgDps = new double[phases];

            var clickOwned = new int[click != null ? click.Count : 0];
            var idleOwned = new int[idle != null ? idle.Count : 0];
            double score = 0d;
            double time = 0d;
            const double dt = 0.25d;

            Recalc(config, click, idle, clickOwned, idleOwned, out double clickPower, out double idlePower);

            for (int phase = 0; phase < phases; phase++)
            {
                double hp = config.GetPhaseHp(phase);
                report.phaseHp[phase] = hp;
                double left = hp;
                double start = time;
                double damageAcc = 0d;

                while (left > 0d)
                {
                    TryBuys(config, click, idle, clickOwned, idleOwned, ref score, ref clickPower, ref idlePower, clicksPerSecond);
                    double dps = clickPower * clicksPerSecond + idlePower;
                    if (dps < 0.0001d)
                        dps = 0.0001d;

                    double step = dt;
                    double gain = dps * step;
                    score += gain;
                    left -= gain;
                    damageAcc += gain;
                    time += step;

                    if (time > 200000d)
                        break;
                }

                report.phaseSeconds[phase] = time - start;
                report.avgDps[phase] = report.phaseSeconds[phase] > 0d
                    ? damageAcc / report.phaseSeconds[phase]
                    : 0d;
            }

            report.totalSeconds = time;
            report.summary =
                $"Total {report.totalSeconds / 60d:0.0} min. Last3: " +
                $"{report.phaseSeconds[Math.Max(0, phases - 3)] / 60d:0.0}/" +
                $"{report.phaseSeconds[Math.Max(0, phases - 2)] / 60d:0.0}/" +
                $"{report.phaseSeconds[phases - 1] / 60d:0.0} min. Phase0 {report.phaseSeconds[0]:0.0}s.";
            return report;
        }

        public static void FitPhaseHp(BalanceConfig config, IReadOnlyList<UpgradeDef> click, IReadOnlyList<UpgradeDef> idle, double clicksPerSecond = 3d)
        {
            if (config == null || config.phaseHp == null || config.phaseHp.Length == 0)
                return;

            double target = 0d;
            if (config.targetPhaseSeconds != null)
            {
                for (int i = 0; i < config.targetPhaseSeconds.Length; i++)
                    target += config.targetPhaseSeconds[i];
            }

            if (target <= 0d)
                target = 7200d;

            double lo = 0.05d;
            double hi = 20d;
            double[] original = (double[])config.phaseHp.Clone();

            for (int i = 0; i < 28; i++)
            {
                double mid = Math.Sqrt(lo * hi);
                ScaleHp(config, original, mid);
                var report = Run(config, click, idle, clicksPerSecond);
                if (report.totalSeconds > target)
                    hi = mid;
                else
                    lo = mid;
            }

            ScaleHp(config, original, Math.Sqrt(lo * hi));
        }

        static void ScaleHp(BalanceConfig config, double[] original, double scale)
        {
            for (int i = 0; i < config.phaseHp.Length && i < original.Length; i++)
                config.phaseHp[i] = Math.Max(1d, original[i] * scale);
        }

        static void Recalc(
            BalanceConfig config,
            IReadOnlyList<UpgradeDef> click,
            IReadOnlyList<UpgradeDef> idle,
            int[] clickOwned,
            int[] idleOwned,
            out double clickPower,
            out double idlePower)
        {
            clickPower = config != null ? config.baseClickPower : 1d;
            idlePower = 0d;
            if (click != null)
            {
                for (int i = 0; i < click.Count; i++)
                {
                    if (click[i] != null)
                        clickPower += clickOwned[i] * click[i].powerPerCopy;
                }
            }

            if (idle != null)
            {
                for (int i = 0; i < idle.Count; i++)
                {
                    if (idle[i] != null)
                        idlePower += idleOwned[i] * idle[i].powerPerCopy;
                }
            }
        }

        static void TryBuys(
            BalanceConfig config,
            IReadOnlyList<UpgradeDef> click,
            IReadOnlyList<UpgradeDef> idle,
            int[] clickOwned,
            int[] idleOwned,
            ref double score,
            ref double clickPower,
            ref double idlePower,
            double clicksPerSecond)
        {
            for (int safety = 0; safety < 48; safety++)
            {
                int bestKind = -1;
                int bestIndex = -1;
                double bestEff = 0d;
                double bestCost = 0d;

                Consider(click, clickOwned, true, clicksPerSecond, score, ref bestKind, ref bestIndex, ref bestEff, ref bestCost);
                Consider(idle, idleOwned, false, clicksPerSecond, score, ref bestKind, ref bestIndex, ref bestEff, ref bestCost);

                if (bestKind < 0)
                    return;

                score -= bestCost;
                if (bestKind == 0)
                    clickOwned[bestIndex]++;
                else
                    idleOwned[bestIndex]++;

                Recalc(config, click, idle, clickOwned, idleOwned, out clickPower, out idlePower);
            }
        }

        static void Consider(
            IReadOnlyList<UpgradeDef> defs,
            int[] owned,
            bool isClick,
            double clicksPerSecond,
            double score,
            ref int bestKind,
            ref int bestIndex,
            ref double bestEff,
            ref double bestCost)
        {
            if (defs == null)
                return;

            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null)
                    continue;
                if (!Unlocked(defs, owned, i))
                    continue;

                double cost = def.CostForOwned(owned[i]);
                if (cost > score || cost <= 0d)
                    continue;

                double gain = isClick ? def.powerPerCopy * clicksPerSecond : def.powerPerCopy;
                double eff = gain / cost;
                if (eff > bestEff)
                {
                    bestEff = eff;
                    bestIndex = i;
                    bestKind = isClick ? 0 : 1;
                    bestCost = cost;
                }
            }
        }

        static bool Unlocked(IReadOnlyList<UpgradeDef> defs, int[] owned, int index)
        {
            var def = defs[index];
            if (def.requires == null)
                return true;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i] == def.requires)
                    return owned[i] >= 1;
            }

            return true;
        }
    }
}
