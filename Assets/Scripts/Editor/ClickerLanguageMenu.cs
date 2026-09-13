#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using YG;

namespace Clicker.EditorTools
{
    public static class ClickerLanguageMenu
    {
        const string RuPath = "Clicker/Language/Русский";
        const string EnPath = "Clicker/Language/English";

        [MenuItem(RuPath, false, 200)]
        static void SetRussian() => SetLanguage("ru");

        [MenuItem(RuPath, true)]
        static bool SetRussianValidate()
        {
            Menu.SetChecked(RuPath, !IsEnglish());
            return true;
        }

        [MenuItem(EnPath, false, 201)]
        static void SetEnglish() => SetLanguage("en");

        [MenuItem(EnPath, true)]
        static bool SetEnglishValidate()
        {
            Menu.SetChecked(EnPath, IsEnglish());
            return true;
        }

        static bool IsEnglish()
        {
            string lang = CurrentLang();
            switch (lang)
            {
                case "ru":
                case "be":
                case "kk":
                case "uk":
                case "uz":
                    return false;
                default:
                    return true;
            }
        }

        static string CurrentLang()
        {
            if (EditorApplication.isPlaying && !string.IsNullOrEmpty(YG2.lang))
                return YG2.lang.ToLowerInvariant();

            var info = YG2.infoYG;
            if (info != null && !string.IsNullOrEmpty(info.Simulation.language))
                return info.Simulation.language.ToLowerInvariant();

            return "ru";
        }

        static void SetLanguage(string lang)
        {
            var info = YG2.infoYG;
            if (info != null)
            {
                Undo.RecordObject(info, "Set clicker language");
                info.Simulation.language = lang;
                EditorUtility.SetDirty(info);
            }

            if (EditorApplication.isPlaying)
                YG2.SwitchLanguage(lang);

            Debug.Log(lang == "en"
                ? "Clicker: language set to English."
                : "Clicker: language set to Russian.");
        }
    }
}
#endif
