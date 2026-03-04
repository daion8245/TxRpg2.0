using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace TxRpg.Camera
{
    public class CameraShaker
    {
        private readonly Transform _transform;
        private Vector3 _originalLocalPos;
        private bool _isShaking;

        public CameraShaker(Transform transform)
        {
            _transform = transform;
            _originalLocalPos = transform.localPosition;
        }

        public async UniTask ShakeAsync(float intensity, float duration, CancellationToken ct = default)
        {
            if (_isShaking) return;

            _isShaking = true;
            _originalLocalPos = _transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);

                // Perlin 노이즈 기반 셰이크
                float x = (Mathf.PerlinNoise(elapsed * 25f, 0f) - 0.5f) * 2f * intensity * decay;
                float y = (Mathf.PerlinNoise(0f, elapsed * 25f) - 0.5f) * 2f * intensity * decay;

                _transform.localPosition = _originalLocalPos + new Vector3(x, y, 0f);
                await UniTask.Yield(ct);
            }

            _transform.localPosition = _originalLocalPos;
            _isShaking = false;
        }
    }
}
