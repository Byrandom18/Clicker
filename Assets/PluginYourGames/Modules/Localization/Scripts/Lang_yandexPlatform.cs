#if YandexGamesPlatform_yg
using System.Runtime.InteropServices;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        [DllImport("__Internal")]
        private static extern string LangRequest_js();

        public string GetLanguage()
        {
            string value = LangRequest_js();
            return string.IsNullOrEmpty(value) ? "ru" : value;
        }
    }
}
#endif