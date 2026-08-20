using System.Collections.Generic;
using UnityEngine;
using YG;

namespace Clicker
{
    // Scene wiring (you assemble Canvas): Clicker/Create Default Data Assets,
    // Clicker/Create Prefabs, Clicker/Add World Objects To Open Scene.
    public class ClickerGame : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] BalanceConfig balance;
        [SerializeField] DialogCatalog dialogs;

        [Header("World")]
        [SerializeField] EnemySlotDirector slots;

        [Header("UI")]
        [SerializeField] BattleZoneClick battleZone;
        [SerializeField] ShopView shop;
        [SerializeField] HudView hud;
        [SerializeField] SpeechBubbleView bubble;
        [SerializeField] VictoryView victory;

        EconomyService _economy;
        CombatService _combat;
        InterstitialGate _ads;
        bool _booted;
        bool _blockPlay;
        bool _dirty;
        float _lastSave = -10f;
        float _hudAcc;

        void Awake()
        {
            Application.targetFrameRate = 60;
            _ads = new InterstitialGate(this);
        }

        void OnEnable()
        {
            BattleZoneClick.Pressed += HandleClick;
            ShopRowView.BuyClicked += HandleBuy;
            if (bubble != null)
                bubble.ContinueClicked += HandleBubbleContinue;
            YG2.onSwitchLang += HandleLang;
        }

        void OnDisable()
        {
            BattleZoneClick.Pressed -= HandleClick;
            ShopRowView.BuyClicked -= HandleBuy;
            if (bubble != null)
                bubble.ContinueClicked -= HandleBubbleContinue;
            YG2.onSwitchLang -= HandleLang;
            _ads?.Cancel();
        }

        void Start()
        {
            if (YG2.isSDKEnabled)
                Boot();
            else
                YG2.onGetSDKData += Boot;
        }

        void OnDestroy()
        {
            YG2.onGetSDKData -= Boot;
        }

        void Update()
        {
            if (!_booted || _combat == null || _economy == null)
                return;

            if (CanTick())
            {
                double gain = _economy.IdlePerSecond * Time.deltaTime;
                if (gain > 0d)
                {
                    _economy.AddIncome(gain);
                    if (_combat.ApplyDamage(gain))
                        BeginInterlude();
                    _dirty = true;
                }
            }

            _hudAcc += Time.unscaledDeltaTime;
            if (_hudAcc >= 0.12f)
            {
                _hudAcc = 0f;
                RefreshUi();
            }

            MaybeSave(false);
        }

        void Boot()
        {
            YG2.onGetSDKData -= Boot;
            if (_booted)
                return;
            _booted = true;

            if (balance == null)
            {
                Debug.LogError("ClickerGame: assign BalanceConfig.");
                return;
            }

            _economy = new EconomyService(balance);
            _combat = new CombatService(balance);
            _combat.InitFromSave();

            if (shop != null)
            {
                shop.Collect();
                var defs = new List<UpgradeDef>();
                var rows = shop.Rows;
                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i] != null && rows[i].Definition != null)
                        defs.Add(rows[i].Definition);
                }

                _economy.SetDefinitions(defs);
            }
            else
            {
                _economy.Recalc();
            }

            ApplyMute(YG2.saves.muted, false);
            if (slots != null)
            {
                slots.SnapToPhase(_combat.PhaseIndex);
                RefreshEnemySprites(false);
            }

            if (bubble != null)
                bubble.Hide();
            if (victory != null)
                victory.Hide();

            if (hud != null)
            {
                if (hud.MuteButton != null)
                    hud.MuteButton.onClick.AddListener(ToggleMute);
                if (hud.RewardedButton != null)
                    hud.RewardedButton.onClick.AddListener(HandleRewarded);
            }

            if (_combat.IsWon)
            {
                ShowVictory();
                return;
            }

            if (_combat.HpLeft <= 0d)
            {
                BeginInterlude();
                RefreshUi();
                return;
            }

            SetPlaying(true);
            YG2.GameplayStart();
            RefreshUi();
        }

        bool CanTick()
        {
            return !_blockPlay
                   && !_combat.IsWon
                   && !_combat.HasPendingInterlude
                   && !YG2.isPauseGame
                   && YG2.isFocusWindowGame
                   && !YG2.nowAdsShow
                   && Time.timeScale > 0f;
        }

        void HandleClick()
        {
            if (!_booted || !CanTick())
                return;

            double amount = _economy.ClickPower;
            _economy.AddIncome(amount);
            if (_combat.ApplyDamage(amount))
                BeginInterlude();
            _dirty = true;
            RefreshUi();
        }

        void HandleBuy(UpgradeDef def)
        {
            if (!_booted || _blockPlay || _combat.IsWon || def == null)
                return;
            if (!_economy.TryBuy(def))
                return;
            MaybeSave(true);
            RefreshUi();
        }

        void HandleRewarded()
        {
            if (!_booted || !CanTick())
                return;

            YG2.RewardedAdvShow("hpBoost", () =>
            {
                if (_combat == null || _combat.IsWon || _blockPlay)
                    return;
                float pct = balance.GetRewardedPercent(_combat.PhaseIndex);
                double dmg = System.Math.Min(_combat.HpMax * pct, _combat.HpLeft);
                if (dmg <= 0d)
                    return;
                _economy.AddIncome(dmg);
                if (_combat.ApplyDamage(dmg))
                    BeginInterlude();
                MaybeSave(true);
                RefreshUi();
            });
        }

        void ToggleMute()
        {
            ApplyMute(!YG2.saves.muted, true);
            RefreshUi();
        }

        void ApplyMute(bool muted, bool save)
        {
            YG2.saves.muted = muted;
            AudioListener.volume = muted ? 0f : 1f;
            if (save)
                MaybeSave(true);
        }

        void BeginInterlude()
        {
            if (_blockPlay)
                return;

            _blockPlay = true;
            SetPlaying(false);
            YG2.GameplayStop();
            RefreshEnemySprites(true);

            int phase = _combat.PhaseIndex;
            if (slots != null)
            {
                slots.PlaySwap(phase, () => OpenBubble(phase));
            }
            else
            {
                OpenBubble(phase);
            }
        }

        void OpenBubble(int phase)
        {
            string line = dialogs != null ? dialogs.GetPhaseLine(phase) : string.Empty;
            Transform head = null;
            if (slots != null)
            {
                var enemy = slots.GetEnemy(phase % 3);
                if (enemy != null)
                    head = enemy.HeadAnchor;
            }

            if (bubble != null)
                bubble.Show(line, head);
            else
                HandleBubbleContinue();
        }

        void HandleBubbleContinue()
        {
            if (!_blockPlay)
                return;
            if (bubble != null)
                bubble.Hide();

            bool lastPhase = _combat.PhaseIndex >= _combat.PhaseCount - 1;
            if (lastPhase)
            {
                FinishInterlude();
                return;
            }

            _ads.ShowThen(FinishInterlude);
        }

        void FinishInterlude()
        {
            _combat.AdvanceAfterInterlude();
            MaybeSave(true);

            if (_combat.IsWon)
            {
                ShowVictory();
                return;
            }

            if (_combat.HasPendingInterlude)
            {
                _blockPlay = false;
                BeginInterlude();
                return;
            }

            if (slots != null)
                slots.SnapToPhase(_combat.PhaseIndex);
            RefreshEnemySprites(false);
            SetPlaying(true);
            YG2.GameplayStart();
            RefreshUi();
        }

        void ShowVictory()
        {
            _blockPlay = true;
            SetPlaying(false);
            YG2.GameplayStop();
            if (bubble != null)
                bubble.Hide();
            if (victory != null)
                victory.Show(dialogs != null ? dialogs.VictoryText : Loc.VictoryBody);
            RefreshUi();
            MaybeSave(true);
        }

        void SetPlaying(bool playing)
        {
            if (battleZone != null)
                battleZone.SetClicksEnabled(playing);
            if (hud != null)
                hud.SetRewardedInteractable(playing);
        }

        void RefreshEnemySprites(bool afterKill)
        {
            if (slots == null || _combat == null)
                return;
            for (int i = 0; i < 3; i++)
            {
                var view = slots.GetEnemy(i);
                if (view != null)
                    view.ApplyStage(_combat.GetSpriteIndex(i, afterKill));
            }
        }

        void RefreshUi()
        {
            if (_economy == null || _combat == null)
                return;
            if (shop != null)
                shop.Refresh(_economy);
            if (hud != null)
            {
                float pct = balance != null ? balance.GetRewardedPercent(_combat.PhaseIndex) : 0.1f;
                hud.Refresh(_economy, _combat, pct, YG2.saves.muted);
            }
        }

        void HandleLang(string _)
        {
            RefreshUi();
            if (bubble != null && bubble.gameObject.activeInHierarchy && _combat != null)
            {
                var enemy = slots != null ? slots.GetEnemy(_combat.ActiveEnemyIndex) : null;
                bubble.Show(dialogs != null ? dialogs.GetPhaseLine(_combat.PhaseIndex) : string.Empty,
                    enemy != null ? enemy.HeadAnchor : null);
            }
        }

        void MaybeSave(bool force)
        {
            if (!force && !_dirty)
                return;
            if (!force && Time.unscaledTime - _lastSave < 1f)
                return;
            YG2.SaveProgress();
            _lastSave = Time.unscaledTime;
            _dirty = false;
        }
    }
}
