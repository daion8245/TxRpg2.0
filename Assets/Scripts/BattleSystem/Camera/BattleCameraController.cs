using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Animation;
using TxRpg.Core;
using TxRpg.Core.Events;
using UnityEngine;

namespace TxRpg.Camera
{
    public class BattleCameraController : MonoBehaviour
    {
        [Header("이벤트 채널")]
        [SerializeField] private CameraShakeEventChannel shakeChannel;
        [SerializeField] private CameraZoomEventChannel zoomChannel;

        [Header("기본 설정")]
        [SerializeField] private float defaultOrthoSize = 5f;
        [SerializeField] private Vector3 defaultPosition = new(0f, 0f, -10f);

        private UnityEngine.Camera _camera;
        private CameraShaker _shaker;
        private CancellationTokenSource _zoomCts;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null)
                _camera = UnityEngine.Camera.main;

            _shaker = new CameraShaker(transform);

            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            shakeChannel?.Register(OnShake);
            zoomChannel?.Register(OnZoom);
        }

        private void OnDisable()
        {
            shakeChannel?.Unregister(OnShake);
            zoomChannel?.Unregister(OnZoom);
        }

        private void OnShake(CameraShakePayload payload)
        {
            Shake(payload.Intensity, payload.Duration);
        }

        private void OnZoom(CameraZoomPayload payload)
        {
            ZoomToAsync(payload.TargetPosition, payload.OrthoSize, payload.Duration, destroyCancellationToken).Forget();
        }

        public void Shake(float intensity, float duration)
        {
            _shaker.ShakeAsync(intensity, duration, destroyCancellationToken).Forget();
        }

        public async UniTask ZoomToAsync(Vector3 targetPos, float orthoSize, float duration, CancellationToken ct = default)
        {
            _zoomCts?.Cancel();
            _zoomCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            float startOrtho = _camera.orthographicSize;
            Vector3 startPos = transform.position;
            var endPos = new Vector3(targetPos.x, targetPos.y, startPos.z);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                _camera.orthographicSize = Mathf.Lerp(startOrtho, orthoSize, smooth);
                transform.position = Vector3.Lerp(startPos, endPos, smooth);

                await UniTask.Yield(_zoomCts.Token);
            }

            _camera.orthographicSize = orthoSize;
            transform.position = endPos;
        }

        public async UniTask ResetAsync(float duration = 0.5f, CancellationToken ct = default)
        {
            await ZoomToAsync(defaultPosition, defaultOrthoSize, duration, ct);
        }
    }
}
