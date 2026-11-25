using TxRpg.Core;
using UnityEngine;

namespace TxRpg.Puzzle
{
    /// <summary>
    /// 충격판의 UnityEvent에 연결되어 여닫히는 문. Collider2D와 위치를 제어합니다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class PuzzleDoor : MonoBehaviour
    {
        [SerializeField] private Vector3 openOffset = new(0f, 3.2f, 0f);
        [SerializeField] private float openSpeed = 5f;
        [SerializeField] private SpriteRenderer doorRenderer;
        [SerializeField] private Color closedColor = new(0.55f, 0.25f, 0.22f, 1f);
        [SerializeField] private Color openColor = new(0.28f, 0.72f, 0.45f, 0.35f);

        private BoxCollider2D _collider;
        private Vector3 _closedPosition;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            if (doorRenderer == null) doorRenderer = GetComponentInChildren<SpriteRenderer>();
            _closedPosition = transform.position;
        }

        private void Update()
        {
            Vector3 target = _isOpen ? _closedPosition + openOffset : _closedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, openSpeed * Time.deltaTime);
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            GameSessionState.PuzzleDoorOpened = true;
            if (_collider != null) _collider.enabled = false;
            if (doorRenderer != null) doorRenderer.color = openColor;
        }

        public void CloseImmediate()
        {
            _isOpen = false;
            GameSessionState.PuzzleDoorOpened = false;
            transform.position = _closedPosition;
            if (_collider != null) _collider.enabled = true;
            if (doorRenderer != null) doorRenderer.color = closedColor;
        }
    }
}
