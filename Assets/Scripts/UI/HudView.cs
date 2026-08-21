using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class HudView : MonoBehaviour
    {
        const int StagesPerEnemy = 4;

        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text dpsText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] Image hpFill;
        [SerializeField] Image[] phaseDots;
        [SerializeField] Button muteButton;
        [SerializeField] Image muteIcon;
        [SerializeField] Button rewardedButton;
        [SerializeField] TMP_Text rewardedLabel;

        static readonly Color DotDone = new Color(0.35f, 0.85f, 0.4f, 1f);
        static readonly Color DotCurrent = new Color(1f, 0.85f, 0.2f, 1f);
        static readonly Color DotEmpty = new Color(0.25f, 0.25f, 0.28f, 1f);

        public Button MuteButton => muteButton;
        public Button RewardedButton => rewardedButton;

        public void Refresh(EconomyService economy, CombatService combat, bool muted)
        {
            if (economy == null || combat == null)
                return;

            if (scoreText != null)
                scoreText.text = $"{Loc.Score}: {NumberFormatter.Format(economy.Score)}";
            if (dpsText != null)
            {
                dpsText.text =
                    $"{Loc.ClickPower}: {NumberFormatter.Format(economy.ClickPower)}   " +
                    $"{Loc.IdlePower}: {NumberFormatter.Format(economy.IdlePerSecond)}{Loc.PerSecond}";
            }

            if (hpText != null)
                hpText.text = $"{NumberFormatter.Format(combat.HpLeft)} / {NumberFormatter.Format(combat.HpMax)}";
            if (hpFill != null)
                hpFill.fillAmount = combat.HpFill01;

            RefreshPhaseDots(combat);

            if (muteIcon != null)
            {
                Color c = muteIcon.color;
                c.a = muted ? 0.4f : 1f;
                muteIcon.color = c;
            }

            if (rewardedLabel != null)
                rewardedLabel.text = Loc.MegaAttack;
        }

        public void SetRewardedInteractable(bool on)
        {
            if (rewardedButton != null)
                rewardedButton.interactable = on;
        }

        void RefreshPhaseDots(CombatService combat)
        {
            if (phaseDots == null)
                return;

            int stage = combat.IsWon ? StagesPerEnemy : Mathf.Clamp(combat.ActiveStageIndex, 0, StagesPerEnemy - 1);
            for (int i = 0; i < phaseDots.Length; i++)
            {
                if (phaseDots[i] == null)
                    continue;

                bool visible = i < StagesPerEnemy;
                if (phaseDots[i].gameObject.activeSelf != visible)
                    phaseDots[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;

                if (combat.IsWon || i < stage)
                    phaseDots[i].color = DotDone;
                else if (i == stage)
                    phaseDots[i].color = DotCurrent;
                else
                    phaseDots[i].color = DotEmpty;
            }
        }
    }
}
