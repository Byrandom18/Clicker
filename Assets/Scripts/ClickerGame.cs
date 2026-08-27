using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

namespace Clicker
{
    // Scene: GameRoot/ClickerGame. Data lives in Assets/Resources/Data.
    // Prefabs: Assets/Prefabs. Missing inspector refs are resolved at boot.
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

        [Header("Timing")]
        [SerializeField] float idleAdSeconds = 5f;
        [SerializeField] float phaseClearDelay = 0.55f;

        EconomyService _economy;
        CombatService _combat;
        InterstitialGate _ads;
        bool _booted;
        bool _blockPlay;
        bool _dirty;
        bool _idleAdBusy;
        float _lastSave;
        float _lastActivity;
        float _hudAcc;
        Coroutine _interludeRoutine;
        const float AutoSaveInterval = 30f;

        void Awake()
        {
            Application.targetFrameRate = 60;
            ResolveSceneRefs();
            if (balance == null)
                balance = ClickerCatalog.LoadBalance();
            if (dialogs == null)
                dialogs = ClickerCatalog.LoadDialogs();
            _ads = new InterstitialGate(this);
        }

        void ResolveSceneRefs()
        {
            if (slots == null)
                slots = FindFirstObjectByType<EnemySlotDirector>(FindObjectsInactive.Include);
            if (battleZone == null)
                battleZone = FindFirstObjectByType<BattleZoneClick>(FindObjectsInactive.Include);
            if (shop == null)
                shop = FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);
            if (hud == null)
                hud = FindFirstObjectByType<HudView>(FindObjectsInactive.Include);
            if (bubble == null)
                bubble = FindFirstObjectByType<SpeechBubbleView>(FindObjectsInactive.Include);
            if (victory == null)
                victory = FindFirstObjectByType<VictoryView>(FindObjectsInactive.Include);
        }

        void OnEnable()
        {
            BattleZoneClick.Pressed += HandleClick;
            ShopRowView.BuyClicked += HandleBuy;
            if (bubble != null)
                bubble.ContinueClicked += HandleBubbleContinue;
            if (victory != null)
                victory.ContinueClicked += HandleVictoryContinue;
            YG2.onSwitchLang += HandleLang;
            YG2.onCloseAnyAdv += HandleAnyAdClosed;
        }

        void OnDisable()
        {
            BattleZoneClick.Pressed -= HandleClick;
            ShopRowView.BuyClicked -= HandleBuy;
            if (bubble != null)
                bubble.ContinueClicked -= HandleBubbleContinue;
            if (victory != null)
                victory.ContinueClicked -= HandleVictoryContinue;
            YG2.onSwitchLang -= HandleLang;
            YG2.onCloseAnyAdv -= HandleAnyAdClosed;
            StopInterludeRoutine();
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
            StopInterludeRoutine();
            FlushSave();
        }

        void OnApplicationQuit()
        {
            FlushSave();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
                FlushSave();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
                FlushSave();
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

            MaybeIdleAd();
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
                Debug.LogError("ClickerGame: BalanceConfig not assigned and Resources/Data/Balance missing.");
                return;
            }

            _economy = new EconomyService(balance);
            _combat = new CombatService(balance);
            bool wiped = ClickerSave.Sanitize();
            _combat.InitFromSave();
            if (wiped)
                FlushSave();

            if (slots != null)
                slots.EnsureBound();

            var catalog = ClickerCatalog.LoadUpgrades();
            if (shop != null)
            {
                shop.Collect();
                if (catalog.Length == 0)
                {
                    var defs = new List<UpgradeDef>();
                    var rows = shop.Rows;
                    for (int i = 0; i < rows.Length; i++)
                    {
                        if (rows[i] != null && rows[i].Definition != null)
                            defs.Add(rows[i].Definition);
                    }

                    catalog = defs.ToArray();
                }
            }

            _economy.SetDefinitions(catalog);

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
                hud.SnapHearts(_combat.IsWon || _combat.IsEndless
                    ? CombatService.CampaignStagesPerEnemy
                    : _combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
            }

            MarkActivity();

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

            MarkActivity();
            PunchActive();
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
            MarkActivity();
            MaybeSave(true);
            RefreshUi();
        }

        void HandleRewarded()
        {
            if (!_booted || !CanTick())
                return;

            MarkActivity();
            YG2.RewardedAdvShow("hpBoost", () =>
            {
                if (_combat == null || _combat.IsWon || _blockPlay)
                    return;
                float pct = balance.GetRewardedPercent(_combat.PhaseIndex);
                double dmg = System.Math.Min(_combat.HpMax * pct, _combat.HpLeft);
                if (dmg <= 0d)
                    return;
                PunchActive();
                _economy.AddIncome(dmg);
                if (_combat.ApplyDamage(dmg))
                    BeginInterlude();
                MaybeSave(true);
                RefreshUi();
            });
        }

        void ToggleMute()
        {
            ApplyMute(!YG2.saves.muted, false);
            _dirty = true;
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
            if (slots != null)
                slots.KillAllClickPunches();

            int phase = _combat.PhaseIndex;
            if (hud != null && !_combat.IsEndless)
            {
                hud.SnapHearts(_combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
                hud.PlayDestroy(_combat.ActiveStageIndex);
            }

            StopInterludeRoutine();
            _interludeRoutine = StartCoroutine(PhaseClearThenSwap(phase));
        }

        IEnumerator PhaseClearThenSwap(int phase)
        {
            if (phaseClearDelay > 0f)
                yield return new WaitForSecondsRealtime(phaseClearDelay);

            if (slots != null)
                slots.KillAllClickPunches();
            RefreshEnemySprites(true);

            if (slots != null)
                slots.PlaySwap(phase, () => OpenBubble(phase));
            else
                OpenBubble(phase);

            _interludeRoutine = null;
        }

        void OpenBubble(int completedPhase)
        {
            string line = dialogs != null ? dialogs.GetPhaseLine(completedPhase) : string.Empty;
            Transform head = null;
            if (slots != null)
            {
                bool lastCampaign = _combat != null && !_combat.IsEndless && completedPhase >= _combat.PhaseCount - 1;
                int speaker = lastCampaign ? completedPhase % 3 : (completedPhase + 1) % 3;
                var enemy = slots.GetEnemy(speaker);
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
            MarkActivity();
            if (bubble != null)
                bubble.Hide();

            bool lastCampaign = !_combat.IsEndless && _combat.PhaseIndex >= _combat.PhaseCount - 1;
            if (lastCampaign)
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

            _blockPlay = false;
            if (slots != null)
                slots.SnapToPhase(_combat.PhaseIndex);
            RefreshEnemySprites(false);
            if (hud != null)
                hud.SnapHearts(_combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
            SetPlaying(true);
            YG2.GameplayStart();
            MarkActivity();
            RefreshUi();
        }

        void ShowVictory()
        {
            _blockPlay = true;
            SetPlaying(false);
            YG2.GameplayStop();
            if (bubble != null)
                bubble.Hide();
            if (hud != null)
                hud.SnapHearts(CombatService.CampaignStagesPerEnemy);
            RefreshEnemySprites(false);
            if (victory != null)
                victory.Show(dialogs != null ? dialogs.VictoryText : Loc.VictoryBody);
            RefreshUi();
            MaybeSave(true);
        }

        void HandleVictoryContinue()
        {
            if (!_booted || _combat == null || !_combat.IsWon)
                return;

            _combat.StartEndless();
            if (victory != null)
                victory.Hide();
            if (bubble != null)
                bubble.Hide();

            if (_combat.HasPendingInterlude)
            {
                _blockPlay = false;
                BeginInterlude();
                MaybeSave(true);
                return;
            }

            _blockPlay = false;
            if (slots != null)
                slots.SnapToPhase(_combat.PhaseIndex);
            RefreshEnemySprites(false);
            if (hud != null)
                hud.SnapHearts(_combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
            SetPlaying(true);
            YG2.GameplayStart();
            MarkActivity();
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
                hud.Refresh(_economy, _combat, YG2.saves.muted);
        }

        void HandleLang(string _)
        {
            RefreshUi();
            if (victory != null && victory.gameObject.activeInHierarchy && _combat != null && _combat.IsWon)
                victory.Show(dialogs != null ? dialogs.VictoryText : Loc.VictoryBody);
            if (bubble != null && bubble.gameObject.activeInHierarchy && _combat != null)
            {
                bool useNext = _blockPlay && !_combat.IsWon
                               && (_combat.IsEndless || _combat.PhaseIndex < _combat.PhaseCount - 1);
                int speaker = useNext ? (_combat.PhaseIndex + 1) % 3 : _combat.ActiveEnemyIndex;
                var enemy = slots != null ? slots.GetEnemy(speaker) : null;
                bubble.Show(dialogs != null ? dialogs.GetPhaseLine(_combat.PhaseIndex) : string.Empty,
                    enemy != null ? enemy.HeadAnchor : null);
            }
        }

        void MarkActivity()
        {
            _lastActivity = Time.unscaledTime;
        }

        void HandleAnyAdClosed()
        {
            _idleAdBusy = false;
            MarkActivity();
        }

        void MaybeIdleAd()
        {
            if (!_booted || _idleAdBusy || !CanTick())
                return;
            if (idleAdSeconds <= 0f || Time.unscaledTime - _lastActivity < idleAdSeconds)
                return;
            if (!YG2.isTimerAdvCompleted || YG2.nowAdsShow)
                return;

            _idleAdBusy = true;
            MarkActivity();
            _ads.ShowThen(() =>
            {
                _idleAdBusy = false;
                MarkActivity();
            });
        }

        void PunchActive()
        {
            if (slots == null || _combat == null)
                return;
            var view = slots.GetEnemy(_combat.ActiveEnemyIndex);
            if (view != null)
                view.PlayClickPunch();
        }

        void StopInterludeRoutine()
        {
            if (_interludeRoutine == null)
                return;
            StopCoroutine(_interludeRoutine);
            _interludeRoutine = null;
        }

        void MaybeSave(bool force)
        {
            if (!_booted)
                return;
            if (!force && !_dirty)
                return;
            if (!force && Time.unscaledTime - _lastSave < AutoSaveInterval)
                return;
            FlushSave();
        }

        void FlushSave()
        {
            if (!_booted)
                return;
            YG2.SaveProgress();
            _lastSave = Time.unscaledTime;
            _dirty = false;
        }
    }
}
