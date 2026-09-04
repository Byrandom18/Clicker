using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] DamagePopupPool damagePopups;

        [Header("Timing")]
        [SerializeField] float bubbleVisibleSeconds = 3f;
        [SerializeField] float phaseClearDelay = 0.55f;
        [SerializeField] float minClickInterval = 0.1f;

        [Header("Stage VFX")]
        [SerializeField] GameObject stageChangeVfxPrefab;
        [SerializeField] float stageCoverSeconds = 1f;
        [SerializeField] float stageCoverPeak = 0.15f;
        [SerializeField, Range(0.4f, 3f), Tooltip("Множитель размера облака относительно спрайта.")]
        float stageCoverSize = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Непрозрачность облака. 0 — полностью прозрачное, 1 — плотное.")]
        float stageCoverOpacity = 0.82f;
        [SerializeField, Range(0.3f, 4f), Tooltip("Множитель размера префаба Cartoon FX.")]
        float stageVfxScale = 1f;
        [SerializeField, Tooltip("Смещение точки спавна эффекта смены фазы относительно центра врага.")]
        Vector2 stageVfxOffset;

        [Header("Hit VFX")]
        [SerializeField] GameObject clickVfxPrefab;
        [SerializeField] GameObject rewardedVfxPrefab;
        [SerializeField, Range(0.15f, 2.5f)] float clickVfxScale = 0.4f;
        [SerializeField, Range(0.3f, 3f)] float rewardedVfxScale = 1.25f;
        [SerializeField, Tooltip("Смещение точки спавна эффекта атаки за рекламу относительно центра врага.")]
        Vector2 rewardedVfxOffset;

        [Header("Audio")]
        [SerializeField] SfxController sfx;
        [SerializeField] MusicController music;

        [Header("Auto Upgrade")]
        [SerializeField] float autoUpgradeSeconds = 120f;
        [SerializeField] float autoUpgradeInterval = 0.3f;
        [SerializeField] float autoUpgradeClicksPerSecond = 3f;

        EconomyService _economy;
        CombatService _combat;
        bool _booted;
        bool _blockPlay;
        bool _swapping;
        bool _dirty;
        bool _musicUnlocked;
        float _lastSave;
        float _lastClickTime = float.NegativeInfinity;
        float _hudAcc;
        float _autoUpgradeAcc;
        int _screenW;
        int _screenH;
        Coroutine _swapRoutine;
        Coroutine _bubbleRoutine;
        GameObject _stageBurstGo;
        GameObject _stageCoverGo;
        HitVfxPool _clickHits;
        HitVfxPool _rewardedHits;
        int _bubbleLinePhase = -1;
        const float AutoSaveInterval = 5f;

        void Awake()
        {
            Application.targetFrameRate = 60;
            AspectLetterbox.Ensure();
            EnsureLegacyUiInput();
            ResolveSceneRefs();
            if (balance == null)
                balance = ClickerCatalog.LoadBalance();
            if (dialogs == null)
                dialogs = ClickerCatalog.LoadDialogs();
            ApplyMusicMute(YG2.saves.musicMuted, false);
        }

        static void EnsureLegacyUiInput()
        {
            var es = EventSystem.current != null
                ? EventSystem.current
                : FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (es == null)
                return;

            var modules = es.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null && modules[i].GetType().Name == "InputSystemUIInputModule")
                    Destroy(modules[i]);
            }

            if (es.GetComponent<StandaloneInputModule>() == null)
                es.gameObject.AddComponent<StandaloneInputModule>();
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
            if (damagePopups == null)
                damagePopups = FindFirstObjectByType<DamagePopupPool>(FindObjectsInactive.Include);
            if (sfx == null)
                sfx = FindFirstObjectByType<SfxController>(FindObjectsInactive.Include);
            if (music == null)
                music = FindFirstObjectByType<MusicController>(FindObjectsInactive.Include);
            EnsureSfx();
            EnsureMusic();
        }

        void EnsureSfx()
        {
            if (sfx != null)
                return;
            var go = new GameObject("Sfx");
            go.transform.SetParent(transform, false);
            sfx = go.AddComponent<SfxController>();
        }

        void EnsureMusic()
        {
            if (music != null)
                return;
            var go = new GameObject("Music");
            go.transform.SetParent(transform, false);
            music = go.AddComponent<MusicController>();
        }

        void OnEnable()
        {
            BattleZoneClick.Pressed += HandleClick;
            ShopRowView.BuyClicked += HandleBuy;
            if (victory != null)
                victory.ContinueClicked += HandleVictoryContinue;
            YG2.onSwitchLang += HandleLang;
            YG2.onCloseAnyAdv += HandleAnyAdClosed;
            YG2.onHideWindowGame += FlushSave;
            YG2.onPauseGame += HandlePauseForMusic;
        }

        void OnDisable()
        {
            BattleZoneClick.Pressed -= HandleClick;
            ShopRowView.BuyClicked -= HandleBuy;
            if (victory != null)
                victory.ContinueClicked -= HandleVictoryContinue;
            YG2.onSwitchLang -= HandleLang;
            YG2.onCloseAnyAdv -= HandleAnyAdClosed;
            YG2.onHideWindowGame -= FlushSave;
            YG2.onPauseGame -= HandlePauseForMusic;
            StopSwapRoutine();
            StopBubbleRoutine();
            StopStageVfx(true);
            _swapping = false;
            if (slots != null)
                slots.KillTween();
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
            StopSwapRoutine();
            StopBubbleRoutine();
            StopStageVfx(true);
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
            TryUnlockMusic();
            MaybeSaveOnResize();

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

            TickAutoUpgrade();
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
            ApplyMusicMute(YG2.saves.musicMuted, false);
            if (slots != null)
            {
                slots.SnapToPhase(_combat.PhaseIndex);
                RefreshEnemySprites(false);
            }

            if (bubble != null)
                bubble.Hide();
            if (victory != null)
                victory.Hide();
            EnsureDamagePopups();
            EnsureHitVfx();

            if (hud != null)
            {
                if (hud.MuteButton != null)
                    hud.MuteButton.onClick.AddListener(ToggleMute);
                if (hud.MusicMuteButton != null)
                    hud.MusicMuteButton.onClick.AddListener(ToggleMusicMute);
                if (hud.RewardedButton != null)
                    hud.RewardedButton.onClick.AddListener(HandleRewarded);
                if (hud.AutoUpgradeButton != null)
                    hud.AutoUpgradeButton.onClick.AddListener(HandleAutoUpgrade);
                hud.SnapHearts(_combat.IsWon || _combat.IsEndless
                    ? CombatService.CampaignStagesPerEnemy
                    : _combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
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
                   && !YG2.isPauseGame
                   && YG2.isFocusWindowGame
                   && !YG2.nowAdsShow
                   && Time.timeScale > 0f;
        }

        void HandleClick(Vector2 screenPos)
        {
            UnlockMusic();
            if (!_booted || !CanTick())
                return;
            if (Time.unscaledTime - _lastClickTime < minClickInterval)
                return;
            _lastClickTime = Time.unscaledTime;

            if (!_combat.HasPendingInterlude && !_swapping)
                PunchActive();
            double amount = _economy.ClickPower;
            _economy.AddIncome(amount);
            Sfx.Click();
            PlayClickHit(screenPos);
            if (damagePopups != null)
                damagePopups.Spawn(amount, screenPos);
            if (_combat.ApplyDamage(amount))
                BeginInterlude();
            _dirty = true;
            RefreshUi();
        }

        void HandleBuy(UpgradeDef def)
        {
            UnlockMusic();
            if (!_booted || _blockPlay || _combat.IsWon || def == null)
                return;
            if (!_economy.TryBuy(def))
                return;
            Sfx.Buy();
            MaybeSave(true);
            RefreshUi();
        }

        void HandleRewarded()
        {
            UnlockMusic();
            if (!_booted || !CanTick() || _combat.HasPendingInterlude)
                return;

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
                Sfx.Explosion();
                PlayRewardedHit(dmg);
                if (_combat.ApplyDamage(dmg))
                    BeginInterlude();
                MaybeSave(true);
                RefreshUi();
            });
        }

        void ToggleMute()
        {
            UnlockMusic();
            bool next = !YG2.saves.muted;
            ApplyMute(next, false);
            if (!next)
                Sfx.Ui();
            _dirty = true;
            RefreshUi();
        }

        void ToggleMusicMute()
        {
            UnlockMusic();
            ApplyMusicMute(!YG2.saves.musicMuted, false);
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

        void ApplyMusicMute(bool muted, bool save)
        {
            YG2.saves.musicMuted = muted;
            if (music != null)
                music.SetMuted(muted);
            if (!muted)
                StartMusicIfAllowed();

            if (save)
                MaybeSave(true);
        }

        void HandleAutoUpgrade()
        {
            UnlockMusic();
            if (!_booted || !CanTick() || _combat.HasPendingInterlude)
                return;

            YG2.RewardedAdvShow("autoUpgrade", () =>
            {
                if (_combat == null || _combat.IsWon || _blockPlay)
                    return;
                YG2.saves.autoUpgradeLeft = Mathf.Max(0.1f, autoUpgradeSeconds);
                _autoUpgradeAcc = 0f;
                TryAutoBuy();
                RefreshBonusButtons();
                MaybeSave(true);
                RefreshUi();
            });
        }

        void TickAutoUpgrade()
        {
            if (!_booted || YG2.saves.autoUpgradeLeft <= 0f)
                return;

            if (CanTick())
            {
                YG2.saves.autoUpgradeLeft -= Time.deltaTime;
                _autoUpgradeAcc += Time.deltaTime;
                float interval = Mathf.Max(0.05f, autoUpgradeInterval);
                while (_autoUpgradeAcc >= interval)
                {
                    _autoUpgradeAcc -= interval;
                    TryAutoBuy();
                }
            }

            if (YG2.saves.autoUpgradeLeft <= 0f)
            {
                YG2.saves.autoUpgradeLeft = 0f;
                _autoUpgradeAcc = 0f;
                RefreshBonusButtons();
                RefreshUi();
            }
        }

        void TryAutoBuy()
        {
            if (_economy == null || !CanTick())
                return;
            var def = _economy.FindBestValueBuy(autoUpgradeClicksPerSecond);
            if (def == null)
                return;
            if (shop != null)
            {
                var row = shop.FindRow(def);
                if (row != null)
                    row.PlayBuyFeedback();
            }

            HandleBuy(def);
        }

        void RefreshBonusButtons()
        {
            if (hud == null || _combat == null)
                return;
            bool allowRewards = _booted && !_blockPlay && !_combat.IsWon && !_combat.HasPendingInterlude;
            hud.SetRewardedInteractable(allowRewards);
            hud.SetAutoUpgradeInteractable(allowRewards && YG2.saves.autoUpgradeLeft <= 0f);
        }

        void BeginInterlude()
        {
            if (_blockPlay || _swapping)
                return;

            _swapping = true;
            StopBubbleRoutine();
            if (bubble != null)
                bubble.Hide();

            if (slots != null)
                slots.KillAllClickPunches();

            SetPlaying(false);

            if (hud != null && !_combat.IsEndless)
            {
                hud.SnapHearts(_combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
                hud.PlayDestroy(_combat.ActiveStageIndex);
            }

            RefreshUi();

            int phase = _combat.PhaseIndex;
            StopSwapRoutine();
            _swapRoutine = StartCoroutine(PhaseClearThenSwap(phase));
        }

        IEnumerator PhaseClearThenSwap(int phase)
        {
            var view = slots != null ? slots.GetEnemy(phase % 3) : null;
            Vector3 pos = (view != null ? view.WorldCenter : Vector3.zero) + (Vector3)stageVfxOffset;
            Vector3 size = view != null ? view.WorldSize : new Vector3(2.2f, 3.2f, 0f);

            float fadeIn = Mathf.Max(0.05f, stageCoverPeak);
            float fadeOut = 0.25f;
            float hold = Mathf.Max(0f, stageCoverSeconds);
            float total = fadeIn + hold + fadeOut;

            StopStageVfx(false);
            Sfx.Phase();
            if (stageChangeVfxPrefab != null)
                _stageBurstGo = StageChangeVfx.SpawnBurst(
                    stageChangeVfxPrefab, pos, StageChangeVfx.ScaleFor(size) * stageVfxScale);
            _stageCoverGo = StageChangeVfx.SpawnCover(
                pos, size, fadeIn, hold, fadeOut, stageCoverSize, stageCoverOpacity);

            if (fadeIn > 0f)
                yield return new WaitForSecondsRealtime(fadeIn);

            if (view != null)
                view.SetSpriteVisible(false);
            RefreshEnemySprites(true);

            if (hold > 0f)
                yield return new WaitForSecondsRealtime(hold);

            if (view != null)
                view.SetSpriteVisible(true);

            if (fadeOut > 0f)
                yield return new WaitForSecondsRealtime(fadeOut);

            float extra = phaseClearDelay - total;
            if (extra > 0f)
                yield return new WaitForSecondsRealtime(extra);

            if (slots != null)
                slots.KillAllClickPunches();

            if (slots != null)
            {
                _swapRoutine = null;
                slots.PlaySwap(phase, () => OnSwapComplete(phase));
                yield break;
            }

            _swapRoutine = null;
            OnSwapComplete(phase);
        }

        void OnSwapComplete(int completedPhase)
        {
            _swapping = false;
            ShowAllEnemySprites();
            if (_combat == null)
                return;

            _combat.AdvanceAfterInterlude();
            MaybeSave(true);

            if (_combat.IsWon)
            {
                if (hud != null)
                    hud.SnapHearts(CombatService.CampaignStagesPerEnemy);
                RefreshEnemySprites(false);
                RefreshUi();
                ShowPhaseBubble(completedPhase, completedPhase % 3);
                return;
            }

            if (_combat.HasPendingInterlude)
            {
                if (hud != null)
                    hud.SnapHearts(_combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
                RefreshUi();
                BeginInterlude();
                return;
            }

            if (slots != null)
                slots.SnapToPhase(_combat.PhaseIndex);
            RefreshEnemySprites(false);
            if (hud != null)
                hud.SnapHearts(_combat.CompletedStagesFor(_combat.ActiveEnemyIndex));
            SetPlaying(true);
            YG2.GameplayStart();
            RefreshUi();
            ShowPhaseBubble(completedPhase, _combat.ActiveEnemyIndex);
        }

        void ShowPhaseBubble(int completedPhase, int speaker)
        {
            Transform head = null;
            if (slots != null)
            {
                var enemy = slots.GetEnemy(speaker);
                if (enemy != null)
                    head = enemy.HeadAnchor;
            }

            _bubbleLinePhase = completedPhase;
            if (bubble != null)
            {
                string line = dialogs != null ? dialogs.GetPhaseLine(completedPhase) : string.Empty;
                bubble.Show(line, head);
            }

            StopBubbleRoutine();
            _bubbleRoutine = StartCoroutine(BubbleThenHide());
        }

        IEnumerator BubbleThenHide()
        {
            float wait = bubbleVisibleSeconds > 0f ? bubbleVisibleSeconds : 3f;
            if (wait > 0f)
                yield return new WaitForSecondsRealtime(wait);
            _bubbleRoutine = null;
            HidePhaseBubble();
        }

        void HidePhaseBubble()
        {
            if (bubble != null)
                bubble.Hide();
            _bubbleLinePhase = -1;
            if (_combat != null && _combat.IsWon)
                ShowVictory();
        }

        void ShowVictory()
        {
            StopBubbleRoutine();
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
            UnlockMusic();
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
            RefreshUi();
            MaybeSave(true);
        }

        void SetPlaying(bool playing)
        {
            if (battleZone != null)
                battleZone.SetClicksEnabled(playing);
            if (!playing)
            {
                if (hud != null)
                {
                    hud.SetRewardedInteractable(false);
                    hud.SetAutoUpgradeInteractable(false);
                }
                return;
            }

            RefreshBonusButtons();
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
                hud.Refresh(_economy, _combat, YG2.saves.muted, YG2.saves.musicMuted, YG2.saves.autoUpgradeLeft);
        }

        void HandleLang(string _)
        {
            RefreshUi();
            if (victory != null && victory.gameObject.activeInHierarchy && _combat != null && _combat.IsWon)
                victory.Show(dialogs != null ? dialogs.VictoryText : Loc.VictoryBody);
            if (bubble != null && bubble.gameObject.activeInHierarchy && _combat != null && _bubbleLinePhase >= 0)
            {
                int speaker = _combat.IsWon ? _bubbleLinePhase % 3 : _combat.ActiveEnemyIndex;
                var enemy = slots != null ? slots.GetEnemy(speaker) : null;
                bubble.Show(dialogs != null ? dialogs.GetPhaseLine(_bubbleLinePhase) : string.Empty,
                    enemy != null ? enemy.HeadAnchor : null);
            }
        }

        void HandleAnyAdClosed()
        {
            RefreshBonusButtons();
            StartMusicIfAllowed();
        }

        void TryUnlockMusic()
        {
            if (_musicUnlocked)
                return;
            if (!Input.GetMouseButtonDown(0) && Input.touchCount == 0)
                return;
            UnlockMusic();
        }

        void UnlockMusic()
        {
            if (_musicUnlocked)
            {
                StartMusicIfAllowed();
                return;
            }

            _musicUnlocked = true;
            StartMusicIfAllowed();
        }

        void HandlePauseForMusic(bool paused)
        {
            if (!paused)
                StartMusicIfAllowed();
        }

        void StartMusicIfAllowed()
        {
            if (!_musicUnlocked || music == null)
                return;
            if (YG2.saves.musicMuted || YG2.isPauseGame || YG2.nowAdsShow)
                return;
            music.Play();
        }

        void MaybeSaveOnResize()
        {
            int w = Screen.width;
            int h = Screen.height;
            if (w == _screenW && h == _screenH)
                return;
            _screenW = w;
            _screenH = h;
            if (_booted)
                FlushSave();
        }

        void EnsureDamagePopups()
        {
            if (damagePopups != null)
                return;
            damagePopups = FindFirstObjectByType<DamagePopupPool>(FindObjectsInactive.Include);
            if (damagePopups != null)
                return;

            Transform canvasRoot = null;
            if (battleZone != null)
            {
                var canvas = battleZone.GetComponentInParent<Canvas>();
                if (canvas != null)
                    canvasRoot = canvas.transform;
            }

            if (canvasRoot == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                    canvasRoot = canvas.transform;
            }

            if (canvasRoot == null)
                return;
            damagePopups = DamagePopupPool.Create(canvasRoot);
        }

        void EnsureHitVfx()
        {
            if (clickVfxPrefab == null)
                clickVfxPrefab = ClickerCatalog.LoadClickHitVfx();
            if (rewardedVfxPrefab == null)
                rewardedVfxPrefab = ClickerCatalog.LoadRewardedHitVfx();

            if (_clickHits == null && clickVfxPrefab != null)
                _clickHits = HitVfxPool.Create("ClickHits", transform, clickVfxPrefab, 10, clickVfxScale, false, true);
            if (_rewardedHits == null && rewardedVfxPrefab != null)
                _rewardedHits = HitVfxPool.Create("RewardedHits", transform, rewardedVfxPrefab, 2, rewardedVfxScale, true, false);
        }

        void PlayClickHit(Vector2 screenPos)
        {
            if (_clickHits != null)
                _clickHits.Play(ScreenToWorld(screenPos), clickVfxScale);
        }

        void PlayRewardedHit(double dmg)
        {
            Vector3 world = ActiveEnemyWorld();
            Vector3 vfxPos = world + (Vector3)rewardedVfxOffset;
            if (_rewardedHits != null)
                _rewardedHits.Play(vfxPos, rewardedVfxScale);
            if (damagePopups != null)
                damagePopups.SpawnMega(dmg, WorldToScreen(world));
        }

        Vector3 ActiveEnemyWorld()
        {
            if (slots == null || _combat == null)
                return Vector3.zero;
            var view = slots.GetEnemy(_combat.ActiveEnemyIndex);
            return view != null ? view.WorldCenter : Vector3.zero;
        }

        static Vector3 ScreenToWorld(Vector2 screen)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return Vector3.zero;
            float z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        }

        static Vector2 WorldToScreen(Vector3 world)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector3 screen = cam.WorldToScreenPoint(world);
            return new Vector2(screen.x, screen.y);
        }

        void PunchActive()
        {
            if (slots == null || _combat == null)
                return;
            var view = slots.GetEnemy(_combat.ActiveEnemyIndex);
            if (view != null)
                view.PlayClickPunch(slots.ClickPunchScale);
        }

        void StopSwapRoutine()
        {
            if (_swapRoutine == null)
                return;
            StopCoroutine(_swapRoutine);
            _swapRoutine = null;
            StopStageVfx(true);
        }

        void StopStageVfx(bool showSprites)
        {
            if (_stageBurstGo != null)
            {
                Destroy(_stageBurstGo);
                _stageBurstGo = null;
            }

            if (_stageCoverGo != null)
            {
                Destroy(_stageCoverGo);
                _stageCoverGo = null;
            }

            if (showSprites)
                ShowAllEnemySprites();
        }

        void ShowAllEnemySprites()
        {
            if (slots == null)
                return;
            for (int i = 0; i < 3; i++)
            {
                var view = slots.GetEnemy(i);
                if (view != null)
                    view.SetSpriteVisible(true);
            }
        }

        void StopBubbleRoutine()
        {
            if (_bubbleRoutine == null)
                return;
            StopCoroutine(_bubbleRoutine);
            _bubbleRoutine = null;
            _bubbleLinePhase = -1;
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
