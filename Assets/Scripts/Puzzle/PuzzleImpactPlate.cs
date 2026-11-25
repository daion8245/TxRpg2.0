using TxRpg.Data;
using TxRpg.Magic;
using UnityEngine;
using UnityEngine.Events;

namespace TxRpg.Puzzle
{
    /// <summary>
    /// 투척 방패의 물리적 충격을 감지해 시각/음향 단서를 주고 UnityEvent(문 열기)를 실행합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(AudioSource))]
    public class PuzzleImpactPlate : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer plateRenderer;
        [SerializeField] private Color idleColor = new(0.85f, 0.72f, 0.25f, 1f);
        [SerializeField] private Color activatedColor = new(0.35f, 0.95f, 0.55f, 1f);
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private UnityEvent onActivated;

        private AudioSource _audioSource;
        private AudioClip _chimeClip;
        private float _flashTimer;
        private bool _activated;

        public bool IsActivated => _activated;

        private void Awake()
        {
            if (plateRenderer == null) plateRenderer = GetComponentInChildren<SpriteRenderer>();
            _audioSource = GetComponent<AudioSource>();
            _chimeClip = BuildChimeClip();
            if (plateRenderer != null) plateRenderer.color = idleColor;
        }

        private void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (plateRenderer != null)
                {
                    Color baseCol = _activated ? activatedColor : idleColor;
                    plateRenderer.color = Color.Lerp(baseCol, flashColor, Mathf.Clamp01(_flashTimer / 0.45f));
                }
            }
        }

        public void OnHitBySpell(SpellData spell, PuzzleRoomController room)
        {
            if (spell == null) return;

            bool isThrownShield = spell.Body == MagicWordType.Shield && spell.IsLaunched;
            if (!isThrownShield)
            {
                _flashTimer = 0.25f;
                room?.SetFeedback($"충격판에 [{spell.Name}]이 닿았지만 반응하지 않습니다. [발사 + 방패]의 충격이 필요합니다!");
                return;
            }

            _flashTimer = 0.45f;
            if (!_activated)
            {
                _activated = true;
                if (_audioSource != null && _chimeClip != null)
                    _audioSource.PlayOneShot(_chimeClip, 0.8f);

                onActivated?.Invoke();
                room?.OnDoorUnlocked();
            }
            else
            {
                room?.SetFeedback("충격판이 이미 작동해 문이 열려 있습니다! 오른쪽 통로로 나가 전투 씬으로 이동하세요.");
            }
        }

        public void ResetPlate()
        {
            _activated = false;
            _flashTimer = 0f;
            if (plateRenderer != null) plateRenderer.color = idleColor;
        }

        private static AudioClip BuildChimeClip()
        {
            const int rate = 22050;
            int length = rate / 3;
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Exp(-t * 7f);
                data[i] = env * 0.35f * (Mathf.Sin(2f * Mathf.PI * 587.33f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 880f * t));
            }
            var clip = AudioClip.Create("PlateChime", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
