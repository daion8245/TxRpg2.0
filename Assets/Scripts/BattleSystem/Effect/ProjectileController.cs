using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace TxRpg.Effect
{
    public class ProjectileController : MonoBehaviour
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private bool useArc;
        [SerializeField] private float arcHeight = 2f;

        public async UniTask LaunchAsync(Vector3 startPos, Vector3 endPos, CancellationToken ct = default)
        {
            transform.position = startPos;
            gameObject.SetActive(true);

            float distance = Vector3.Distance(startPos, endPos);
            float duration = distance / Mathf.Max(speed, 0.1f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                if (useArc)
                {
                    float arc = arcHeight * 4f * t * (1f - t);
                    pos.y += arc;
                }

                transform.position = pos;

                // 진행 방향으로 회전
                if (elapsed > 0)
                {
                    Vector3 direction = (pos - transform.position).normalized;
                    if (direction != Vector3.zero)
                    {
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                }

                await UniTask.Yield(ct);
            }

            transform.position = endPos;
            gameObject.SetActive(false);
        }
    }
}
