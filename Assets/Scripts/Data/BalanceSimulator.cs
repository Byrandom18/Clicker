using System;
using System.Collections.Generic;

namespace Clicker
{
    public sealed class BalanceSimReport
    {
        public double[] phaseSeconds;
        public double[] phaseHp;
        public double[] avgDps;
        public double[] targetSeconds;
        public double totalSeconds;
        public string summary;
    }

    public static class BalanceSimulator
    {
        const double Dt = 0.25d;
        const double MaxSimTime = 200000d;
        const double MinDps = 0.0001d;
        const int DefaultPhaseCount = 15;

        static readonly double[] DefaultTargetPhaseSeconds =
        {
            15d, 30d, 45d, 60d, 90d, 120d, 240d, 360d, 480d,
            1200d, 1200d, 1200d, 2400d, 2400d, 2400d
        };

        public static BalanceSimReport Run(BalanceConfig config, IReadOnlyList<UpgradeDef> click, IReadOnlyList<UpgradeDef> idle, double clicksPerSecond = 3d)
        {
            var report = new BalanceSimReport();
            if (config == null)
            {
                report.summary = "No balance config.";
                return report;
            }

            int phases = PhaseCount(config);
            report.phaseSeconds = new double[phases];
            report.phaseHp = new double[phases];
            report.avgDps = new double[phases];
            report.targetSeconds = new double[phases];

            var state = CreateState(config, click, idle);

            for (int phase = 0; phase < phases; phase++)
            {
                double hp = config.GetPhaseHp(phase);
                report.phaseHp[phase] = hp;
                report.targetSeconds[phase] = TargetSecondsForPhase(config, phase);
                Advance(config, click, idle, state, clicksPerSecond, double.PositiveInfinity, hp, out double elapsed, out double damage);
                report.phaseSeconds[phase] = elapsed;
                report.avgDps[phase] = elapsed > 0d ? damage / elapsed : 0d;
            }

            report.totalSeconds = state.time;
            report.summary = FormatSummary(report);
            return report;
        }

        public static void FitPhaseHp(BalanceConfig config, IReadOnlyList<UpgradeDef> click, IReadOnlyList<UpgradeDef> idle, double clicksPerSecond = 3d)
        {
            if (config == null)
                return;

            int phases = PhaseCount(config);
            EnsurePhaseHpArray(config, phases);

            var state = CreateState(config, click, idle);

            for (int phase = 0; phase < phases; phase++)
            {
                double target = TargetSecondsForPhase(config, phase);
                if (target <= 0d)
                {
                    double existing = config.phaseHp[phase];
                    if (existing <= 0d)
                        existing = 1d;
                    Advance(config, click, idle, state, clicksPerSecond, double.PositiveInfinity, existing, out _, out _);
                    continue;
                }

                Advance(config, click, idle, state, clicksPerSecond, target, double.PositiveInfinity, out _, out double damage);
                config.phaseHp[phase] = Math.Max(1d, damage);
            }
        }

        static SimState CreateState(BalanceConfig config, IReadOnlyList<UpgradeDef> click, IReadOnlyList<UpgradeDef> idle)
        {
            var state = new SimState
            {
                clickOwned = new int[click != null ? click.Count : 0],
                idleOwned = new int[idle != null ? idle.Count : 0]
            };
            Recalc(config, click, idle, state);
            return state;
        }

        static void Advance(
            BalanceConfig config,
            IReadOnlyList<UpgradeDef> click,
            IReadOnlyList<UpgradeDef> idle,
            SimState state,
            double clicksPerSecond,
            double timeLimit,
            double hpLimit,
            out double elapsed,
            out double damageDealt)
        {
            elapsed = 0d;
            damageDealt = 0d;

            while (elapsed + 1e-12d < timeLimit && damageDealt + 1e-12d < hpLimit)
            {
                TryBuys(config, click, idle, state, clicksPerSecond);
                double dps = state.clickPower * clicksPerSecond + state.idlePower;
                if (dps < MinDps)
                    dps = MinDps;

                double step = Dt;
                double remainTime = timeLimit - elapsed;
                if (step > remainTime)
                    step = remainTime;

                double gain = dps * step;
                double remainHp = hpLimit - damageDealt;
                if (gain > remainHp)
                {
                    step = remainHp / dps;
                    gain = remainHp;
                }

                if (step <= 0d)
                    break;

                state.score += gain;
                damageDealt += gain;
                elapsed += step;
                state.time += step;

                if (state.time > MaxSimTime)
                    break;
            }
        }

        static int PhaseCount(BalanceConfig config)
        {
            return config != null && config.phaseCount > 0 ? config.phaseCount : DefaultPhaseCount;
        }

        static double TargetSecondsForPhase(BalanceConfig config, int phase)
        {
            double t = config != null ? config.GetTargetSeconds(phase) : 0d;
            if (t > 0d)
                return t;
            if (phase >= 0 && phase < DefaultTargetPhaseSeconds.Length)
                return DefaultTargetPhaseSeconds[phase];
            return 0d;
        }

        static void EnsurePhaseHpArray(BalanceConfig config, int phases)
        {
            if (config.phaseHp != null && config.phaseHp.Length == phases)
                return;

            var next = new double[phases];
            if (config.phaseHp != null)
            {
                int n = Math.Min(next.Length, config.phaseHp.Length);
                for (int i = 0; i < n; i++)
                    next[i] = config.phaseHp[i];
            }

            config.phaseHp = next;
        }

        static string FormatSummary(BalanceSimReport report)
        {
            int phases = report.phaseSeconds.Length;
            return
                $"Total {report.totalSeconds / 60d:0.0} min. Last3: " +
                $"{report.phaseSeconds[Math.Max(0, phases - 3)] / 60d:0.0}/" +
                $"{report.phaseSeconds[Math.Max(0, phases - 2)] / 60d:0.0}/" +
                $"{report.phaseSeconds[phases - 1] / 60d:0.0} min. Phase0 {report.phaseSeconds[0]:0.0}s.";
        }

        static void Recalc(
            BalanceConfig config,
            IReadOnlyList<UpgradeDef> click,
            IReadOnlyList<UpgradeDef> idle,
            SimState state)
        {
            state.clickPower = config != null ? config.baseClickPower : 1d;
            state.idlePower = 0d;
            if (click != null)
            {
                for (int i = 0; i < click.Count; i++)
                {
                    if (click[i] != null)
                        state.clickPower += state.clickOwned[i] * click[i].powerPerCopy;
                }
            }

            if (idle != null)
            {
                for (int i = 0; i < idle.Count; i++)
                {
                    if (idle[i] != null)
                        state.idlePower += state.idleOwned[i] * idle[i].powerPerCopy;
                }
            }
        }

        static void TryBuys(
            BalanceConfig config,
            IReadOnlyList<UpgradeDef> click,
            IReadOnlyList<UpgradeDef> idle,
            SimState state,
            double clicksPerSecond)
        {
            for (int safety = 0; safety < 48; safety++)
            {
                int bestKind = -1;
                int bestIndex = -1;
                double bestEff = 0d;
                double bestCost = 0d;

                Consider(click, state.clickOwned, true, clicksPerSecond, state.score, ref bestKind, ref bestIndex, ref bestEff, ref bestCost);
                Consider(idle, state.idleOwned, false, clicksPerSecond, state.score, ref bestKind, ref bestIndex, ref bestEff, ref bestCost);

                if (bestKind < 0)
                    return;

                state.score -= bestCost;
                if (bestKind == 0)
                    state.clickOwned[bestIndex]++;
                else
                    state.idleOwned[bestIndex]++;

                Recalc(config, click, idle, state);
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

        sealed class SimState
        {
            public int[] clickOwned;
            public int[] idleOwned;
            public double score;
            public double clickPower;
            public double idlePower;
            public double time;
        }
    }
}
