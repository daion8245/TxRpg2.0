using Cysharp.Threading.Tasks;
using System.Threading;
using TxRpg.Animation;
using UnityEngine;

namespace TxRpg.Camera
{
    public class CinematicCamera
    {
        private readonly BattleCameraController _controller;

        public CinematicCamera(BattleCameraController controller)
        {
            _controller = controller;
        }

        public async UniTask ZoomInOnTarget(Transform target, float zoomSize, float duration, CancellationToken ct = default)
        {
            if (target == null) return;
            await _controller.ZoomToAsync(target.position, zoomSize, duration, ct);
        }

        public async UniTask PanTo(Vector3 position, float duration, CancellationToken ct = default)
        {
            await _controller.ZoomToAsync(position, UnityEngine.Camera.main.orthographicSize, duration, ct);
        }

        public async UniTask CutinSequence(Transform target, float zoomSize, float holdDuration, CancellationToken ct = default)
        {
            // 줌인
            await _controller.ZoomToAsync(target.position, zoomSize, 0.3f, ct);

            // 홀드
            await UniTask.Delay((int)(holdDuration * 1000), cancellationToken: ct);

            // 리셋
            await _controller.ResetAsync(0.3f, ct);
        }
    }
}
