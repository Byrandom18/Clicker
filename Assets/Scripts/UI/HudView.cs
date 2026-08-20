using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text dpsText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] Image hpFill;
        [SerializeField] Image[] phaseDots;
        [SerializeField] Button muteButton;
        [SerializeField] TMP_Text muteLabel;
        [SerializeField] Button rewardedButton;
        [SerializeField] TMP_Text rewardedLabel;

        public Button MuteButton => muteButton;
        public Button RewardedButton => rewardedButton;

        public void Refresh(EconomyService economy, CombatService combat, float rewardedPercent, bool muted)
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

            if (phaseDots != null)
            {
                for (int i = 0; i < phaseDots.Length; i++)
                {
                    if (phaseDots[i] == null)
                        continue;
                    bool done = combat.IsWon || i < combat.PhaseIndex;
                    bool current = !combat.IsWon && i == combat.PhaseIndex;
                    phaseDots[i].color = done
                        ? new Color(0.35f, 0.85f, 0.4f, 1f)
                        : current
                            ? new Color(1f, 0.85f, 0.2f, 1f)
                            : new Color(0.25f, 0.25f, 0.28f, 1f);
                }
            }

            if (muteLabel != null)
                muteLabel.text = muted ? Loc.MuteOn : Loc.MuteOff;
            if (rewardedLabel != null)
                rewardedLabel.text = Loc.RewardedButton(rewardedPercent);
        }

        public void SetRewardedInteractable(bool on)
        {
            if (rewardedButton != null)
                rewardedButton.interactable = on;
        }
    }
}
