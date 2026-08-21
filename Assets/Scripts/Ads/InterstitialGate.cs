using System;
using System.Collections;
using UnityEngine;
using YG;

namespace Clicker
{
    public sealed class InterstitialGate
    {
        readonly MonoBehaviour _host;
        Action _onDone;
        Coroutine _wait;

        public InterstitialGate(MonoBehaviour host)
        {
            _host = host;
        }

        public void ShowThen(Action onDone)
        {
            Cancel();
            _onDone = onDone;

            YG2.onCloseInterAdvWasShow += HandleClosed;
            YG2.onErrorInterAdv += HandleError;
            YG2.InterstitialAdvShow();
            _wait = _host.StartCoroutine(WaitForCloseOrSkip());
        }

        public void Cancel()
        {
            YG2.onCloseInterAdvWasShow -= HandleClosed;
            YG2.onErrorInterAdv -= HandleError;
            if (_wait != null && _host != null)
                _host.StopCoroutine(_wait);
            _wait = null;
            _onDone = null;
        }

        void HandleClosed(bool _)
        {
            Finish();
        }

        void HandleError()
        {
            Finish();
        }

        IEnumerator WaitForCloseOrSkip()
        {
            yield return null;
            yield return null;

            if (!YG2.nowAdsShow)
            {
                Finish();
                yield break;
            }

            float t = 0f;
            while (t < 120f && YG2.nowAdsShow)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Finish();
        }

        void Finish()
        {
            var done = _onDone;
            Cancel();
            done?.Invoke();
        }
    }
}
