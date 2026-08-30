using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicker
{
    public class HudView : MonoBehaviour
    {
        const int StagesPerEnemy = CombatService.CampaignStagesPerEnemy;

        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text dpsText;
        [SerializeField] TMP_Text hpText;
        [SerializeField] Image hpFill;
        [SerializeField] Image[] phaseDots;
        [SerializeField] Button muteButton;
        [SerializeField] Image muteIcon;
        [SerializeField] Button musicMuteButton;
        [SerializeField] Image musicMuteIcon;
        [SerializeField] Button autoUpgradeButton;
        [SerializeField] TMP_Text autoUpgradeLabel;
        [SerializeField] Button rewardedButton;
        [SerializeField] TMP_Text rewardedLabel;
        [SerializeField, Range(0f, 0.3f), Tooltip("Насколько сильно кнопка награды увеличивается при дыхании.")]
        float rewardedBreathStrength = 0.08f;
        [SerializeField, Range(0.1f, 4f), Tooltip("Как быстро кнопка награды дышит.")]
        float rewardedBreathIntensity = 1.2f;

        public Button MuteButton => muteButton;
        public Button MusicMuteButton => musicMuteButton;
        public Button AutoUpgradeButton => autoUpgradeButton;
        public Button RewardedButton => rewardedButton;

        void Awake()
        {
            ResolveExtraButtons();
            UiButtonScaleFeedback.EnsureAll();
            ApplyRewardBreath();
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            var first = rewardedButton != null ? rewardedButton.GetComponent<UiButtonScaleFeedback>() : null;
            if (first != null)
                first.SetBreath(true, rewardedBreathStrength, rewardedBreathIntensity);
            var second = autoUpgradeButton != null ? autoUpgradeButton.GetComponent<UiButtonScaleFeedback>() : null;
            if (second != null)
                second.SetBreath(true, rewardedBreathStrength, rewardedBreathIntensity);
        }

        void OnDestroy()
        {
            KillHeartTweens();
        }

        void ApplyRewardBreath()
        {
            var first = UiButtonScaleFeedback.Ensure(rewardedButton);
            if (first != null)
                first.SetBreath(true, rewardedBreathStrength, rewardedBreathIntensity);
            var second = UiButtonScaleFeedback.Ensure(autoUpgradeButton);
            if (second != null)
                second.SetBreath(true, rewardedBreathStrength, rewardedBreathIntensity);
        }

        public void Refresh(EconomyService economy, CombatService combat, bool muted, bool musicMuted, float autoUpgradeLeft)
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

            ApplyMuteIcon(muteIcon, muted);
            ApplyMuteIcon(musicMuteIcon, musicMuted);

            if (rewardedLabel != null)
                rewardedLabel.text = Loc.MegaAttack;
            if (autoUpgradeLabel != null)
            {
                autoUpgradeLabel.text = autoUpgradeLeft > 0f
                    ? Loc.AutoUpgradeLeft(autoUpgradeLeft)
                    : Loc.SmartAutoUpgrade;
            }
        }

        public void SetRewardedInteractable(bool on)
        {
            if (rewardedButton != null)
                rewardedButton.interactable = on;
        }

        public void SetAutoUpgradeInteractable(bool on)
        {
            if (autoUpgradeButton != null)
                autoUpgradeButton.interactable = on;
        }

        void ResolveExtraButtons()
        {
            if (musicMuteButton == null)
                musicMuteButton = FindButton("MuteMusicButton");
            if (musicMuteIcon == null && musicMuteButton != null)
                musicMuteIcon = musicMuteButton.GetComponentInChildren<Image>(true);
            if (musicMuteButton != null && musicMuteButton.targetGraphic == null && musicMuteIcon != null)
                musicMuteButton.targetGraphic = musicMuteIcon;

            if (autoUpgradeButton == null)
            {
                autoUpgradeButton = FindButton("AutoUpgradeButton")
                    ?? FindButton("AutoBuyButton")
                    ?? FindButton("RewardAutoButton")
                    ?? FindButton("RewardButton (1)");
            }

            if (autoUpgradeLabel == null && autoUpgradeButton != null)
                autoUpgradeLabel = autoUpgradeButton.GetComponentInChildren<TMP_Text>(true);
        }

        Button FindButton(string objectName)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == objectName)
                    return buttons[i];
            }

            return null;
        }

        static void ApplyMuteIcon(Image icon, bool muted)
        {
            if (icon == null)
                return;
            Color c = icon.color;
            c.a = muted ? 0.4f : 1f;
            icon.color = c;
        }

        public void SnapHearts(int completedStages)
        {
            if (phaseDots == null)
                return;

            KillHeartTweens();
            completedStages = Mathf.Clamp(completedStages, 0, StagesPerEnemy);
            for (int i = 0; i < phaseDots.Length; i++)
            {
                var img = phaseDots[i];
                if (img == null)
                    continue;

                bool show = i < StagesPerEnemy && i >= completedStages;
                ResetHeartVisual(img);
                if (img.gameObject.activeSelf != show)
                    img.gameObject.SetActive(show);
            }
        }

        public void PlayDestroy(int stageIndex)
        {
            if (phaseDots == null || stageIndex < 0 || stageIndex >= phaseDots.Length)
                return;

            var img = phaseDots[stageIndex];
            if (img == null)
                return;

            img.DOKill();
            ResetHeartVisual(img);
            if (!img.gameObject.activeSelf)
                img.gameObject.SetActive(true);

            DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(img.gameObject)
                .Append(img.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.12f, 4, 0.4f))
                .Append(img.transform.DOScale(Vector3.zero, 0.18f))
                .Join(img.DOFade(0f, 0.18f))
                .OnComplete(() =>
                {
                    if (img != null)
                        img.gameObject.SetActive(false);
                });
        }

        void KillHeartTweens()
        {
            if (phaseDots == null)
                return;
            for (int i = 0; i < phaseDots.Length; i++)
            {
                if (phaseDots[i] != null)
                    phaseDots[i].DOKill();
            }
        }

        static void ResetHeartVisual(Image img)
        {
            img.DOKill();
            img.transform.localScale = Vector3.one;
            Color c = img.color;
            c.a = 1f;
            img.color = c;
        }
    }
}
