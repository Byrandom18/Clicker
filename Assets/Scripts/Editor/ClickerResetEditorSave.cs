#if UNITY_EDITOR
using System.IO;
using Clicker;
using UnityEditor;
using UnityEngine;
using YG;

namespace Clicker.EditorTools
{
    public static class ClickerResetEditorSave
    {
        [MenuItem("Clicker/Reset Editor Save")]
        public static void ResetEditorSave()
        {
            string path = Path.Combine(InfoYG.PATCH_PC_EDITOR, "SavesEditorYG2.json");
            File.WriteAllText(path,
                "{\n  \"idSave\": 1,\n  \"clickerInitialized\": false,\n  \"clickerSaveVersion\": 2,\n  \"score\": 0.0,\n  \"phaseIndex\": 0,\n  \"hpLeft\": -1.0,\n  \"pendingOverflow\": 0.0,\n  \"muted\": false,\n  \"musicMuted\": false,\n  \"autoUpgradeLeft\": 0.0,\n  \"gameWon\": false,\n  \"endlessMode\": false,\n  \"upgrades\": []\n}\n");
            ClickerSave.ResetProgress();
            AssetDatabase.Refresh();
            Debug.Log("Clicker: editor save reset. Press Play for a fresh start.");
        }
    }
}
#endif
