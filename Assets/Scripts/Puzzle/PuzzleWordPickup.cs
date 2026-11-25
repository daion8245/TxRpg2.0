using TxRpg.Data;
using UnityEngine;
using UnityEngine.Events;

namespace TxRpg.Puzzle
{
    /// <summary>
    /// 2D 트리거 충돌로 마법 단어를 습득하는 룬 오브젝트.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PuzzleWordPickup : MonoBehaviour
    {
        [SerializeField] private MagicWordType word;
        [SerializeField] private UnityEvent onCollected;

        public MagicWordType Word => word;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<VillagePlayerMovement>() == null) return;

            var controller = FindFirstObjectByType<PuzzleRoomController>();
            controller?.CollectWord(word);
            onCollected?.Invoke();
            gameObject.SetActive(false);
        }

        public void ResetPickup()
        {
            gameObject.SetActive(true);
        }
    }
}
