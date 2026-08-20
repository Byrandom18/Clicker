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
        Coroutine _safety;

        public InterstitialGate(MonoBehaviour host)
        {
            _host = host;
        }

        public void ShowThen(Action onDone)
        {
            Cancel();
            _onDone = onDone;
            YG2.onCloseInterAdvWasShow += HandleClosed;
            YG2.InterstitialAdvShow();
            _safety = _host.StartCoroutine(Safety());
        }

        public void Cancel()
        {
            YG2.onCloseInterAdvWasShow -= HandleClosed;
            if (_safety != null && _host != null)
                _host.StopCoroutine(_safety);
            _safety = null;
            _onDone = null;
        }

        void HandleClosed(bool wasShown)
        {
            Finish();
        }

        IEnumerator Safety()
        {
            float t = 0f;
            while (t < 0.45f && !YG2.nowAdsShow)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!YG2.nowAdsShow)
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
