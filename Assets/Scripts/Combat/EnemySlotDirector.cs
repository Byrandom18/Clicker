using System;
using DG.Tweening;
using UnityEngine;

namespace Clicker
{
    public class EnemySlotDirector : MonoBehaviour
    {
        [SerializeField] Transform slotLeft;
        [SerializeField] Transform slotCenter;
        [SerializeField] Transform slotRight;
        [SerializeField] EnemyView enemyA;
        [SerializeField] EnemyView enemyB;
        [SerializeField] EnemyView enemyC;
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] float duration = 0.6f;
        [SerializeField] Ease ease = Ease.InOutQuad;

        static readonly Color Dim = new Color(0.55f, 0.55f, 0.55f, 1f);
        Sequence _seq;

        public EnemyView[] Enemies => new[] { enemyA, enemyB, enemyC };

        public void EnsureBound()
        {
            if (slotLeft == null)
                slotLeft = transform.Find("SlotLeft");
            if (slotCenter == null)
                slotCenter = transform.Find("SlotCenter");
            if (slotRight == null)
                slotRight = transform.Find("SlotRight");

            if (enemyA == null)
                enemyA = FindOrSpawn("EnemyA", "enemy_a");
            if (enemyB == null)
                enemyB = FindOrSpawn("EnemyB", "enemy_b");
            if (enemyC == null)
                enemyC = FindOrSpawn("EnemyC", "enemy_c");

            ApplyDefinitions();
        }

        void ApplyDefinitions()
        {
            AssignDef(enemyA, "enemy_a");
            AssignDef(enemyB, "enemy_b");
            AssignDef(enemyC, "enemy_c");
        }

        static void AssignDef(EnemyView view, string id)
        {
            if (view == null)
                return;
            if (view.Definition == null)
                view.SetDefinition(ClickerCatalog.FindEnemy(id));
        }

        EnemyView FindOrSpawn(string objectName, string defId)
        {
            var existing = FindInScene(objectName);
            if (existing != null)
                return existing;

            GameObject prefab = enemyPrefab != null ? enemyPrefab : ClickerCatalog.LoadEnemyPrefab();
            if (prefab == null)
            {
                Debug.LogError("Clicker: EnemyView prefab missing. Put it at Assets/Prefabs or Assets/Resources/Prefabs.");
                return null;
            }

            var go = Instantiate(prefab);
            go.name = objectName;
            var view = go.GetComponent<EnemyView>();
            if (view != null)
                view.SetDefinition(ClickerCatalog.FindEnemy(defId));
            return view;
        }

        static EnemyView FindInScene(string objectName)
        {
            var views = FindObjectsByType<EnemyView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view != null && view.gameObject.scene.IsValid() && view.gameObject.name == objectName)
                    return view;
            }

            return null;
        }

        public EnemyView GetEnemy(int index)
        {
            switch (index)
            {
                case 0: return enemyA;
                case 1: return enemyB;
                default: return enemyC;
            }
        }

        public void KillAllClickPunches()
        {
            if (enemyA != null)
                enemyA.KillClickPunch();
            if (enemyB != null)
                enemyB.KillClickPunch();
            if (enemyC != null)
                enemyC.KillClickPunch();
        }

        public void SnapToPhase(int phaseIndex)
        {
            KillAllClickPunches();
            KillTween();
            ApplyPose(phaseIndex, true);
        }

        public void PlaySwap(int fromPhaseIndex, Action onComplete)
        {
            KillAllClickPunches();
            KillTween();
            int toPhase = fromPhaseIndex + 1;
            ApplyPose(fromPhaseIndex, false);

            _seq = DOTween.Sequence().SetUpdate(true);
            for (int i = 0; i < 3; i++)
            {
                var view = GetEnemy(i);
                var slot = SlotForEnemy(i, toPhase);
                if (view == null || slot == null)
                    continue;
                bool center = i == toPhase % 3;
                view.transform.SetParent(slot, true);
                _seq.Join(view.transform.DOLocalMove(Vector3.zero, duration).SetEase(ease));
                _seq.Join(view.transform.DOScale(Vector3.one, duration).SetEase(ease));
                if (view.Renderer != null)
                    _seq.Join(view.Renderer.DOColor(center ? Color.white : Dim, duration).SetEase(ease));
            }

            _seq.OnComplete(() =>
            {
                ApplySorting(toPhase);
                onComplete?.Invoke();
            });
        }

        public void KillTween()
        {
            if (_seq != null && _seq.IsActive())
                _seq.Kill();
            _seq = null;
        }

        void OnDestroy()
        {
            KillTween();
        }

        void ApplyPose(int phaseIndex, bool snap)
        {
            for (int i = 0; i < 3; i++)
            {
                var view = GetEnemy(i);
                var slot = SlotForEnemy(i, phaseIndex);
                if (view == null || slot == null)
                    continue;

                bool center = i == phaseIndex % 3;
                if (snap)
                {
                    view.transform.SetParent(slot, false);
                    view.transform.localPosition = Vector3.zero;
                    view.transform.localRotation = Quaternion.identity;
                    view.transform.localScale = Vector3.one;
                    if (view.Renderer != null)
                        view.Renderer.color = center ? Color.white : Dim;
                }

                if (view.Renderer != null)
                    view.Renderer.sortingOrder = center ? 10 : 5;
            }
        }

        void ApplySorting(int phaseIndex)
        {
            for (int i = 0; i < 3; i++)
            {
                var view = GetEnemy(i);
                if (view != null && view.Renderer != null)
                    view.Renderer.sortingOrder = i == phaseIndex % 3 ? 10 : 5;
            }
        }

        Transform SlotForEnemy(int enemyIndex, int phaseIndex)
        {
            // Start phase 0: A center, B right, C left.
            // After n completions, center = n % 3, right = (n+1)%3, left = (n+2)%3.
            int center = Mod(phaseIndex, 3);
            int right = Mod(phaseIndex + 1, 3);
            int left = Mod(phaseIndex + 2, 3);
            if (enemyIndex == center)
                return slotCenter;
            if (enemyIndex == right)
                return slotRight;
            if (enemyIndex == left)
                return slotLeft;
            return slotCenter;
        }

        static int Mod(int v, int m)
        {
            int r = v % m;
            return r < 0 ? r + m : r;
        }
    }
}
