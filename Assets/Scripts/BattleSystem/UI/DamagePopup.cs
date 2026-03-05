using Cysharp.Threading.Tasks;
using TxRpg.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TxRpg.UI
{
    public class DamagePopup : MonoBehaviour, IPoolable
    {
        [SerializeField] private Text damageText;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = Color.yellow;
        [SerializeField] private Color healColor = Color.green;

        private float _elapsed;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Show(int damage, bool isCritical, Vector3 worldPosition)
        {
            transform.position = worldPosition;

            if (damage < 0)
            {
                // 힐
                damageText.text = $"+{-damage}";
                damageText.color = healColor;
            }
            else
            {
                damageText.text = damage.ToString();
                damageText.color = isCritical ? criticalColor : normalColor;
            }

            if (isCritical)
                transform.localScale = Vector3.one * 1.3f;
            else
                transform.localScale = Vector3.one;

            _elapsed = 0f;
            _canvasGroup.alpha = 1f;

            AnimateAsync(destroyCancellationToken).Forget();
        }

        private async UniTask AnimateAsync(System.Threading.CancellationToken ct)
        {
            while (_elapsed < fadeDuration)
            {
                _elapsed += Time.deltaTime;
                float t = _elapsed / fadeDuration;

                // 위로 떠오르기
                transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

                // 페이드 아웃
                _canvasGroup.alpha = 1f - t;

                await UniTask.Yield(ct);
            }

            gameObject.SetActive(false);
        }

        public void OnSpawn()
        {
            _elapsed = 0f;
            _canvasGroup.alpha = 1f;
        }

        public void OnDespawn()
        {
            _elapsed = fadeDuration;
        }
    }
}
