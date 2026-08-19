using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YG;

namespace Clicker
{
    public class HoldPulse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action pulse;
        bool _held;
        float _timer;

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = true;
            _timer = 0f;
            pulse?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => _held = false;
        public void OnPointerExit(PointerEventData eventData) => _held = false;

        void Update()
        {
            if (!_held)
                return;
            _timer += Time.unscaledDeltaTime;
            if (_timer >= 0.38f)
            {
                pulse?.Invoke();
                _timer = 0.28f;
            }
        }
    }

    public class ClickerView : MonoBehaviour
    {
        public event Action OnEnemyClicked;
        public event Action OnContinue;
        public event Action OnRewarded;
        public event Action OnMute;
        public event Action<bool, int> OnBuy;

        EconomyService _economy;
        CombatService _combat;
        BalanceConfig _balance;
        EnemyCatalog _enemies;
        DialogCatalog _dialogs;

        Canvas _canvas;
        Image _background;
        Image[] _enemyImages;
        Text[] _enemyNames;
        RectTransform[] _enemyRects;
        Button _enemyButton;
        RectTransform _clickCatcher;
        Image _hpFill;
        Text _hpText;
        Text _scoreText;
        Text _powerText;
        Text _howTo;
        Image[] _dots;
        Button[] _tabButtons;
        bool _idleTab;
        RectTransform _shopContent;
        ShopRow[] _rows;
        Button _rewarded;
        Text _rewardedLabel;
        Button _mute;
        Text _muteLabel;
        GameObject _dialogRoot;
        Text _dialogText;
        GameObject _victoryRoot;
        Text _victoryBody;
        readonly Sprite[][] _generatedSprites = new Sprite[3][];
        Coroutine _rotateRoutine;

        struct ShopRow
        {
            public GameObject root;
            public Text title;
            public Text detail;
            public Text cost;
            public Button buy;
            public Text buyLabel;
        }

        public void Build(EconomyService economy, CombatService combat, BalanceConfig balance, EnemyCatalog enemies, DialogCatalog dialogs)
        {
            _economy = economy;
            _combat = combat;
            _balance = balance;
            _enemies = enemies;
            _dialogs = dialogs;

            UiFactory.Init();
            UiFactory.EnsureEventSystem();
            BuildPlaceholders();

            var canvasGo = new GameObject("ClickerCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            var root = canvasGo.GetComponent<RectTransform>();
            ApplySafeArea(root);

            _background = UiFactory.Image("Background", root, UiFactory.Bg);
            UiFactory.Stretch(_background.rectTransform);
            _background.raycastTarget = true;
            if (_enemies != null && _enemies.background != null)
            {
                _background.sprite = _enemies.background;
                _background.preserveAspect = false;
                _background.color = Color.white;
            }

            BuildTop(root);
            BuildArena(root);
            BuildShop(root);
            BuildDialog(root);
            BuildVictory(root);
            RefreshAll();
        }

        void ApplySafeArea(RectTransform root)
        {
            var safe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0)
                return;
            var min = safe.position;
            var max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            root.anchorMin = min;
            root.anchorMax = max;
        }

        void BuildPlaceholders()
        {
            for (int e = 0; e < 3; e++)
            {
                _generatedSprites[e] = new Sprite[4];
                Color color = UiFactory.Accent;
                var def = _enemies != null ? _enemies.Get(e) : null;
                if (def != null)
                    color = def.placeholderColor;
                for (int s = 0; s < 4; s++)
                    _generatedSprites[e][s] = UiFactory.BodySprite(color, s);
            }
        }

        void BuildTop(RectTransform root)
        {
            var bar = UiFactory.Panel("TopBar", root, new Color(0f, 0f, 0f, 0.28f));
            UiFactory.SetAnchors(bar, new Vector2(0f, 0.88f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);

            _scoreText = UiFactory.Label("Score", bar, 40, UiFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.SetAnchors(_scoreText.rectTransform, new Vector2(0.02f, 0.45f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);

            _powerText = UiFactory.Label("Power", bar, 22, UiFactory.Text, TextAnchor.MiddleLeft);
            UiFactory.SetAnchors(_powerText.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.72f, 0.5f), Vector2.zero, Vector2.zero);

            _howTo = UiFactory.Label("HowTo", bar, 16, UiFactory.Muted, TextAnchor.MiddleRight);
            _howTo.horizontalOverflow = HorizontalWrapMode.Wrap;
            _howTo.verticalOverflow = VerticalWrapMode.Truncate;
            UiFactory.SetAnchors(_howTo.rectTransform, new Vector2(0.55f, 0.08f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);

            var dotsHost = UiFactory.Root("Dots", root);
            UiFactory.SetAnchors(dotsHost, new Vector2(0.02f, 0.82f), new Vector2(0.52f, 0.88f), Vector2.zero, Vector2.zero);
            _dots = new Image[12];
            for (int i = 0; i < 12; i++)
            {
                int group = i / 4;
                int slot = i % 4;
                var image = UiFactory.Image("Dot" + i, dotsHost, UiFactory.Muted);
                var rt = image.rectTransform;
                float x0 = group * 0.34f + slot * 0.07f;
                UiFactory.SetAnchors(rt, new Vector2(x0, 0.2f), new Vector2(x0 + 0.055f, 0.8f), Vector2.zero, Vector2.zero);
                _dots[i] = image;
            }

            _mute = UiFactory.Button("Mute", root, "♪", 26);
            UiFactory.SetAnchors(_mute.GetComponent<RectTransform>(), new Vector2(0.64f, 0.92f), new Vector2(0.71f, 0.99f), Vector2.zero, Vector2.zero);
            _mute.onClick.AddListener(() => OnMute?.Invoke());
            _muteLabel = _mute.GetComponentInChildren<Text>();

            _rewarded = UiFactory.Button("Rewarded", root, "", 18);
            UiFactory.SetAnchors(_rewarded.GetComponent<RectTransform>(), new Vector2(0.02f, 0.03f), new Vector2(0.28f, 0.11f), Vector2.zero, Vector2.zero);
            _rewarded.onClick.AddListener(() => OnRewarded?.Invoke());
            _rewardedLabel = _rewarded.GetComponentInChildren<Text>();
            _rewarded.GetComponent<Image>().color = UiFactory.Green;
        }

        void BuildArena(RectTransform root)
        {
            var arena = UiFactory.Root("Arena", root);
            UiFactory.SetAnchors(arena, new Vector2(0f, 0.12f), new Vector2(0.72f, 0.82f), Vector2.zero, Vector2.zero);

            _enemyImages = new Image[3];
            _enemyNames = new Text[3];
            _enemyRects = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var image = UiFactory.Image("Enemy" + i, arena, Color.white);
                image.preserveAspect = true;
                image.raycastTarget = false;
                _enemyImages[i] = image;
                _enemyRects[i] = image.rectTransform;

                var name = UiFactory.Label("Name", image.transform, 20, UiFactory.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                UiFactory.SetAnchors(name.rectTransform, new Vector2(-0.1f, -0.18f), new Vector2(1.1f, 0.02f), Vector2.zero, Vector2.zero);
                _enemyNames[i] = name;
            }

            var catcher = UiFactory.Image("ClickCatcher", arena, new Color(1f, 1f, 1f, 0f));
            catcher.raycastTarget = true;
            UiFactory.SetAnchors(catcher.rectTransform, new Vector2(0.32f, 0.05f), new Vector2(0.68f, 0.88f), Vector2.zero, Vector2.zero);
            _clickCatcher = catcher.rectTransform;
            _enemyButton = catcher.gameObject.AddComponent<Button>();
            _enemyButton.targetGraphic = catcher;
            _enemyButton.transition = Selectable.Transition.None;
            _enemyButton.onClick.AddListener(() => OnEnemyClicked?.Invoke());

            var hpBg = UiFactory.Image("HpBg", arena, UiFactory.HpBg);
            UiFactory.SetAnchors(hpBg.rectTransform, new Vector2(0.18f, 0.90f), new Vector2(0.82f, 0.98f), Vector2.zero, Vector2.zero);
            hpBg.raycastTarget = false;

            _hpFill = UiFactory.Image("HpFill", hpBg.transform, UiFactory.Hp);
            var fillRt = _hpFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = new Vector2(4, 4);
            fillRt.offsetMax = new Vector2(-4, -4);
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = 0;

            _hpText = UiFactory.Label("HpText", hpBg.transform, 20, UiFactory.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Stretch(_hpText.rectTransform);
        }

        void BuildShop(RectTransform root)
        {
            var shop = UiFactory.Panel("Shop", root, UiFactory.PanelColor);
            UiFactory.SetAnchors(shop, new Vector2(0.72f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            var title = UiFactory.Label("Title", shop, 30, UiFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.SetAnchors(title.rectTransform, new Vector2(0.04f, 0.93f), new Vector2(0.96f, 0.995f), Vector2.zero, Vector2.zero);
            title.text = Loc.Shop;

            _tabButtons = new Button[2];
            _tabButtons[0] = UiFactory.Button("TabClick", shop, Loc.TabClick, 20);
            UiFactory.SetAnchors(_tabButtons[0].GetComponent<RectTransform>(), new Vector2(0.04f, 0.86f), new Vector2(0.48f, 0.925f), Vector2.zero, Vector2.zero);
            _tabButtons[0].onClick.AddListener(() => { _idleTab = false; RefreshShop(); });

            _tabButtons[1] = UiFactory.Button("TabIdle", shop, Loc.TabIdle, 20);
            UiFactory.SetAnchors(_tabButtons[1].GetComponent<RectTransform>(), new Vector2(0.52f, 0.86f), new Vector2(0.96f, 0.925f), Vector2.zero, Vector2.zero);
            _tabButtons[1].onClick.AddListener(() => { _idleTab = true; RefreshShop(); });

            var viewport = UiFactory.Panel("Viewport", shop, new Color(0f, 0f, 0f, 0.12f));
            UiFactory.SetAnchors(viewport, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.85f), Vector2.zero, Vector2.zero);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            _shopContent = UiFactory.Root("Content", viewport);
            _shopContent.anchorMin = new Vector2(0f, 1f);
            _shopContent.anchorMax = new Vector2(1f, 1f);
            _shopContent.pivot = new Vector2(0.5f, 1f);
            _shopContent.offsetMin = Vector2.zero;
            _shopContent.offsetMax = Vector2.zero;
            var layout = _shopContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = _shopContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _shopContent;
            scroll.viewport = viewport;

            _rows = new ShopRow[12];
            for (int i = 0; i < 12; i++)
                _rows[i] = CreateRow(i);
        }

        ShopRow CreateRow(int index)
        {
            var row = new ShopRow();
            var panel = UiFactory.Panel("Row" + index, _shopContent, UiFactory.PanelAlt);
            var le = panel.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 96;
            le.preferredHeight = 96;
            row.root = panel.gameObject;

            row.title = UiFactory.Label("Title", panel, 20, UiFactory.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.SetAnchors(row.title.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.70f, 0.95f), Vector2.zero, Vector2.zero);

            row.detail = UiFactory.Label("Detail", panel, 16, UiFactory.Muted, TextAnchor.MiddleLeft);
            UiFactory.SetAnchors(row.detail.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.70f, 0.52f), Vector2.zero, Vector2.zero);

            row.buy = UiFactory.Button("Buy", panel, Loc.Buy, 16);
            UiFactory.SetAnchors(row.buy.GetComponent<RectTransform>(), new Vector2(0.70f, 0.18f), new Vector2(0.97f, 0.82f), Vector2.zero, Vector2.zero);
            row.buyLabel = row.buy.GetComponentInChildren<Text>();
            row.cost = row.buyLabel;

            int captured = index;
            var hold = row.buy.gameObject.AddComponent<HoldPulse>();
            hold.pulse = () => OnBuy?.Invoke(_idleTab, captured);
            row.buy.onClick.RemoveAllListeners();
            return row;
        }

        void BuildDialog(RectTransform root)
        {
            var overlay = UiFactory.Panel("Dialog", root, new Color(0.05f, 0.03f, 0.08f, 0.55f));
            UiFactory.Stretch(overlay);
            _dialogRoot = overlay.gameObject;
            _dialogRoot.SetActive(false);

            var box = UiFactory.Panel("Box", overlay, UiFactory.PanelColor);
            UiFactory.SetAnchors(box, new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.72f), Vector2.zero, Vector2.zero);

            _dialogText = UiFactory.Label("Text", box, 28, UiFactory.Text, TextAnchor.MiddleCenter);
            _dialogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _dialogText.verticalOverflow = VerticalWrapMode.Overflow;
            UiFactory.SetAnchors(_dialogText.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);

            var cont = UiFactory.Button("Continue", box, Loc.Continue, 26);
            UiFactory.SetAnchors(cont.GetComponent<RectTransform>(), new Vector2(0.30f, 0.08f), new Vector2(0.70f, 0.24f), Vector2.zero, Vector2.zero);
            cont.onClick.AddListener(() => OnContinue?.Invoke());
        }

        void BuildVictory(RectTransform root)
        {
            var overlay = UiFactory.Panel("Victory", root, new Color(0.05f, 0.03f, 0.08f, 0.78f));
            UiFactory.Stretch(overlay);
            _victoryRoot = overlay.gameObject;
            _victoryRoot.SetActive(false);

            var title = UiFactory.Label("Title", overlay, 64, UiFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.SetAnchors(title.rectTransform, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.78f), Vector2.zero, Vector2.zero);
            title.text = Loc.VictoryTitle;

            _victoryBody = UiFactory.Label("Body", overlay, 32, UiFactory.Text, TextAnchor.MiddleCenter);
            _victoryBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.SetAnchors(_victoryBody.rectTransform, new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.58f), Vector2.zero, Vector2.zero);
        }

        public void RefreshAll()
        {
            RefreshHud();
            RefreshShop();
            RefreshEnemies(true);
            RefreshMute();
            RefreshRewarded();
            if (_howTo != null)
                _howTo.text = Loc.HowToPlay;
        }

        public void RefreshHud()
        {
            if (_scoreText == null)
                return;
            _scoreText.text = Loc.Score + ": " + NumberFormatter.Format(_economy.Score);
            _powerText.text = Loc.ClickPower + " " + NumberFormatter.Format(_economy.ClickPower) +
                              "   " + Loc.IdlePower + " " + NumberFormatter.Format(_economy.IdlePerSecond) + Loc.PerSecond;

            double max = _combat.HpMax;
            _hpFill.fillAmount = _combat.HpNormalized;
            _hpText.text = NumberFormatter.Format(_combat.HpLeft) + " / " + NumberFormatter.Format(max);

            for (int i = 0; i < _dots.Length; i++)
            {
                if (i < _combat.PhaseIndex)
                    _dots[i].color = UiFactory.Green;
                else if (i == _combat.PhaseIndex && !_combat.Won)
                    _dots[i].color = UiFactory.Accent;
                else
                    _dots[i].color = UiFactory.Muted;
            }

            bool canPlay = !_combat.Locked && !_combat.Won;
            if (_enemyButton != null)
                _enemyButton.interactable = canPlay;
            if (_rewarded != null)
                _rewarded.interactable = canPlay && !YG2.nowAdsShow;
        }

        public void RefreshShop()
        {
            if (_rows == null)
                return;

            if (_tabButtons != null)
            {
                _tabButtons[0].GetComponent<Image>().color = _idleTab ? UiFactory.ButtonColor : UiFactory.Accent;
                _tabButtons[1].GetComponent<Image>().color = _idleTab ? UiFactory.Accent : UiFactory.ButtonColor;
                _tabButtons[0].GetComponentInChildren<Text>().color = _idleTab ? UiFactory.Text : Color.black;
                _tabButtons[1].GetComponentInChildren<Text>().color = _idleTab ? Color.black : UiFactory.Text;
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                var def = _economy.Def(_idleTab, i);
                if (def == null)
                    continue;
                bool unlocked = _economy.IsUnlocked(_idleTab, i);
                int owned = _economy.Owned(_idleTab, i);
                _rows[i].title.text = def.DisplayName;
                _rows[i].detail.text = Loc.PlusPower(def.powerPerCopy, _idleTab) + "  ·  " + owned + " " + Loc.Owned;
                if (!unlocked)
                {
                    _rows[i].buy.interactable = false;
                    _rows[i].buyLabel.text = Loc.Locked;
                    _rows[i].buyLabel.fontSize = 13;
                }
                else
                {
                    double cost = _economy.NextCost(_idleTab, i);
                    _rows[i].buy.interactable = _economy.CanAfford(_idleTab, i) && !_combat.Won;
                    _rows[i].buyLabel.fontSize = 16;
                    _rows[i].buyLabel.text = Loc.Buy + "\n" + NumberFormatter.Format(cost);
                }
            }
        }

        public void RefreshMute()
        {
            if (_muteLabel == null)
                return;
            _muteLabel.text = YG2.saves.muted ? "✕♪" : "♪";
        }

        public void RefreshRewarded()
        {
            if (_rewardedLabel == null)
                return;
            _rewardedLabel.text = Loc.RewardedButton(_balance.GetRewardedPercent(_combat.PhaseIndex));
        }

        public void RefreshEnemies(bool instant)
        {
            RefreshEnemies(instant, _combat.PhaseIndex);
        }

        public void RefreshEnemies(bool instant, int visualPhase)
        {
            int phase = visualPhase;
            for (int e = 0; e < 3; e++)
            {
                int stage = CombatService.SpriteStageForEnemy(e, phase);
                _enemyImages[e].sprite = ResolveSprite(e, stage);
                _enemyImages[e].color = Color.white;
                var def = _enemies != null ? _enemies.Get(e) : null;
                _enemyNames[e].text = def != null ? def.DisplayName : Loc.Enemy + " " + (e + 1);
            }

            PlaceEnemies(phase);
        }

        public void BeginTransition(int fromPhase)
        {
            int toPhase = Mathf.Min(fromPhase + 1, 11);
            for (int e = 0; e < 3; e++)
            {
                int stage = CombatService.SpriteStageForEnemy(e, toPhase);
                _enemyImages[e].sprite = ResolveSprite(e, stage);
                var def = _enemies != null ? _enemies.Get(e) : null;
                _enemyNames[e].text = def != null ? def.DisplayName : Loc.Enemy + " " + (e + 1);
            }

            PlaceEnemies(fromPhase);
            PlayRotation(fromPhase, toPhase, null);
        }

        Sprite ResolveSprite(int enemy, int stage)
        {
            var def = _enemies != null ? _enemies.Get(enemy) : null;
            var sprite = def != null ? def.GetSprite(stage) : null;
            if (sprite != null)
                return sprite;
            return _generatedSprites[enemy][Mathf.Clamp(stage, 0, 3)];
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
            RefreshEnemies(true);
            float t = 0f;
            const float duration = 0.7f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                BlendPlaces(fromPhase, toPhase, k);
                yield return null;
            }

            PlaceEnemies(toPhase);
            _rotateRoutine = null;
            done?.Invoke();
        }

        void BlendPlaces(int fromPhase, int toPhase, float k)
        {
            for (int e = 0; e < 3; e++)
            {
                GetSlot(fromPhase, e, out var aPos, out var aScale, out int aOrder, out var aColor);
                GetSlot(toPhase, e, out var bPos, out var bScale, out int bOrder, out var bColor);
                _enemyRects[e].anchorMin = Vector2.Lerp(aPos, bPos, k);
                _enemyRects[e].anchorMax = _enemyRects[e].anchorMin;
                _enemyRects[e].pivot = new Vector2(0.5f, 0.12f);
                _enemyRects[e].anchoredPosition = Vector2.zero;
                _enemyRects[e].sizeDelta = Vector2.Lerp(aScale, bScale, k);
                _enemyImages[e].color = Color.Lerp(aColor, bColor, k);
                _enemyImages[e].transform.SetSiblingIndex(Mathf.RoundToInt(Mathf.Lerp(aOrder, bOrder, k)));
            }

            if (_clickCatcher != null)
                _clickCatcher.SetAsLastSibling();
        }

        void PlaceEnemies(int phase)
        {
            BlendPlaces(phase, phase, 1f);
            if (_enemyButton != null)
                _enemyButton.interactable = !_combat.Locked && !_combat.Won;
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
            _dialogRoot.SetActive(true);
            _dialogText.text = _dialogs != null ? _dialogs.GetPhaseLine(phaseIndex) : string.Empty;
            RefreshHud();
        }

        public void HideDialog()
        {
            _dialogRoot.SetActive(false);
        }

        public void ShowVictory()
        {
            HideDialog();
            _victoryRoot.SetActive(true);
            _victoryBody.text = _dialogs != null ? _dialogs.VictoryText : Loc.VictoryBody;
            RefreshHud();
        }

        public void PunchActive()
        {
            int active = _combat.PhaseIndex % 3;
            if (_punchRoutine != null)
                StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchRoutine(_enemyRects[active]));
        }

        Coroutine _punchRoutine;

        System.Collections.IEnumerator PunchRoutine(RectTransform rt)
        {
            Vector3 baseScale = rt.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 8f;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                rt.localScale = baseScale * s;
                yield return null;
            }
            rt.localScale = baseScale;
            _punchRoutine = null;
        }
    }
}
