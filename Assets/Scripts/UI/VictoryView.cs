using TMPro;
using UnityEngine;

namespace Clicker
{
    public class VictoryView : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;

        public void Show(string bodyText)
        {
            if (title != null)
                title.text = Loc.VictoryTitle;
            if (body != null)
                body.text = string.IsNullOrEmpty(bodyText) ? Loc.VictoryBody : bodyText;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
