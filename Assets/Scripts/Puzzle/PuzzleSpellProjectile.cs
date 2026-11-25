using TxRpg.Data;
using TxRpg.Magic;
using UnityEngine;

namespace TxRpg.Puzzle
{
    /// <summary>
    /// Unity Physics2D(Rigidbody2D + CircleCollider2D)로 날아가는 탐험용 주문 투사체.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class PuzzleSpellProjectile : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 14f;
        [SerializeField] private float strongSpeed = 20f;
        [SerializeField] private float maxLifetime = 1.6f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D _rb;
        private SpellData _spell;
        private PuzzleRoomController _owner;
        private float _lifeTimer;

        public void Launch(SpellData spell, Vector2 direction, PuzzleRoomController owner)
        {
            _spell = spell;
            _owner = owner;
            _rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            float speed = spell.IsStrong ? strongSpeed : baseSpeed;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = direction.normalized * speed;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = spell.IsBurning
                    ? new Color(1f, 0.45f, 0.15f)
                    : (spell.Body == MagicWordType.Fire ? new Color(1f, 0.3f, 0.2f) : new Color(0.35f, 0.85f, 1f));
            }

            if (spell.IsStrong)
                transform.localScale *= 1.3f;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= maxLifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<VillagePlayerMovement>() != null) return;
            if (other.GetComponent<PuzzleWordPickup>() != null) return;
            if (other.GetComponent<PuzzleScenePortal>() != null) return;

            var plate = other.GetComponent<PuzzleImpactPlate>();
            if (plate != null)
            {
                plate.OnHitBySpell(_spell, _owner);
                Destroy(gameObject);
                return;
            }

            if (!other.isTrigger)
                Destroy(gameObject);
        }
    }
}
