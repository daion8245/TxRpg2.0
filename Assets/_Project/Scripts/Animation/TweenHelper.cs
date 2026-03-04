using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace TxRpg.Animation
{
    /// <summary>
    /// UniTask 기반 경량 트윈 유틸리티.
    /// DOTween 설치 후에는 DOTween 래퍼로 교체할 수 있습니다.
    /// </summary>
    public static class TweenHelper
    {
        public static async UniTask MoveToAsync(Transform t, Vector3 target, float duration, CancellationToken ct = default)
        {
            var start = t.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                t.position = Vector3.Lerp(start, target, EaseOutQuad(progress));
                await UniTask.Yield(ct);
            }

            t.position = target;
        }

        public static async UniTask ScalePunchAsync(Transform t, Vector3 punch, float duration, CancellationToken ct = default)
        {
            var originalScale = t.localScale;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            // 확대
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / halfDuration);
                t.localScale = originalScale + punch * EaseOutQuad(progress);
                await UniTask.Yield(ct);
            }

            // 복귀
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / halfDuration);
                t.localScale = (originalScale + punch) - punch * EaseOutQuad(progress);
                await UniTask.Yield(ct);
            }

            t.localScale = originalScale;
        }

        public static async UniTask ColorFlashAsync(SpriteRenderer sr, Color flashColor, float duration, CancellationToken ct = default)
        {
            if (sr == null) return;

            var originalColor = sr.color;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            // 플래시 색으로
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / halfDuration);
                sr.color = Color.Lerp(originalColor, flashColor, progress);
                await UniTask.Yield(ct);
            }

            // 원래 색으로
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / halfDuration);
                sr.color = Color.Lerp(flashColor, originalColor, progress);
                await UniTask.Yield(ct);
            }

            sr.color = originalColor;
        }

        public static async UniTask FadeAsync(CanvasGroup cg, float from, float to, float duration, CancellationToken ct = default)
        {
            if (cg == null) return;

            float elapsed = 0f;
            cg.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(from, to, EaseOutQuad(progress));
                await UniTask.Yield(ct);
            }

            cg.alpha = to;
        }

        public static async UniTask ShakePositionAsync(Transform t, float intensity, float duration, CancellationToken ct = default)
        {
            var originalPos = t.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                float x = Random.Range(-1f, 1f) * intensity * decay;
                float y = Random.Range(-1f, 1f) * intensity * decay;
                t.localPosition = originalPos + new Vector3(x, y, 0f);
                await UniTask.Yield(ct);
            }

            t.localPosition = originalPos;
        }

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }
    }
}
