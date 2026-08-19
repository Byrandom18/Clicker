using System.Collections;
using UnityEngine;
using YG;

namespace Clicker
{
    [DefaultExecutionOrder(-50)]
    public class ClickerGame : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInstance()
        {
            if (FindFirstObjectByType<ClickerGame>() != null)
                return;
            new GameObject("ClickerRoot").AddComponent<ClickerGame>();
        }

        public BalanceConfig balance;
        public EnemyCatalog enemies;
        public DialogCatalog dialogs;

        EconomyService _economy;
        CombatService _combat;
        ClickerView _view;
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
            LoadConfigs();
            TuneCamera();
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

        void TuneCamera()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiFactory.Hex("120E1C");
        }

        void Boot()
        {
            YG2.onGetSDKData -= Boot;
            if (_booted)
                return;
            _booted = true;

            YG2.HideBanner();
            SaveUtil.EnsureArrays();

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

            _view = gameObject.AddComponent<ClickerView>();
            _view.Build(_economy, _combat, balance, enemies, dialogs);
            _view.OnEnemyClicked += HandleClick;
            _view.OnBuy += HandleBuy;
            _view.OnContinue += HandleContinue;
            _view.OnRewarded += HandleRewarded;
            _view.OnMute += HandleMute;

            _combat.OnPhaseCleared += HandlePhaseCleared;
            _combat.OnVictory += HandleVictoryReached;
            _combat.OnChanged += MarkDirty;

            YG2.onPauseGame += HandlePause;
            YG2.onSwitchLang += HandleLang;
            ApplyMute();
            _view.RefreshAll();

            if (YG2.saves.gameWon)
            {
                _view.ShowVictory();
                YG2.GameplayStop();
            }
            else if (_combat.HasPendingInterlude)
            {
                _view.ShowDialog(_combat.PhaseIndex);
                _view.BeginTransition(_combat.PhaseIndex);
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
                _view.RefreshHud();
                _view.RefreshShop();
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
            {
                cam.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        void HandleClick()
        {
            if (!_ready || _combat.Locked || _combat.Won || YG2.nowAdsShow)
                return;
            _combat.ApplyDamage(_economy.ClickPower);
            _view.PunchActive();
            _view.RefreshHud();
            MarkDirty();
        }

        void HandleBuy(bool idle, int index)
        {
            if (!_ready || _combat.Won)
                return;
            if (!_economy.TryBuy(idle, index))
                return;
            _view.RefreshShop();
            _view.RefreshHud();
            SaveNow();
        }

        void HandlePhaseCleared(int phase)
        {
            YG2.GameplayStop();
            _view.ShowDialog(phase);
            _view.BeginTransition(phase);
            _view.RefreshHud();
            SaveNow();
        }

        void HandleContinue()
        {
            if (!_ready || _waitingAd || !_combat.HasPendingInterlude)
                return;

            _view.HideDialog();
            _view.SnapRotation();

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
            {
                FinishInterlude();
            }
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
            if (_combat.Won)
                return;
            if (_combat.Locked)
                return;

            _view.HideDialog();
            _view.RefreshAll();
            if (!YG2.isPauseGame)
                YG2.GameplayStart();
        }

        void HandleVictoryReached()
        {
            _view.ShowVictory();
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
                _view.RefreshHud();
                _view.RefreshRewarded();
                SaveNow();
            });
        }

        void HandleMute()
        {
            YG2.saves.muted = !YG2.saves.muted;
            ApplyMute();
            _view.RefreshMute();
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
            if (_view != null)
                _view.RefreshAll();
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
