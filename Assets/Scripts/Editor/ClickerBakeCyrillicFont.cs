#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Clicker.EditorTools
{
    public class ClickerBakeCyrillicFont : IPreprocessBuildWithReport
    {
        const string FallbackPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
        const string SourcePath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            Bake(false);
        }

        [InitializeOnLoadMethod]
        static void AutoBakeIfMissing()
        {
            EditorApplication.delayCall += () => Bake(false);
        }

        [MenuItem("Clicker/Bake Cyrillic Font")]
        public static void BakeMenu()
        {
            Bake(true);
        }

        public static void Bake(bool force)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            if (font == null || source == null)
            {
                if (force)
                    Debug.LogError("Clicker: LiberationSans fallback or TTF is missing.");
                return;
            }

            font.ReadFontAssetDefinition();
            if (!force && font.HasCharacter('В', false, false) && font.HasCharacter('Ю', false, false)
                && font.characterTable != null && font.characterTable.Count >= 60)
                return;

            var so = new SerializedObject(font);
            SetObj(so, "m_SourceFontFile_EditorRef", source);
            SetObj(so, "m_SourceFontFile", source);
            SetStr(so, "m_SourceFontFileGUID", AssetDatabase.AssetPathToGUID(SourcePath));
            SetInt(so, "m_AtlasPopulationMode", (int)AtlasPopulationMode.Dynamic);
            SetBool(so, "m_IsMultiAtlasTexturesEnabled", false);
            SetBool(so, "m_ClearDynamicDataOnBuild", false);
            SetBool(so, "m_GetFontFeatures", false);
            SetInt(so, "m_AtlasWidth", 2048);
            SetInt(so, "m_AtlasHeight", 2048);
            so.ApplyModifiedPropertiesWithoutUndo();

            font.ClearFontAssetData(false);
            string missing;
            bool allAdded = font.TryAddCharacters(Charset(), out missing, false);

            so = new SerializedObject(font);
            SetInt(so, "m_AtlasPopulationMode", (int)AtlasPopulationMode.Static);
            SetObj(so, "m_SourceFontFile", null);
            SetBool(so, "m_ClearDynamicDataOnBuild", false);
            SetBool(so, "m_IsMultiAtlasTexturesEnabled", false);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            int count = font.characterTable != null ? font.characterTable.Count : 0;
            if (!allAdded && !string.IsNullOrEmpty(missing))
                Debug.LogWarning("Clicker: Cyrillic font baked with missing glyphs: " + missing);
            else
                Debug.Log("Clicker: baked " + count + " glyphs into LiberationSans SDF Fallback (static).");
        }

        static void SetObj(SerializedObject so, string name, Object value)
        {
            var p = so.FindProperty(name);
            if (p != null)
                p.objectReferenceValue = value;
        }

        static void SetStr(SerializedObject so, string name, string value)
        {
            var p = so.FindProperty(name);
            if (p != null)
                p.stringValue = value;
        }

        static void SetInt(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p != null)
                p.intValue = value;
        }

        static void SetBool(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p != null)
                p.boolValue = value;
        }

        static string Charset()
        {
            var sb = new StringBuilder(128);
            sb.Append("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ");
            sb.Append("абвгдеёжзийклмнопрстуфхцчшщъыьэюя");
            sb.Append("ІіЇїЄєҐґЎў");
            sb.Append("—–…«»№");
            return sb.ToString();
        }
    }
}
#endif
