using Cysharp.Threading.Tasks;
using TMPro;
using TxRpg.Core;
using UnityEngine;

namespace TxRpg.UI
{
    public class DamagePopup : MonoBehaviour, IPoolable
    {
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private float floatSpeed = 1.2f;
        [SerializeField] private float fadeDuration = 0.85f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = Color.yellow;
        [SerializeField] private Color healColor = Color.green;

        private float _elapsed;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (damageText == null)
                damageText = GetComponentInChildren<TMP_Text>();

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Show(int damage, bool isCritical, Vector3 worldPosition)
        {
            if (damage < 0)
                ShowText($"+{-damage}", healColor, worldPosition, 1f);
            else
                ShowText($"-{damage}", isCritical ? criticalColor : normalColor, worldPosition, isCritical ? 1.25f : 1f);
        }

        public void ShowText(string text, Color color, Vector3 worldPosition, float scale = 1f)
        {
            transform.position = worldPosition;
            transform.localScale = Vector3.one * scale;

            if (damageText != null)
            {
                damageText.text = text;
                damageText.color = color;
            }

            _elapsed = 0f;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            gameObject.SetActive(true);

            AnimateAsync(destroyCancellationToken).Forget();
        }

        private async UniTask AnimateAsync(System.Threading.CancellationToken ct)
        {
            while (_elapsed < fadeDuration)
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / fadeDuration);
                transform.position += Vector3.up * (floatSpeed * Time.deltaTime);
                if (_canvasGroup != null) _canvasGroup.alpha = 1f - t;
                await UniTask.Yield(ct);
            }

            Destroy(gameObject);
        }

        public void OnSpawn()
        {
            _elapsed = 0f;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        public void OnDespawn()
        {
            _elapsed = fadeDuration;
        }
    }
}
