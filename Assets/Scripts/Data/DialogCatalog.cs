using System;
using UnityEngine;

namespace Clicker
{
    [Serializable]
    public class DialogLine
    {
        [TextArea(2, 4)] public string ru;
        [TextArea(2, 4)] public string en;

        public string Text => Loc.T(ru, en);
    }

    [CreateAssetMenu(menuName = "Clicker/Dialogs", fileName = "Dialogs")]
    public class DialogCatalog : ScriptableObject
    {
        public DialogLine[] phaseLines;
        public DialogLine victory;

        public string GetPhaseLine(int phaseIndex)
        {
            if (phaseLines == null || phaseLines.Length == 0)
                return string.Empty;
            int i = Mathf.Clamp(phaseIndex, 0, phaseLines.Length - 1);
            return phaseLines[i] != null ? phaseLines[i].Text : string.Empty;
        }

        public string VictoryText
        {
            get
            {
                if (victory == null || string.IsNullOrEmpty(victory.Text))
                    return Loc.VictoryBody;
                return victory.Text;
            }
        }
    }
}
