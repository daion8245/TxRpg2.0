using System.Collections.Generic;
using UnityEngine;

namespace TxRpg.Sound
{
    public class SoundPool
    {
        private readonly List<AudioSource> _sources = new();
        private readonly Transform _parent;

        public SoundPool(Transform parent, int initialSize)
        {
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
            {
                _sources.Add(CreateSource());
            }
        }

        public void Play(AudioClip clip, float volume = 1f)
        {
            var source = GetAvailableSource();
            source.clip = clip;
            source.volume = volume;
            source.Play();
        }

        private AudioSource GetAvailableSource()
        {
            foreach (var source in _sources)
            {
                if (!source.isPlaying)
                    return source;
            }

            // 모든 소스가 사용 중이면 새로 생성
            var newSource = CreateSource();
            _sources.Add(newSource);
            return newSource;
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("SFX_Source");
            go.transform.SetParent(_parent);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
