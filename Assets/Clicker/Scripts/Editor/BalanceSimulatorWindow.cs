using Clicker;
using UnityEditor;
using UnityEngine;

namespace Clicker.EditorTools
{
    public class BalanceSimulatorWindow : EditorWindow
    {
        BalanceConfig _config;
        BalanceSimulator.Report _report;
        Vector2 _scroll;
        double _cps = 3d;

        [MenuItem("Clicker/Balance Simulator")]
        public static void Open()
        {
            GetWindow<BalanceSimulatorWindow>("Clicker Balance");
        }

        void OnGUI()
        {
            _config = (BalanceConfig)EditorGUILayout.ObjectField("Balance", _config, typeof(BalanceConfig), false);
            _cps = EditorGUILayout.DoubleField("Clicks per second", _cps);

            if (GUILayout.Button("Simulate default numbers"))
            {
                var cfg = BalanceDefaults.CreateBalance();
                _report = BalanceSimulator.Run(cfg, _cps);
            }

            if (GUILayout.Button("Log fitted hpBase for defaults"))
            {
                var cfg = BalanceDefaults.CreateBalance();
                BalanceSimulator.FitHpBase(cfg, _cps);
                Debug.Log($"Clicker fitted hpBase={cfg.hpBase:0.###} growth={cfg.hpGrowth} total={BalanceSimulator.Run(cfg, _cps).totalSeconds / 60d:0.0} min");
                _report = BalanceSimulator.Run(cfg, _cps);
            }

            if (GUILayout.Button("Fit hpBase to target total time") && _config != null)
            {
                Undo.RecordObject(_config, "Fit clicker HP");
                BalanceSimulator.FitHpBase(_config, _cps);
                EditorUtility.SetDirty(_config);
                _report = BalanceSimulator.Run(_config, _cps);
            }

            if (_report.phases == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Total", $"{_report.totalSeconds / 60d:0.0} min  (target {_report.targetTotal / 60d:0.0} min)");
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _report.phases.Length; i++)
            {
                var p = _report.phases[i];
                double err = p.targetSeconds > 0d ? (p.seconds / p.targetSeconds - 1d) * 100d : 0d;
                EditorGUILayout.LabelField(
                    $"Phase {i}",
                    $"{p.seconds:0}s / {p.targetSeconds:0}s  ({err:+0.0;-0.0}%)  DPS {p.dpsAtEnd:0}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Click owned", string.Join(", ", _report.clickOwned));
            EditorGUILayout.LabelField("Idle owned", string.Join(", ", _report.idleOwned));
            EditorGUILayout.EndScrollView();
        }
    }
}
