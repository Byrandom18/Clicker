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
        [SerializeField] float duration = 0.6f;
        [SerializeField] Ease ease = Ease.InOutQuad;

        static readonly Color Dim = new Color(0.55f, 0.55f, 0.55f, 1f);
        Sequence _seq;

        public EnemyView[] Enemies => new[] { enemyA, enemyB, enemyC };

        public EnemyView GetEnemy(int index)
        {
            switch (index)
            {
                case 0: return enemyA;
                case 1: return enemyB;
                default: return enemyC;
            }
        }

        public void SnapToPhase(int phaseIndex)
        {
            KillTween();
            ApplyPose(phaseIndex, true);
        }

        public void PlaySwap(int fromPhaseIndex, Action onComplete)
        {
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
                _seq.Join(view.transform.DOMove(slot.position, duration).SetEase(ease));
                _seq.Join(view.transform.DOScale(slot.localScale, duration).SetEase(ease));
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
                    view.transform.position = slot.position;
                    view.transform.localScale = slot.localScale;
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
