using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Animation;
using UnityEngine;

namespace TxRpg.Effect
{
    public class ScreenEffect : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private SpriteRenderer flashRenderer;

        public async UniTask FlashAsync(Color color, float duration, CancellationToken ct = default)
        {
            if (flashRenderer == null) return;

            flashRenderer.color = new Color(color.r, color.g, color.b, 0f);
            flashRenderer.gameObject.SetActive(true);

            float half = duration * 0.5f;
            float elapsed = 0f;

            // Fade in
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / half);
                flashRenderer.color = new Color(color.r, color.g, color.b, alpha);
                await UniTask.Yield(ct);
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / half);
                flashRenderer.color = new Color(color.r, color.g, color.b, alpha);
                await UniTask.Yield(ct);
            }

            flashRenderer.gameObject.SetActive(false);
        }

        public async UniTask FadeToBlackAsync(float duration, CancellationToken ct = default)
        {
            if (fadePanel == null) return;
            await TweenHelper.FadeAsync(fadePanel, 0f, 1f, duration, ct);
        }

        public async UniTask FadeFromBlackAsync(float duration, CancellationToken ct = default)
        {
            if (fadePanel == null) return;
            await TweenHelper.FadeAsync(fadePanel, 1f, 0f, duration, ct);
        }
    }
}
