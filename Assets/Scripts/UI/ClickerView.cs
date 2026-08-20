using System;
using UnityEngine;
using UnityEngine.UI;
using YG;

namespace Clicker
{
    public class ClickerView : MonoBehaviour
    {
        public event Action OnEnemyClicked;
        public event Action OnContinue;
        public event Action OnRewarded;
        public event Action OnMute;
        public event Action<int> OnBuy;

        [Header("HUD")]
        public Image background;
        public Text scoreText;
        public Text powerText;
        public Text howToText;
        public Image[] dots;
        public Button muteButton;
        public Text muteLabel;
        public Button rewardedButton;
        public Text rewardedLabel;
        public Text shopTitle;

        [Header("Arena (enemy 0, 1, 2)")]
        public Image[] enemyImages;
        public Text[] enemyNames;
        public RectTransform[] enemyRects;
        public Button enemyClickButton;
        public RectTransform clickCatcher;
        public Image hpFill;
        public Text hpText;

        [Header("Shop")]
        public ShopRowView[] shopRows;

        [Header("Dialog / Victory")]
        public GameObject dialogRoot;
        public Text dialogText;
        public Button continueButton;
        public GameObject victoryRoot;
        public Text victoryTitle;
        public Text victoryBody;

        EconomyService _economy;
        CombatService _combat;
        BalanceConfig _balance;
        EnemyCatalog _enemies;
        DialogCatalog _dialogs;
        Coroutine _rotateRoutine;
        Coroutine _punchRoutine;

        public void Bind(EconomyService economy, CombatService combat, BalanceConfig balance, EnemyCatalog enemies, DialogCatalog dialogs)
        {
            _economy = economy;
            _combat = combat;
            _balance = balance;
            _enemies = enemies;
            _dialogs = dialogs;

            if (enemyClickButton != null)
            {
                enemyClickButton.onClick.RemoveAllListeners();
                enemyClickButton.onClick.AddListener(() => OnEnemyClicked?.Invoke());
            }

            if (muteButton != null)
            {
                muteButton.onClick.RemoveAllListeners();
                muteButton.onClick.AddListener(() => OnMute?.Invoke());
            }

            if (rewardedButton != null)
            {
                rewardedButton.onClick.RemoveAllListeners();
                rewardedButton.onClick.AddListener(() => OnRewarded?.Invoke());
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => OnContinue?.Invoke());
            }

            if (shopRows != null)
            {
                for (int i = 0; i < shopRows.Length; i++)
                {
                    if (shopRows[i] != null)
                        shopRows[i].Setup(i, idx => OnBuy?.Invoke(idx));
                }
            }

            if (_enemies != null && _enemies.background != null && background != null)
            {
                background.sprite = _enemies.background;
                background.color = Color.white;
            }

            if (dialogRoot != null)
                dialogRoot.SetActive(false);
            if (victoryRoot != null)
                victoryRoot.SetActive(false);

            RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshHud();
            RefreshShop();
            RefreshEnemies(true);
            RefreshMute();
            RefreshRewarded();
            if (howToText != null)
                howToText.text = Loc.HowToPlay;
            if (shopTitle != null)
                shopTitle.text = Loc.Shop;
            if (victoryTitle != null)
                victoryTitle.text = Loc.VictoryTitle;
            if (continueButton != null)
            {
                var label = continueButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = Loc.Continue;
            }
        }

        public void RefreshHud()
        {
            if (_economy == null || _combat == null)
                return;
            if (scoreText != null)
                scoreText.text = Loc.Score + ": " + NumberFormatter.Format(_economy.Score);
            if (powerText != null)
            {
                powerText.text = Loc.ClickPower + " " + NumberFormatter.Format(_economy.ClickPower) +
                                 "   " + Loc.IdlePower + " " + NumberFormatter.Format(_economy.IdlePerSecond) + Loc.PerSecond;
            }

            if (hpFill != null)
                hpFill.fillAmount = _combat.HpNormalized;
            if (hpText != null)
                hpText.text = NumberFormatter.Format(_combat.HpLeft) + " / " + NumberFormatter.Format(_combat.HpMax);

            if (dots != null)
            {
                for (int i = 0; i < dots.Length; i++)
                {
                    if (dots[i] == null)
                        continue;
                    if (i < _combat.PhaseIndex)
                        dots[i].color = new Color(0.24f, 0.81f, 0.56f);
                    else if (i == _combat.PhaseIndex && !_combat.Won)
                        dots[i].color = new Color(0.94f, 0.76f, 0.29f);
                    else
                        dots[i].color = new Color(0.54f, 0.51f, 0.60f);
                }
            }

            bool canPlay = !_combat.Locked && !_combat.Won;
            if (enemyClickButton != null)
                enemyClickButton.interactable = canPlay;
            if (rewardedButton != null)
                rewardedButton.interactable = canPlay && !YG2.nowAdsShow;
        }

        public void RefreshShop()
        {
            if (shopRows == null || _economy == null)
                return;
            int n = Mathf.Min(shopRows.Length, _economy.ShopCount);
            for (int i = 0; i < n; i++)
            {
                if (shopRows[i] == null)
                    continue;
                var def = _economy.Def(i);
                shopRows[i].Render(
                    def,
                    _economy.IsUnlocked(i),
                    _economy.Owned(i),
                    _economy.NextCost(i),
                    _economy.CanAfford(i) && !_combat.Won,
                    _economy.IsMaxed(i));
            }
        }

        public void RefreshMute()
        {
            if (muteLabel != null)
                muteLabel.text = YG2.saves.muted ? "✕♪" : "♪";
        }

        public void RefreshRewarded()
        {
            if (rewardedLabel == null || _balance == null || _combat == null)
                return;
            rewardedLabel.text = Loc.RewardedButton(_balance.GetRewardedPercent(_combat.PhaseIndex));
        }

        public void RefreshEnemies(bool instant)
        {
            if (_combat == null)
                return;
            RefreshEnemies(instant, _combat.PhaseIndex);
        }

        public void RefreshEnemies(bool instant, int visualPhase)
        {
            if (enemyImages == null)
                return;
            for (int e = 0; e < enemyImages.Length && e < 3; e++)
            {
                int stage = CombatService.SpriteStageForEnemy(e, visualPhase);
                if (enemyImages[e] != null)
                {
                    var sprite = ResolveSprite(e, stage);
                    if (sprite != null)
                    {
                        enemyImages[e].sprite = sprite;
                        enemyImages[e].color = Color.white;
                    }
                    else
                    {
                        var def = _enemies != null ? _enemies.Get(e) : null;
                        enemyImages[e].color = def != null ? def.placeholderColor : Color.white;
                    }
                }

                if (enemyNames != null && e < enemyNames.Length && enemyNames[e] != null)
                {
                    var def = _enemies != null ? _enemies.Get(e) : null;
                    enemyNames[e].text = def != null ? def.DisplayName : Loc.Enemy + " " + (e + 1);
                }
            }

            PlaceEnemies(visualPhase);
        }

        public void BeginTransition(int fromPhase)
        {
            int toPhase = Mathf.Min(fromPhase + 1, 11);
            RefreshEnemies(true, toPhase);
            PlaceEnemies(fromPhase);
            PlayRotation(fromPhase, toPhase, null);
        }

        Sprite ResolveSprite(int enemy, int stage)
        {
            var def = _enemies != null ? _enemies.Get(enemy) : null;
            return def != null ? def.GetSprite(stage) : null;
        }

        public void PlayRotation(int fromPhase, int toPhase, Action done)
        {
            if (_rotateRoutine != null)
                StopCoroutine(_rotateRoutine);
            _rotateRoutine = StartCoroutine(RotateRoutine(fromPhase, toPhase, done));
        }

        public void SnapRotation()
        {
            if (_rotateRoutine != null)
            {
                StopCoroutine(_rotateRoutine);
                _rotateRoutine = null;
            }
            RefreshEnemies(true);
        }

        System.Collections.IEnumerator RotateRoutine(int fromPhase, int toPhase, Action done)
        {
            float t = 0f;
            const float duration = 0.7f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                BlendPlaces(fromPhase, toPhase, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }

            PlaceEnemies(toPhase);
            _rotateRoutine = null;
            done?.Invoke();
        }

        void BlendPlaces(int fromPhase, int toPhase, float k)
        {
            if (enemyRects == null || enemyImages == null)
                return;
            int count = Mathf.Min(3, Mathf.Min(enemyRects.Length, enemyImages.Length));
            for (int e = 0; e < count; e++)
            {
                if (enemyRects[e] == null || enemyImages[e] == null)
                    continue;
                GetSlot(fromPhase, e, out var aPos, out var aScale, out int aOrder, out var aColor);
                GetSlot(toPhase, e, out var bPos, out var bScale, out int bOrder, out var bColor);
                enemyRects[e].anchorMin = Vector2.Lerp(aPos, bPos, k);
                enemyRects[e].anchorMax = enemyRects[e].anchorMin;
                enemyRects[e].pivot = new Vector2(0.5f, 0.12f);
                enemyRects[e].anchoredPosition = Vector2.zero;
                enemyRects[e].sizeDelta = Vector2.Lerp(aScale, bScale, k);
                var sprite = enemyImages[e].sprite;
                enemyImages[e].color = sprite != null ? Color.Lerp(aColor, bColor, k) : enemyImages[e].color;
                enemyImages[e].transform.SetSiblingIndex(Mathf.RoundToInt(Mathf.Lerp(aOrder, bOrder, k)));
            }

            if (clickCatcher != null)
                clickCatcher.SetAsLastSibling();
        }

        void PlaceEnemies(int phase)
        {
            BlendPlaces(phase, phase, 1f);
            if (enemyClickButton != null && _combat != null)
                enemyClickButton.interactable = !_combat.Locked && !_combat.Won;
        }

        static void GetSlot(int phase, int enemy, out Vector2 anchor, out Vector2 size, out int order, out Color color)
        {
            int active = ((phase % 3) + 3) % 3;
            int next = (active + 1) % 3;
            if (enemy == active)
            {
                anchor = new Vector2(0.50f, 0.42f);
                size = new Vector2(360, 480);
                order = 2;
                color = Color.white;
            }
            else if (enemy == next)
            {
                anchor = new Vector2(0.78f, 0.46f);
                size = new Vector2(210, 280);
                order = 1;
                color = new Color(0.7f, 0.7f, 0.75f, 0.85f);
            }
            else
            {
                anchor = new Vector2(0.22f, 0.46f);
                size = new Vector2(210, 280);
                order = 0;
                color = new Color(0.7f, 0.7f, 0.75f, 0.85f);
            }
        }

        public void ShowDialog(int phaseIndex)
        {
            if (dialogRoot != null)
                dialogRoot.SetActive(true);
            if (dialogText != null && _dialogs != null)
                dialogText.text = _dialogs.GetPhaseLine(phaseIndex);
            RefreshHud();
        }

        public void HideDialog()
        {
            if (dialogRoot != null)
                dialogRoot.SetActive(false);
        }

        public void ShowVictory()
        {
            HideDialog();
            if (victoryRoot != null)
                victoryRoot.SetActive(true);
            if (victoryBody != null)
                victoryBody.text = _dialogs != null ? _dialogs.VictoryText : Loc.VictoryBody;
            RefreshHud();
        }

        public void PunchActive()
        {
            if (_combat == null || enemyRects == null)
                return;
            int active = _combat.PhaseIndex % 3;
            if (active < 0 || active >= enemyRects.Length || enemyRects[active] == null)
                return;
            if (_punchRoutine != null)
                StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine(enemyRects[active]));
        }

        System.Collections.IEnumerator PunchRoutine(RectTransform rt)
        {
            Vector3 baseScale = rt.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 8f;
                rt.localScale = baseScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);
                yield return null;
            }
            rt.localScale = baseScale;
            _punchRoutine = null;
        }
    }
}
