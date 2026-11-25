using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace TxRpg.Effect
{
    public class ProjectileController : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private bool useArc;
        [SerializeField] private float arcHeight = 1.4f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void ConfigureVisual(Color color, float scale = 1f)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.color = color;
            transform.localScale = Vector3.one * scale;
        }

        public async UniTask LaunchAsync(Vector3 startPos, Vector3 endPos, CancellationToken ct = default)
        {
            transform.position = startPos;
            gameObject.SetActive(true);

            float distance = Vector3.Distance(startPos, endPos);
            float duration = Mathf.Max(0.12f, distance / Mathf.Max(speed, 0.1f));
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 prevPos = transform.position;
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                if (useArc)
                {
                    float arc = arcHeight * 4f * t * (1f - t);
                    pos.y += arc;
                }

                Vector3 direction = (pos - prevPos).normalized;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }

                transform.position = pos;
                await UniTask.Yield(ct);
            }

            transform.position = endPos;
            gameObject.SetActive(false);
        }
    }
}
