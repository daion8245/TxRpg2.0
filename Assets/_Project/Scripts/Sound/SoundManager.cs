using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Core;
using TxRpg.Core.Events;
using UnityEngine;

namespace TxRpg.Sound
{
    public class SoundManager : MonoBehaviour
    {
        [Header("이벤트 채널")]
        [SerializeField] private PlaySFXEventChannel sfxChannel;
        [SerializeField] private PlayBGMEventChannel bgmChannel;
        [SerializeField] private StopBGMEventChannel stopBgmChannel;

        [Header("설정")]
        [SerializeField] private int sfxPoolSize = 10;

        private AudioSource _bgmSource;
        private SoundPool _sfxPool;

        private void Awake()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxPool = new SoundPool(transform, sfxPoolSize);

            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            sfxChannel?.Register(OnPlaySFX);
            bgmChannel?.Register(OnPlayBGM);
            stopBgmChannel?.Register(OnStopBGM);
        }

        private void OnDisable()
        {
            sfxChannel?.Unregister(OnPlaySFX);
            bgmChannel?.Unregister(OnPlayBGM);
            stopBgmChannel?.Unregister(OnStopBGM);
        }

        private void OnPlaySFX(PlaySFXPayload payload)
        {
            PlaySFX(payload.Clip, payload.Volume);
        }

        private void OnPlayBGM(PlayBGMPayload payload)
        {
            PlayBGM(payload.Clip, payload.Volume, payload.Loop);
        }

        private void OnStopBGM(StopBGMPayload payload)
        {
            StopBGMAsync(payload.FadeDuration, destroyCancellationToken).Forget();
        }

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            _sfxPool.Play(clip, volume);
        }

        public void PlayBGM(AudioClip clip, float volume = 1f, bool loop = true)
        {
            if (clip == null) return;

            _bgmSource.clip = clip;
            _bgmSource.volume = volume;
            _bgmSource.loop = loop;
            _bgmSource.Play();
        }

        public async UniTask StopBGMAsync(float fadeDuration = 1f, CancellationToken ct = default)
        {
            if (!_bgmSource.isPlaying) return;

            float startVolume = _bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                await UniTask.Yield(ct);
            }

            _bgmSource.Stop();
            _bgmSource.volume = startVolume;
        }
    }
}
