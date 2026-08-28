#if UNITY_EDITOR
using Clicker;
using UnityEditor;
using UnityEngine;

namespace Clicker.EditorTools
{
    public class BalanceSimulatorWindow : EditorWindow
    {
        BalanceConfig _config;
        double _cps = 3d;
        string _report = "Assign Balance, then simulate. Run Clicker/Create Default Data Assets first.";

        [MenuItem("Clicker/Balance Simulator")]
        public static void Open()
        {
            GetWindow<BalanceSimulatorWindow>("Clicker Balance");
        }

        void OnGUI()
        {
            _config = (BalanceConfig)EditorGUILayout.ObjectField("Balance", _config, typeof(BalanceConfig), false);
            _cps = EditorGUILayout.DoubleField("Clicks / sec", _cps);

            if (GUILayout.Button("Load default Balance.asset"))
            {
                _config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ClickerPaths.Balance);
            }

            if (GUILayout.Button("Simulate 2h path"))
            {
                if (_config == null)
                    _config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ClickerPaths.Balance);
                var click = LoadUpgrades(ClickerPaths.ClickDir);
                var idle = LoadUpgrades(ClickerPaths.IdleDir);
                var report = BalanceSimulator.Run(_config, click, idle, _cps);
                _report = Format(report);
            }

            if (GUILayout.Button("Fit phase HP to target phase seconds") && _config != null)
            {
                Undo.RecordObject(_config, "Fit clicker HP");
                var click = LoadUpgrades(ClickerPaths.ClickDir);
                var idle = LoadUpgrades(ClickerPaths.IdleDir);
                BalanceSimulator.FitPhaseHp(_config, click, idle, _cps);
                EditorUtility.SetDirty(_config);
                _report = Format(BalanceSimulator.Run(_config, click, idle, _cps));
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_report, MessageType.Info);
        }

        static UpgradeDef[] LoadUpgrades(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:UpgradeDef", new[] { folder });
            var list = new UpgradeDef[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                list[i] = AssetDatabase.LoadAssetAtPath<UpgradeDef>(AssetDatabase.GUIDToAssetPath(guids[i]));
            System.Array.Sort(list, (a, b) => string.CompareOrdinal(a != null ? a.id : "", b != null ? b.id : ""));
            return list;
        }

        static string Format(BalanceSimReport report)
        {
            if (report == null || report.phaseSeconds == null)
                return "No report.";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(report.summary);
            for (int i = 0; i < report.phaseSeconds.Length; i++)
            {
                double target = report.targetSeconds != null && i < report.targetSeconds.Length
                    ? report.targetSeconds[i]
                    : 0d;
                sb.AppendLine(
                    $"P{i}: {report.phaseSeconds[i]:0.0}s / target {target:0.0}s  HP {report.phaseHp[i]:0}  DPS {report.avgDps[i]:0.0}");
            }

            return sb.ToString();
        }
    }
}
#endif
