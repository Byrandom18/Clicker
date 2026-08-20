using System.Collections;
using UnityEngine;
using YG;

namespace Clicker
{
    [DefaultExecutionOrder(-50)]
    public class ClickerGame : MonoBehaviour
    {
        public BalanceConfig balance;
        public EnemyCatalog enemies;
        public DialogCatalog dialogs;
        public ClickerView view;

        EconomyService _economy;
        CombatService _combat;
        bool _booted;
        bool _ready;
        bool _waitingAd;
        bool _dirty;
        float _lastSave = -10f;
        float _hudAcc;
        Coroutine _adSafety;

        void Awake()
        {
            Application.targetFrameRate = 60;
            if (view == null)
                view = GetComponent<ClickerView>();
            LoadConfigs();
        }

        void Start()
        {
            YG2.onGetSDKData += Boot;
            if (YG2.isSDKEnabled)
                Boot();
        }

        void OnDestroy()
        {
            YG2.onGetSDKData -= Boot;
            YG2.onPauseGame -= HandlePause;
            YG2.onSwitchLang -= HandleLang;
            YG2.onCloseInterAdvWasShow -= HandleAdClosed;
        }

        void LoadConfigs()
        {
            if (balance == null)
                balance = Resources.Load<BalanceConfig>("Clicker/BalanceConfig");
            if (balance == null)
                balance = BalanceDefaults.CreateBalance();

            if (enemies == null)
                enemies = Resources.Load<EnemyCatalog>("Clicker/EnemyCatalog");
            if (enemies == null)
                enemies = BalanceDefaults.CreateEnemies();

            if (dialogs == null)
                dialogs = Resources.Load<DialogCatalog>("Clicker/DialogCatalog");
            if (dialogs == null)
                dialogs = BalanceDefaults.CreateDialogs();
        }

        void Boot()
        {
            YG2.onGetSDKData -= Boot;
            if (_booted)
                return;
            _booted = true;

            if (view == null)
            {
                Debug.LogError("ClickerGame: назначьте ClickerView на объекте сцены.");
                return;
            }

            YG2.HideBanner();
            SaveUtil.EnsureArrays(balance.ShopCount);

            _economy = new EconomyService(balance);
            _combat = new CombatService(balance, _economy);

            if (!YG2.saves.clickerInitialized)
            {
                _combat.InitializeNewRun();
                YG2.saves.clickerInitialized = true;
                SaveNow();
            }
            else
            {
                _combat.Restore();
                _economy.Recalc();
            }

            view.Bind(_economy, _combat, balance, enemies, dialogs);
            view.OnEnemyClicked += HandleClick;
            view.OnBuy += HandleBuy;
            view.OnContinue += HandleContinue;
            view.OnRewarded += HandleRewarded;
            view.OnMute += HandleMute;

            _combat.OnPhaseCleared += HandlePhaseCleared;
            _combat.OnVictory += HandleVictoryReached;
            _combat.OnChanged += MarkDirty;

            YG2.onPauseGame += HandlePause;
            YG2.onSwitchLang += HandleLang;
            ApplyMute();
            view.RefreshAll();

            if (YG2.saves.gameWon)
            {
                view.ShowVictory();
                YG2.GameplayStop();
            }
            else if (_combat.HasPendingInterlude)
            {
                view.ShowDialog(_combat.PhaseIndex);
                view.BeginTransition(_combat.PhaseIndex);
                YG2.GameplayStop();
            }
            else if (!YG2.isPauseGame)
            {
                YG2.GameplayStart();
            }

            _ready = true;
        }

        void Update()
        {
            LimitUltraWide();
            if (!_ready || _combat == null)
                return;

            _hudAcc += Time.unscaledDeltaTime;
            if (_hudAcc >= 0.2f)
            {
                _hudAcc = 0f;
                view.RefreshHud();
                view.RefreshShop();
            }

            if (_dirty && Time.unscaledTime - _lastSave > 2f)
                SaveNow();

            if (_combat.Locked || _combat.Won || _waitingAd)
                return;
            if (YG2.isPauseGame || !YG2.isFocusWindowGame || YG2.nowAdsShow)
                return;
            if (Time.timeScale <= 0f)
                return;

            double idle = _economy.IdlePerSecond * Time.deltaTime;
            if (idle > 0d)
                _combat.ApplyDamage(idle);
        }

        void LimitUltraWide()
        {
            var cam = Camera.main;
            if (cam == null || Screen.height <= 0)
                return;
            float aspect = (float)Screen.width / Screen.height;
            if (aspect > 2f)
            {
                float w = 2f / aspect;
                cam.rect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            else
                cam.rect = new Rect(0f, 0f, 1f, 1f);
        }

        void HandleClick()
        {
            if (!_ready || _combat.Locked || _combat.Won || YG2.nowAdsShow)
                return;
            _combat.ApplyDamage(_economy.ClickPower);
            view.PunchActive();
            view.RefreshHud();
            MarkDirty();
        }

        void HandleBuy(int shopIndex)
        {
            if (!_ready || _combat.Won)
                return;
            if (!_economy.TryBuy(shopIndex))
                return;
            view.RefreshShop();
            view.RefreshHud();
            SaveNow();
        }

        void HandlePhaseCleared(int phase)
        {
            YG2.GameplayStop();
            view.ShowDialog(phase);
            view.BeginTransition(phase);
            view.RefreshHud();
            SaveNow();
        }

        void HandleContinue()
        {
            if (!_ready || _waitingAd || !_combat.HasPendingInterlude)
                return;

            view.HideDialog();
            view.SnapRotation();

            if (YG2.isTimerAdvCompleted && !YG2.nowAdsShow)
            {
                _waitingAd = true;
                YG2.onCloseInterAdvWasShow += HandleAdClosed;
                YG2.InterstitialAdvShow();
                if (_adSafety != null)
                    StopCoroutine(_adSafety);
                _adSafety = StartCoroutine(AdSafetyTimeout());
            }
            else
                FinishInterlude();
        }

        IEnumerator AdSafetyTimeout()
        {
            float t = 0f;
            while (_waitingAd && !YG2.nowAdsShow && t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_waitingAd && !YG2.nowAdsShow)
            {
                YG2.onCloseInterAdvWasShow -= HandleAdClosed;
                _waitingAd = false;
                FinishInterlude();
            }
        }

        void HandleAdClosed(bool shown)
        {
            YG2.onCloseInterAdvWasShow -= HandleAdClosed;
            _waitingAd = false;
            if (_adSafety != null)
            {
                StopCoroutine(_adSafety);
                _adSafety = null;
            }

            FinishInterlude();
        }

        void FinishInterlude()
        {
            _combat.AdvanceAfterInterlude();
            SaveNow();
            if (_combat.Won || _combat.Locked)
                return;
            view.HideDialog();
            view.RefreshAll();
            if (!YG2.isPauseGame)
                YG2.GameplayStart();
        }

        void HandleVictoryReached()
        {
            view.ShowVictory();
            YG2.GameplayStop();
            SaveNow();
            if (YG2.reviewCanShow)
                YG2.ReviewShow();
        }

        void HandleRewarded()
        {
            if (!_ready || _combat.Locked || _combat.Won || YG2.nowAdsShow)
                return;

            YG2.RewardedAdvShow("hp_boost", () =>
            {
                float pct = balance.GetRewardedPercent(_combat.PhaseIndex);
                _combat.ApplyDamage(_combat.HpMax * pct);
                view.RefreshHud();
                view.RefreshRewarded();
                SaveNow();
            });
        }

        void HandleMute()
        {
            YG2.saves.muted = !YG2.saves.muted;
            ApplyMute();
            view.RefreshMute();
            SaveNow();
        }

        void ApplyMute()
        {
            AudioListener.volume = YG2.saves.muted ? 0f : 1f;
        }

        void HandlePause(bool paused)
        {
            if (!_ready)
                return;
            if (!paused && !_combat.Locked && !_combat.Won && !_waitingAd)
                YG2.GameplayStart();
        }

        void HandleLang(string lang)
        {
            if (view != null)
                view.RefreshAll();
        }

        void MarkDirty() => _dirty = true;

        void SaveNow()
        {
            _dirty = false;
            _lastSave = Time.unscaledTime;
            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
    }
}
